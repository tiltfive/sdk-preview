/*
 * Copyright (C) 2020 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using UnityEngine;

using TiltFive;
using TiltFive.Logging;

namespace TiltFive
{

    /// <summary>
    /// Glasses Settings encapsulates all configuration data used by the Glasses'
    /// tracking runtime to compute the Head Pose and apply it to the Camera.
    /// </summary>
    [System.Serializable]
    public class GlassesSettings
    {
#if UNITY_EDITOR
        /// <summary>
        /// Editor only configuration to disable/enable stereo-rendering.
        /// </summary>
        public bool tiltFiveXR = true;
#endif
        /// <summary>
        /// The main camera used for rendering the Scene when the glasses are unavailable, and the gameobject used for the glasses pose.
        /// </summary>
        public Camera headPoseCamera;

        public const float MIN_FOV = 35f;
        public const float MAX_FOV = 64f;
        public const float DEFAULT_FOV = 55f;

        public bool overrideFOV = false;
        public float customFOV = DEFAULT_FOV;
        public float fieldOfView => overrideFOV
            ? Mathf.Clamp(customFOV, MIN_FOV, MAX_FOV)
            : DEFAULT_FOV;

        public GlassesMirrorMode glassesMirrorMode = GlassesMirrorMode.LeftEye;


        /// <summary>
        /// The content scale, in terms of meters per world space unit.
        /// </summary>
        /// <remarks>        
        /// This value can be useful for gravity scaling. Simply divide Earth gravity (9.81m/s^2) by the product of this value and the game board scale.
        /// </remarks>
        /// <example>
        /// Suppose the content scale is set to 1:10cm. Using Unity's default gravity setting,
        /// the player would see an object in freefall appear to accelerate 1/10th as fast as expected, which could feel
        /// unnatural if the game is meant to be perceived as taking place on the table in front of them in the player's space.
        /// To fix this, a script with a reference to the Tilt Five Manager could call the following on Awake():
        /// <code>Physics.gravity = new Vector3(0f, 9.81f / tiltFiveManager.glassesSettings.physicalMetersPerWorldSpaceUnit, 0f);</code>
        /// </example>
        public float physicalMetersPerWorldSpaceUnit => new Length(contentScaleRatio, contentScaleUnit).ToMeters;

        public float worldSpaceUnitsPerPhysicalMeter => 1 / Mathf.Max(physicalMetersPerWorldSpaceUnit, float.Epsilon);  // No dividing by zero.

        public float oneUnitLengthInMeters => (new Length(1, contentScaleUnit)).ToMeters;

        /// <summary>
        /// The real-world unit to be compared against when using <see cref="contentScaleRatio">.
        /// </summary>
        public LengthUnit contentScaleUnit = LengthUnit.Centimeters;

        /// <summary>
        /// The scaling ratio relates physical distances to world-space units.
        /// </summary>
        /// <remarks>
        /// This value defines how distance units in world-space should appear to players in the real world.
        /// This is useful for initially defining a game world's sense of scale, as well as for CAD applications.
        /// Use this value alongside <see cref="contentScaleUnit"> to choose the desired physical units (e.g. centimeters, inches, etc).
        /// Afterwards, use <see cref="gameBoardScale"> for cinematic or gameplay purposes.
        /// </remarks>
        /// <example>
        /// Suppose that we want to display a bedroom scene that is 10 units across in world space.
        /// Also suppose that a person standing in this virtual bedroom would measure that distance to be 4 meters.
        /// In this case, we want 10 in-game units to represent 4 meters. Dividing 10 by 4 gives us 2.5,
        /// so contentScaleRatio should be set to 2.5 for the player to perceive the virtual space at a 1:1 ratio with reality, assuming <see cref="contentScaleUnit"> is set to meters.
        /// If the room was now too large for a comfortable experience using the game board, we could change <see cref="contentScaleUnit"> to inches,
        /// and the room would appear to be 25 inches across, now entirely visible within the borders of the game board.
        /// </example>
        public float contentScaleRatio = 5f;

        /// <summary>
        /// The game board's scale multiplies the perceived size of objects in the scene.
        /// </summary>
        /// <remarks>
        /// When scaling the world to fit on the game board, it can be useful to think in terms of zoom (e.g. 2x, 10x, etc) rather than fussing with absolute units using <see cref="contentScaleRatio"> and <see cref="contentScaleUnit">.
        /// Directly modifying the game board's scale is convenient for cinematics, tweening/animation, and other use cases in which zooming in/out may be desirable.
        /// </remarks>
        public float gameBoardScale => gameBoard != null ? gameBoard.localScale : 1f;

        /// <summary>
        /// The game board is the window into the game world, as well as the origin about which the glasses/wand are tracked.
        /// </summary>
        /// <remarks>        
        /// It can be useful to modify the game board's location for cinematic purposes,
        /// such as following an object (such as a player's avatar) in the scene.
        /// This avoids the need to directly modify the positions/orientations of the glasses or wand,
        /// which track the player's movements relative to the game board.
        /// </remarks>
        public GameBoard gameBoard;

        /// <summary>
        /// The game board position or focal position offset.
        /// </summary>
        public Vector3 gameBoardCenter => gameBoard != null ? gameBoard.position : Vector3.zero;

        /// <summary>
        /// The game board rotation or focal rotational offset.
        /// </summary>
        public Vector3 gameBoardRotation => gameBoard != null ? gameBoard.rotation.eulerAngles : Vector3.zero;
    }

    public enum GlassesMirrorMode
    {
        None,
        LeftEye,
        RightEye,
        Stereoscopic
    }

    /// <summary>
    /// The Glasses API and runtime.
    /// </summary>
    public sealed class Glasses : Singleton<Glasses>
    {

        #region Private Fields

        /// <summary>
        /// The glasses core runtime.
        /// </summary>
        private GlassesCore glassesCore = new GlassesCore();
        
        #endregion


        #region Public Fields

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:TiltFive.Glasses"/> is updated.
        /// </summary>
        /// <value><c>true</c> if updated; otherwise, <c>false</c>.</value>
        public static bool updated => Instance.glassesCore.TrackingUpdated;
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:TiltFive.Glasses"/> is configured.
        /// </summary>
        /// <value><c>true</c> if configured; otherwise, <c>false</c>.</value>
        public static bool configured => Instance.glassesCore.configured;
        /// <summary>
        /// Gets the head pose position.
        /// </summary>
        /// <value>The position.</value>
        public static Vector3 position => Instance.glassesCore.headPosition;
        /// <summary>
        /// Gets the head pose rotation.
        /// </summary>
        /// <value>The rotation.</value>
        public static Quaternion rotation => Instance.glassesCore.headRotation;
        /// <summary>
        /// Gets the head orientation's forward vector.
        /// </summary>
        /// <value>The forward vector.</value>
        public static Vector3 forward => rotation * Vector3.forward;
        /// <summary>
        /// Gets the head orientation's right vector.
        /// </summary>
        /// <value>The right vector.</value>
        public static Vector3 right => rotation * Vector3.right;
        /// <summary>
        /// Gets the head orientation's up vector.
        /// </summary>
        /// <value>The up vector.</value>
        public static Vector3 up => rotation * Vector3.up;

        /// <summary>
        /// Gets the left eye position.
        /// </summary>
        /// <value>The left eye position.</value>
        public static Vector3 leftEyePosition => Instance.glassesCore.eyePosition[(int)AREyes.EYE_LEFT];
        /// <summary>
        /// Gets the right eye position.
        /// </summary>
        /// <value>The right eye position.</value>
        public static Vector3 rightEyePosition => Instance.glassesCore.eyePosition[(int)AREyes.EYE_RIGHT];

        /// <summary>
        /// Indicates whether the glasses are plugged in and functioning.
        /// </summary>
        public static bool glassesAvailable {get; private set;}

        #endregion Public Fields


        #region Public Functions

        /// <summary>
        /// Returns a boolean indication that the head pose was successfully
        /// updated.
        /// </summary>
        /// <returns><c>true</c>, if the head pose was updated, <c>false</c> otherwise.</returns>
        public static bool headPoseUpdated() { return Instance.glassesCore.TrackingUpdated; }

        /// <summary>
        /// Reset this <see cref="T:TiltFive.Glasses"/>.
        /// </summary>
        /// <param name="glassesSettings">Glasses settings for configuring the instance.</param>
        public static void Reset(GlassesSettings glassesSettings)
        {
            Instance.glassesCore.Reset(glassesSettings);
        }

        /// <summary>
        /// Validates the specified glassesSettings with the current instance.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the glasses core is valid with the given settings,
        ///     <c>false</c> otherwise.
        /// </returns>
        /// <param name="glassesSettings">Glasses settings.</param>
        public static bool Validate(GlassesSettings glassesSettings)
        {
            return Instance.glassesCore.Validate(glassesSettings);
        }

        /// <summary>
        /// Updates this <see cref="T:TiltFive.Glasses"/>.
        /// </summary>
        /// <param name="glassesSettings">Glasses settings for the update.</param>
        public static void Update(GlassesSettings glassesSettings)
        {
            Instance.glassesCore.Update(glassesSettings);
        }

        #endregion Public Functions

        /// <summary>
        /// Internal Glasses core runtime.
        /// </summary>
        private class GlassesCore
        {

            /// <summary>
            /// Configuration ready indicator.
            /// </summary>
            public bool configured = false;

            /// <summary>
            /// The position of the game board reference frame w.r.t. the Unity
            /// world-space reference frame.
            /// </summary>
            private Vector3 gameBoardPosition_UnityWorldSpace = Vector3.zero;

            /// <summary>
            /// The rotation taking points from the Unity world-space reference
            /// frame to the game board reference frame.
            /// </summary>
            private Quaternion gameBoardRotation_UnityWorldSpace = Quaternion.identity;

            /// <summary>
            /// The position of the glasses reference frame (half-way between
            /// the eyes) w.r.t. the game board reference frame.
            /// </summary>
            private Vector3 glassesPosition_GameBoardSpace = Vector3.zero;

            /// <summary>
            /// The rotation taking points from the game board reference frame
            /// to the glasses reference frame.
            /// </summary>
            private Quaternion glassesRotation_GameBoardSpace = Quaternion.identity;

            public Vector3 headPosition = Vector3.zero;
            public Quaternion headRotation = Quaternion.identity;
            public Vector3[] eyePosition = new Vector3[(int)AREyes.EYE_MAX];
            private Quaternion[] eyeRotation = new Quaternion[(int)AREyes.EYE_MAX];

            /// <summary>
            /// The default position of the glasses relative to the board.
            /// </summary>
            /// <remarks>
            /// The glasses camera will snap back to this position if the glasses are unavailable.
            /// If different behavior is desired in this scenario, a different camera should be used.
            /// </remarks>
            private readonly Vector3 DEFAULT_GLASSES_POSITION_GAME_BOARD_SPACE = new Vector3(0f, 0.5f, -0.5f);

            /// <summary>
            /// The default rotation of the glasses relative to the board.
            /// </summary>
            /// <remarks>
            /// The glasses camera will snap back to this rotation if the glasses are unavailable.
            /// If different behavior is desired in this scenario, a different camera should be used.
            /// </remarks>
            private readonly Quaternion DEFAULT_GLASSES_ROTATION_GAME_BOARD_SPACE = Quaternion.Euler(new Vector3(-45f, 0f, 0f));

            /// <summary>
            /// Gets a value indicating whether this <see cref="T:TiltFive.Glasses.GlassesCore"/> tracking was successfully updated.
            /// </summary>
            /// <value><c>true</c> if tracking updated; otherwise, <c>false</c>.</value>
            public bool TrackingUpdated { get; private set; } = false;

            /// <summary>
            /// The split stereo camera implementation used in lieu of XRSettings.
            /// </summary>
            protected SplitStereoCamera splitStereoCamera = null;

            /// <summary>
            /// Reset this <see cref="T:TiltFive.Glasses.GlassesCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for configuring the instance.</param>
            public void Reset(GlassesSettings glassesSettings)
            {
                configured = false;

                if (null == glassesSettings.headPoseCamera)
                {
                    Log.Error($"Required Camera assignment missing from { GetType() }.");
                    return;
                }

                if(glassesSettings.headPoseCamera.fieldOfView != glassesSettings.fieldOfView)
                {
                    glassesSettings.headPoseCamera.fieldOfView = glassesSettings.fieldOfView;
                }

#if UNITY_EDITOR
                if (glassesSettings.tiltFiveXR)
                {
#endif
                    //if the splitScreenCamera does not exist already.
                    if (null == splitStereoCamera)
                    {
                        //get the head pose camera's GameObject
                        GameObject cameraObject = glassesSettings.headPoseCamera.gameObject;

                        //Check whether it is set up as a SplitScreenCamera, and if not:
                        if (!cameraObject.TryGetComponent<SplitStereoCamera>(out splitStereoCamera))
                        {
                            // Add it ourselves. The OnAwake call will create & configure
                            // the eye cameras to render with. it will also use theCamera
                            // as the source.
                            splitStereoCamera = cameraObject.AddComponent<SplitStereoCamera>();
                        }
                    }
#if UNITY_EDITOR
                }
#endif //UNITY_EDITOR

                configured = true;
            }

            /// <summary>
            /// Update the pose of the game board.
            /// </summary>
            /// <param name="glassesSettings">Glasses settings.</param>
            private void UpdateGameBoardPose(GlassesSettings glassesSettings)
            {
                Vector3 posUGBL_TGT = glassesSettings.gameBoardCenter;
                Quaternion rotToUGBL_TGT = Quaternion.Euler(glassesSettings.gameBoardRotation);

                gameBoardPosition_UnityWorldSpace = posUGBL_TGT;
                gameBoardRotation_UnityWorldSpace = rotToUGBL_TGT;
            }

            /// <summary>
            /// Tests this <see cref="T:TiltFive.Glasses.GlassesCore"/> for validity
            /// with the paramterized <see cref="T:TiltFive.Glasses.GlassesSettings"/>
            /// </summary>
            /// <returns><c>true</c>, if valid, <c>false</c> otherwise.</returns>
            /// <param name="glassesSettings">Glasses settings.</param>
            public bool Validate(GlassesSettings glassesSettings)
            {
                bool valid = true;
                valid &= (glassesSettings.headPoseCamera == splitStereoCamera.headPoseCamera);
                valid &= (glassesSettings.headPoseCamera.fieldOfView == glassesSettings.fieldOfView);
                return valid;
            }

            /// <summary>
            /// Updates this <see cref="T:TiltFive.Glasses.GlassesCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for the update.</param>
            public virtual void Update(GlassesSettings glassesSettings)
            {
                TrackingUpdated = false;
#if UNITY_EDITOR
                if (null == glassesSettings)
                {
                    Log.Error("GlassesSettings configuration required for Glasses tracking Update.");
                    return;
                }
#endif

                if (null == splitStereoCamera)
                {
                    Log.Error("Stereo camera(s) missing from Glasses - aborting Update.");
                    return;
                }

                if (glassesSettings.headPoseCamera != splitStereoCamera.headPoseCamera)
                {
                    Log.Warn("Found mismatched Cameras in GlassesCore Update - should call Reset.");
                    return;
                }

                // Check whether the glasses are plugged in and available.                
                glassesAvailable = Display.GetGlassesAvailability();
                splitStereoCamera.enabled = glassesAvailable && glassesSettings.glassesMirrorMode != GlassesMirrorMode.None;
                splitStereoCamera.glassesMirrorMode = glassesSettings.glassesMirrorMode;

                // Latch the latest user-provided transforms game board transforms.
                UpdateGameBoardPose(glassesSettings);

                // Get the latest glasses pose w.r.t. the game board.
                glassesPosition_GameBoardSpace = DEFAULT_GLASSES_POSITION_GAME_BOARD_SPACE;
                glassesRotation_GameBoardSpace = DEFAULT_GLASSES_ROTATION_GAME_BOARD_SPACE;

                if(glassesAvailable)
                {
                    Display.GetHeadPose(ref glassesPosition_GameBoardSpace, ref glassesRotation_GameBoardSpace);
                }

                // Get the glasses pose in Unity world-space.
                float scaleToUGBL_UWRLD = glassesSettings.physicalMetersPerWorldSpaceUnit * glassesSettings.gameBoardScale;
                if(scaleToUGBL_UWRLD <= 0)
                {
                    Log.Error("Division by zero error: Content Scale and Game Board scale must be positive non-zero values.");
                    scaleToUGBL_UWRLD = Mathf.Max(scaleToUGBL_UWRLD, float.Epsilon);
                }

                float scaleToUWRLD_UGBL = 1.0f / scaleToUGBL_UWRLD;
                Vector3 posUGLS_UWRLD = Quaternion.Inverse(gameBoardRotation_UnityWorldSpace) *
                    (scaleToUWRLD_UGBL * glassesPosition_GameBoardSpace) + gameBoardPosition_UnityWorldSpace;
                Quaternion rotToUGLS_UWRLD = glassesRotation_GameBoardSpace * gameBoardRotation_UnityWorldSpace;

                // Set the head pose (main) camera.
                headPosition = posUGLS_UWRLD;
                headRotation = Quaternion.Inverse(rotToUGLS_UWRLD);

                // Set the game board transform on the SplitStereoCamera.
                splitStereoCamera.posUGBL_UWRLD = gameBoardPosition_UnityWorldSpace;
                splitStereoCamera.rotToUGBL_UWRLD = gameBoardRotation_UnityWorldSpace;
                splitStereoCamera.scaleToUGBL_UWRLD = scaleToUGBL_UWRLD;

                // TODO: Revisit native XR support.

                // NOTE: We do this because "Mock HMD" in UNITY_2017_0_2_OR_NEWER
                // the fieldOfView is locked to 111.96 degrees (Vive emulation),
                // so setting custom projection matrices is broken. If Unity
                // opens the API to custom settings, we can go back to native XR
                // support.

                // Manual split screen 'new glasses' until the day Unity lets
                // me override their Mock HMD settings.
                Camera theCamera = glassesSettings.headPoseCamera;
                if (null == theCamera)
                {
                    Log.Error("Main Camera missing from GlassesSettings - aborting Update.");
                    return;
                }

                Transform headPose = theCamera.transform;
                headPose.transform.position = headPosition;
                headPose.transform.rotation = headRotation;

                // compute half ipd translation
                float ipd_UGBL = 0.063f;
                // float ipd_GameBoardSpace
                float ipd_UWRLD = scaleToUWRLD_UGBL * ipd_UGBL;
                // float ipd_UnityWorldSpace
                Vector3 eyeOffset = (headPose.right.normalized * (ipd_UWRLD * 0.5f));

                // set the left eye camera offset from the head by the half ipd amount (-)
                eyePosition[(int)AREyes.EYE_LEFT] = headPose.position - eyeOffset;
                eyeRotation[(int)AREyes.EYE_LEFT] = headRotation;

                // set the right eye camera offset from the head by the half ipd amount (+)
                eyePosition[(int)AREyes.EYE_RIGHT] = headPose.position + eyeOffset;
                eyeRotation[(int)AREyes.EYE_RIGHT] = headRotation;

                Camera leftEyeCamera = splitStereoCamera.leftEyeCamera;
                if (null != leftEyeCamera)
                {
                    GameObject leftEye = leftEyeCamera.gameObject;
                    leftEye.transform.position = eyePosition[(int)AREyes.EYE_LEFT];
                    leftEye.transform.rotation = eyeRotation[(int)AREyes.EYE_LEFT];

                    //make sure projection fields are synchronized to the head camera.
                    leftEyeCamera.nearClipPlane = glassesSettings.headPoseCamera.nearClipPlane;
                    leftEyeCamera.farClipPlane = glassesSettings.headPoseCamera.farClipPlane;
                    leftEyeCamera.fieldOfView = glassesSettings.headPoseCamera.fieldOfView;
                }

                Camera rightEyeCamera = splitStereoCamera.rightEyeCamera;
                if (null != rightEyeCamera)
                {
                    GameObject rightEye = rightEyeCamera.gameObject;
                    rightEye.transform.position = eyePosition[(int)AREyes.EYE_RIGHT];
                    rightEye.transform.rotation = eyeRotation[(int)AREyes.EYE_RIGHT];

                    //make sure projection fields are synchronized to the head camera.
                    rightEyeCamera.nearClipPlane = glassesSettings.headPoseCamera.nearClipPlane;
                    rightEyeCamera.farClipPlane = glassesSettings.headPoseCamera.farClipPlane;
                    rightEyeCamera.fieldOfView = glassesSettings.headPoseCamera.fieldOfView;
                }
            }
        }
    }
}
