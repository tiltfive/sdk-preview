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

using System.Collections;
using UnityEngine;
using TiltFive;
using TiltFive.Logging;

namespace TiltFive
{

    /// <summary>
    /// Display settings constants.
    /// </summary>
    [System.Serializable]
    public class DisplaySettings
    {
        /// <summary> The display width. </summary>
        public const int width = 2560;
        /// <summary> The display height. </summary>
        public const int height = 720;
        /// <summary> The display half width (in stereo rendering this is width for a single eye). </summary>
        public const int halfWidth = (width / 2);
        /// <summary> The depth buffer's precision. </summary>
        public const int depthBuffer = 24;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class SplitStereoCamera : MonoBehaviour
    {

        /// <summary> Name assigned to any dynamically created (missing) head pose camera. </summary>
        private const string HEAD_CAMERA_NAME = "Head Camera";
        /// <summary> The head pose (Main) camera. </summary>
        public Camera theHeadPoseCamera = null;
        /// <summary> The head pose Camera property. </summary>
        public Camera headPoseCamera { get { return theHeadPoseCamera; } }
        /// <summary> The head pose GameObject property. </summary>
        public GameObject headPose { get { return theHeadPoseCamera.gameObject; } }

        /// <summary> The name assigned to the dynamically created camera used for rendering the left eye. </summary>
        private const string LEFT_EYE_CAMERA_NAME = "Left Eye Camera";
        /// <summary> The left eye camera GameObject. </summary>
        private GameObject leftEye;
        /// <summary> The left eye Camera property. </summary>
        public Camera leftEyeCamera { get { return eyeCamera[(int)AREyes.EYE_LEFT]; } }

        /// <summary> The name assigned to the dynamically created camera used for rendering the right eye. </summary>
        private const string RIGHT_EYE_CAMERA_NAME = "Right Eye Camera";
        /// <summary> The right eye camera GameObject. </summary>
        private GameObject rightEye;
        /// <summary> The right eye Camera property. </summary>
        public Camera rightEyeCamera { get { return eyeCamera[(int)AREyes.EYE_RIGHT]; } }

        /// <summary> In-editor toggle for displaying the eye cameras in the runtime Hierarchy. </summary>
        public bool showCameras = true;
        /// <summary> The Camera objects. </summary>
        private Camera[] eyeCamera = new Camera[2];

        /// <summary>
        /// The position of the game board reference frame w.r.t. the Unity
        /// world-space reference frame.
        /// </summary>
        public Vector3 posUGBL_UWRLD = Vector3.zero;

        /// <summary>
        /// The rotation taking points from the Unity world-space reference
        /// frame to the game board reference frame.
        /// </summary>
        public Quaternion rotToUGBL_UWRLD = Quaternion.identity;

        /// <summary>
        /// The uniform scale factor that takes points from the Unity
        /// world-space to the game board reference frame.
        /// </summary>
        public float scaleToUGBL_UWRLD = 1.0f;

        /// <summary> The name of the custom shader that blits the rendertextures to the backbuffer. </summary>
        private const string SHADER_DISPLAY_BLIT = "Tilt Five/Simple Blend Shader";
        /// <summary> The Material used to store/reference the shader. </summary>
        private Material displayBlitShader;

        private System.IntPtr leftTexHandle;
        private System.IntPtr rightTexHandle;

        /// <summary> The Cameras' field of view property. </summary>
        public float fieldOfView
        {
            get { return headPoseCamera.fieldOfView; }
            set { rightEyeCamera.fieldOfView = leftEyeCamera.fieldOfView = headPoseCamera.fieldOfView = value; }
        }

        /// <summary> The Cameras' near clip plane property. </summary>
        public float nearClipPlane
        {
            get { return headPoseCamera.nearClipPlane; }
            set { rightEyeCamera.nearClipPlane = leftEyeCamera.nearClipPlane = headPoseCamera.nearClipPlane = value; }
        }

        /// <summary> The Cameras' far clip plane property. </summary>
        public float farClipPlane
        {
            get { return headPoseCamera.farClipPlane; }
            set { rightEyeCamera.farClipPlane = leftEyeCamera.farClipPlane = headPoseCamera.farClipPlane = value; }
        }

        /// <summary> The Cameras' aspect ratio property. </summary>
        public float aspectRatio
        {
            get { return headPoseCamera.aspect; }
            set
            {
                headPoseCamera.aspect = value;
                rightEyeCamera.aspect = leftEyeCamera.aspect = value;
            }
        }

        /// <summary>
        /// Awake this instance.
        /// </summary>
        void Awake()
        {

#if UNITY_EDITOR
            // if we are initializing this, then we need to make sure we turn off XR
            // we need to do it before we start mucking with Cameras or else we get
            // warnings about XREyeTextures.
            if (UnityEngine.XR.XRSettings.enabled)
            {
                Log.Warn("XRSettings.enabled=true loadDeviceName={0}. Setting to none and disabling in {1}.", UnityEngine.XR.XRSettings.loadedDeviceName, GetType());
                StartCoroutine(LoadUnityXRSDK("none", false));
            }
#endif


            // try to get the head camera from the GameObject
            // in RT mode, the Camera has to be the same object as the stero camera script.            
            if(!this.TryGetComponent<Camera>(out theHeadPoseCamera))
            {
                //Create one on the GameObject
                theHeadPoseCamera = gameObject.AddComponent<Camera>();
                headPoseCamera.name = HEAD_CAMERA_NAME;
                Log.Warn("Runtime AddComponent<Camera> to GameObject.name={0}", HEAD_CAMERA_NAME);
            }

            // Our manual Split Stereo won't work with HDR enabled during setup
            // because (of course) Unity breaks viewport settings in this case.
            bool allowHDR = theHeadPoseCamera.allowHDR;
            theHeadPoseCamera.allowHDR = false;

            // For this mode, we need the headPose Camera to be enabled, as it is the
            // primary Camera for blitting to the backbuffer.
            headPoseCamera.enabled = true;


            leftEye = GameObject.Find(LEFT_EYE_CAMERA_NAME);
            if (null != leftEye)
            {
                Destroy(leftEye);
                leftEye = null;
                Log.Warn("Runtime replacement of Scene's pre-existing GameObject.name={0}", LEFT_EYE_CAMERA_NAME);
            }

            leftEye = new GameObject(LEFT_EYE_CAMERA_NAME);
            leftEye.transform.parent = transform;
            eyeCamera[(int)AREyes.EYE_LEFT] = leftEye.AddComponent<Camera>();

            leftEyeCamera.CopyFrom(headPoseCamera);

            //now the right eye Camera object
            rightEye = GameObject.Find(RIGHT_EYE_CAMERA_NAME);
            if (null != rightEye)
            {
                Destroy(rightEye);
                rightEye = null;
                Log.Warn("Runtime replacement of Scene's pre-existing GameObject.name={0}", RIGHT_EYE_CAMERA_NAME);
            }

            rightEye = new GameObject(RIGHT_EYE_CAMERA_NAME);
            rightEye.transform.parent = transform;
            eyeCamera[(int)AREyes.EYE_RIGHT] = rightEye.AddComponent<Camera>();

            rightEyeCamera.CopyFrom(headPoseCamera);

            //create the left eye camera's render texture
            RenderTexture leftTex = new RenderTexture(
                DisplaySettings.halfWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                RenderTextureFormat.ARGB32);
            if (leftEyeCamera.allowMSAA && QualitySettings.antiAliasing > 1)
            {
                leftTex.antiAliasing = QualitySettings.antiAliasing;
            }
            leftEyeCamera.targetTexture = leftTex;
            leftEyeCamera.depth = headPoseCamera.depth - 1;

            //create the right eye camera's render texture
            RenderTexture rightTex = new RenderTexture(DisplaySettings.halfWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                RenderTextureFormat.ARGB32);
            if (rightEyeCamera.allowMSAA && QualitySettings.antiAliasing > 1)
            {
                rightTex.antiAliasing = QualitySettings.antiAliasing;
            }
            rightEyeCamera.targetTexture = rightTex;
            rightEyeCamera.depth = headPoseCamera.depth - 1;


            // Load the blitting shader to copy the the left & right render textures
            // into the backbuffer
            displayBlitShader = new Material(Shader.Find(SHADER_DISPLAY_BLIT));
            // Did we find it?
            if (null == displayBlitShader)
            {
                Log.Error("Failed to load Shader '{0}'", SHADER_DISPLAY_BLIT);
            }

            theHeadPoseCamera.allowHDR = allowHDR; //restore HDR setting

            SyncFields(headPoseCamera);
            SyncTransform();
            showHideCameras();

        }

        /// <summary>
        /// Sets Unity's XR SDK to the input parameters. This should NEVER run.
        /// </summary>
        /// <returns>On completion.</returns>
        /// <param name="newDevice">The XR device name to load.</param>
        /// <param name="useXr">If set to <c>true</c>, XRSettings are enabled. Otherwise, false.</param>
        IEnumerator LoadUnityXRSDK(string newDevice, bool useXr)
        {
            if (System.String.Compare(UnityEngine.XR.XRSettings.loadedDeviceName, newDevice, true) != 0)
            {
                UnityEngine.XR.XRSettings.LoadDeviceByName(newDevice);
                yield return null;
                UnityEngine.XR.XRSettings.enabled = useXr;
                yield return new WaitForEndOfFrame();

                Log.Debug("loadedDeviceName={0} and XRSettings.enabled={1}",
                    UnityEngine.XR.XRSettings.loadedDeviceName,
                    UnityEngine.XR.XRSettings.enabled);
            }
        }

        /// <summary>
        /// EDITOR-ONLY: Syncs the eye Cameras' transform to the Head Pose
        /// when tracking is not available.
        /// </summary>
        void SyncTransform()
        {

#if UNITY_EDITOR
            // We move the eye Cameras in the Editor to emulate head pose and eye movement.
            // In builds, we only set the camera transforms with Glasses tracking data.

            if (null == headPoseCamera)
                return;

            if (!Glasses.updated)
            {
                GameObject pose = headPose;
                // left eye copy and adjust
                leftEye.transform.position = pose.transform.position;
                leftEye.transform.localPosition = pose.transform.localPosition;
                leftEye.transform.rotation = pose.transform.rotation;
                leftEye.transform.localRotation = pose.transform.localRotation;
                leftEye.transform.localScale = pose.transform.localScale;
                leftEye.transform.Translate(-leftEye.transform.right.normalized * (headPoseCamera.stereoSeparation * 0.5f));

                //right eye copy and adjust
                rightEye.transform.position = pose.transform.position;
                rightEye.transform.localPosition = pose.transform.localPosition;
                rightEye.transform.rotation = pose.transform.rotation;
                rightEye.transform.localRotation = pose.transform.localRotation;
                rightEye.transform.localScale = headPose.transform.localScale;
                rightEye.transform.Translate(rightEye.transform.right.normalized * (headPoseCamera.stereoSeparation * 0.5f));
            }
#endif
        }

        /// <summary>
        /// Configure rendering parameters for the upcoming frame.
        /// </summary>
        void OnPreRender()
        {
            headPoseCamera.targetTexture = null;

            // Check whether the left/right render textures' states have been invalidated,
            // and reset the cached texture handles if so. See the longer explanation below in Update()
            if (!leftEyeCamera.targetTexture.IsCreated() || !rightEyeCamera.targetTexture.IsCreated())
            {
                leftTexHandle = System.IntPtr.Zero;
                rightTexHandle = System.IntPtr.Zero;
            }
        }

        /// <summary>
        /// Configure rendering parameters now that frame is completed rendering.
        /// </summary>
        void OnPostRender()
        {
            // this runs before OnRenderImage(_, _)
        }

        /// <summary>
        /// Apply post-processing effects to the final image before it is
        /// presented.
        /// </summary>
        /// <param name="src">The source render texture.</param>
        /// <param name="dst">The destination render texture.</param>
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            Graphics.Blit(leftEyeCamera.targetTexture,
                          null as RenderTexture,
                          new Vector2(1.0f, 1.0f),
                          new Vector2(0.0f, 0.0f));

            Graphics.Blit(rightEyeCamera.targetTexture,
                          null as RenderTexture,
                          new Vector2(1.0f, 1.0f),
                          new Vector2(0.0f, 0.0f));

            bool isSrgb = leftEyeCamera.targetTexture.sRGB;

            float fovYDegrees = theHeadPoseCamera.fieldOfView;

            Vector3 posULVC_UWRLD = leftEyeCamera.transform.position;
            Quaternion rotToUWRLD_ULVC = leftEyeCamera.transform.rotation;
            Vector3 posURVC_UWRLD = rightEyeCamera.transform.position;
            Quaternion rotToUWRLD_URVC = rightEyeCamera.transform.rotation;

            Vector3 posULVC_UGBL = rotToUGBL_UWRLD * (scaleToUGBL_UWRLD * (posULVC_UWRLD - posUGBL_UWRLD));
            Quaternion rotToUGBL_ULVC = rotToUGBL_UWRLD * rotToUWRLD_ULVC;

            Vector3 posURVC_UGBL = rotToUGBL_UWRLD * (scaleToUGBL_UWRLD * (posURVC_UWRLD - posUGBL_UWRLD));
            Quaternion rotToUGBL_URVC = rotToUGBL_UWRLD * rotToUWRLD_URVC;


            /* Render textures have a state (created or not created), and that state can be invalidated.
            There are a few ways this can happen, including the game switching to/from fullscreen, 
            or the system screensaver being displayed. When this happens, the native texture pointers we
            pass to the native plugin are also invalidated, and garbage data gets displayed by the glasses.
                        
            To fix this, we can check whether the state has been invalidated and reacquire a valid native texture pointer.
            RenderTexture's IsCreated() function reports false if the render texture has been invalidated.
            We must detect this change above in OnPreRender(), because IsCreated reports true within Update().
            If we detect that the render textures have been invalidated, we null out the cached pointers and reacquire here.
            */
            if(leftTexHandle == System.IntPtr.Zero || rightTexHandle == System.IntPtr.Zero)
            {                
                leftTexHandle = leftEyeCamera.targetTexture.GetNativeTexturePtr();
                rightTexHandle = rightEyeCamera.targetTexture.GetNativeTexturePtr();
            }            

            Plugin.PresentStereoImages(leftTexHandle, rightTexHandle,
                                       leftEyeCamera.targetTexture.width, leftEyeCamera.targetTexture.height,
                                       isSrgb,
                                       fovYDegrees,
                                       rotToUGBL_ULVC,
                                       posULVC_UGBL,
                                       rotToUGBL_URVC,
                                       posURVC_UGBL);
        }

        /// <summary>
        /// Syncs the Cameras' fields to the input parameter.
        /// </summary>
        /// <param name="theCamera">The camera to read from.</param>
        void SyncFields(Camera theCamera)
        {
            fieldOfView = theCamera.fieldOfView;
            nearClipPlane = theCamera.nearClipPlane;
            farClipPlane = theCamera.farClipPlane;
            aspectRatio = theCamera.aspect;
        }

        /// <summary>
        /// EDITOR-ONLY
        /// </summary>
        void OnValidate()
        {

#if UNITY_EDITOR
            if (false == UnityEditor.EditorApplication.isPlaying)
                return;
#endif
            if (null == headPoseCamera)
                return;

            if (null != leftEye && null != rightEye)
                showHideCameras();

            SyncFields(headPoseCamera);
            SyncTransform();
        }

        /// <summary>
        /// Show/hide to the eye camerasin the hierarchy.
        /// </summary>
        void showHideCameras()
        {
            if (showCameras)
            {
                leftEye.hideFlags = HideFlags.None;
                rightEye.hideFlags = HideFlags.None;
            }
            else
            {
                leftEye.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                rightEye.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            }
        }
    }
}
