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

using TiltFive.Logging;

namespace TiltFive
{
    public enum AREyes
    {
        EYE_LEFT = 0,
        EYE_RIGHT,
        EYE_MAX,
    }

    [Serializable]
    public class AxesBoolean
    {
        public bool x = true;
        public bool y = true;
        public bool z = true;

        public AxesBoolean(bool setX, bool setY, bool setZ)
        {
            x = setX;
            y = setY;
            z = setZ;
        }
    }

    [Serializable]
    public class AllAxesBoolean
    {
        public bool xyz = true;

        public AllAxesBoolean(bool setXYZ)
        {
            xyz = setXYZ;
        }
    }

    public struct ARProjectionFrustum
    {
        public float m_Left;
        public float m_Right;
        public float m_Bottom;
        public float m_Top;
        public float m_Near;
        public float m_Far;


        public ARProjectionFrustum(float l, float r, float b, float t, float n, float f)
        {
            m_Left = l; m_Right = r; m_Bottom = b; m_Top = t; m_Near = n; m_Far = f;
        }
    }

    public class Display : TiltFive.SingletonComponent<Display>
    {
        // Head Pose
        [NonSerialized]
        float[] _headPosition = new float[3];
        [NonSerialized]
        float[] _headRotation = new float[4];

        // Wand Pose
        [NonSerialized]
        float[] _wandPosition = new float[3];
        [NonSerialized]
        float[] _wandRotation = new float[4];

        // Stereo camera poses.
        [NonSerialized] float[] _rotToULVC_UGBL = new float[4];
        [NonSerialized] float[] _rotToURVC_UGBL = new float[4];
        [NonSerialized] float[] _posULVC_UGBL = new float[3];
        [NonSerialized] float[] _posURVC_UGBL = new float[3];

        // Display Settings.
        [NonSerialized] int[] _displaySettings = new int[2];

        // Frame sender render-thread callback.
        [NonSerialized]
        IntPtr _sendFrameCallback = IntPtr.Zero;

        void Awake()
        {
            _sendFrameCallback = NativePlugin.GetSendFrameCallback();

            LogVersion();

            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 0;
        }

        void Start()
        {

        }

        void Update()
        {

        }

        private void LogVersion()
        {
            string version = "NOT VERSIONED";

            // load version file and get the string value
            TextAsset asset = (TextAsset)Resources.Load("pluginversion", typeof(TextAsset));
            if (asset != null)
            {
                version = asset.text;
            }

            // turn on logging if it was turned off
            bool logEnabled = Debug.unityLogger.logEnabled;
            if (!logEnabled)
            {
                Debug.unityLogger.logEnabled = true;
            }

            // get previous setting
            StackTraceLogType logType = Application.GetStackTraceLogType(LogType.Log);

            // turn off stacktrace logging for our messaging
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            Log.Info("\n********************************" +
                  "\n* Tilt Five: Unity SDK Version - " +
                  version +
                  "\n********************************");

            // reset to initial log settings
            Application.SetStackTraceLogType(LogType.Log, logType);

            // reset logging enabled to previous
            Debug.unityLogger.logEnabled = logEnabled;
        }

        public static bool GetGlassesAvailability()
        {
            return Instance.GetGlassesAvailabilityImpl();
        }

        bool GetGlassesAvailabilityImpl()
        {
            return NativePlugin.RefreshGlassesAvailable() == 0;
        }

        public static bool GetHeadPose(ref Vector3 position, ref Quaternion rotation)
        {
            return Instance.GetHeadPoseImpl(ref position, ref rotation);
        }

        bool GetHeadPoseImpl(ref Vector3 position, ref Quaternion rotation)
        {
            int result = 1;
            try
            {
                result = NativePlugin.GetGlassesPose(_headRotation, _headPosition);
                if(0 == result)
                {
                    position = new Vector3(_headPosition[0], _headPosition[1], _headPosition[2]);
                    rotation = new Quaternion(_headRotation[0], _headRotation[1], _headRotation[2], _headRotation[3]);
                }
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 == result);
        }

        static public bool PresentStereoImages(
                IntPtr leftTexHandle,
                IntPtr rightTexHandle,
                int texWidth_PIX,
                int texHeight_PIX,
                bool isSrgb,
                float fovYDegrees,
                float widthToHeightRatio,
                Quaternion rotToUGBL_ULVC,
                Vector3 posULVC_UGBL,
                Quaternion rotToUGBL_URVC,
                Vector3 posURVC_UGBL) {
            return Instance.PresentStereoImagesImpl(leftTexHandle,
                                                    rightTexHandle,
                                                    texWidth_PIX,
                                                    texHeight_PIX,
                                                    isSrgb,
                                                    fovYDegrees,
                                                    widthToHeightRatio,
                                                    rotToUGBL_ULVC,
                                                    posULVC_UGBL,
                                                    rotToUGBL_URVC,
                                                    posURVC_UGBL);
        }

        bool PresentStereoImagesImpl(
                IntPtr leftTexHandle,
                IntPtr rightTexHandle,
                int texWidth_PIX,
                int texHeight_PIX,
                bool isSrgb,
                float fovYDegrees,
                float widthToHeightRatio,
                Quaternion rotToUGBL_ULVC,
                Vector3 posULVC_UGBL,
                Quaternion rotToUGBL_URVC,
                Vector3 posURVC_UGBL)
        {
            float startY_VCI = -Mathf.Tan(fovYDegrees * (0.5f * Mathf.PI / 180.0f));
            float startX_VCI = startY_VCI * widthToHeightRatio;
            float width_VCI  = -2.0f * startX_VCI;
            float height_VCI = -2.0f * startY_VCI;

            _rotToULVC_UGBL[0] = -rotToUGBL_ULVC.x;
            _rotToULVC_UGBL[1] = -rotToUGBL_ULVC.y;
            _rotToULVC_UGBL[2] = -rotToUGBL_ULVC.z;
            _rotToULVC_UGBL[3] = rotToUGBL_ULVC.w;

            _posULVC_UGBL[0] = posULVC_UGBL.x;
            _posULVC_UGBL[1] = posULVC_UGBL.y;
            _posULVC_UGBL[2] = posULVC_UGBL.z;

            _rotToURVC_UGBL[0] = -rotToUGBL_URVC.x;
            _rotToURVC_UGBL[1] = -rotToUGBL_URVC.y;
            _rotToURVC_UGBL[2] = -rotToUGBL_URVC.z;
            _rotToURVC_UGBL[3] = rotToUGBL_URVC.w;

            _posURVC_UGBL[0] = posURVC_UGBL.x;
            _posURVC_UGBL[1] = posURVC_UGBL.y;
            _posURVC_UGBL[2] = posURVC_UGBL.z;

            int result = 0;
            try
            {
                result = NativePlugin.QueueStereoImages(leftTexHandle,
                                                        rightTexHandle,
                                                        (ushort) texWidth_PIX,
                                                        (ushort) texHeight_PIX,
                                                        isSrgb,
                                                        startX_VCI,
                                                        startY_VCI,
                                                        width_VCI,
                                                        height_VCI,
                                                        _rotToULVC_UGBL,
                                                        _posULVC_UGBL,
                                                        _rotToURVC_UGBL,
                                                        _posURVC_UGBL);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            if (result != 0) {
                return false;
            }

            GL.IssuePluginEvent(_sendFrameCallback, 0);

            return true;
        }

        public static bool GetDisplayDimensions(ref Vector2Int displayDimensions)
        {
            return Instance.GetDisplayDimensionsImpl(ref displayDimensions);
        }

        private bool GetDisplayDimensionsImpl(ref Vector2Int displayDimensions)
        {
            int result = 1;
            try
            {
                result = NativePlugin.GetMaxDisplayDimensions(_displaySettings);

                if(result == 0)
                {
                    displayDimensions = new Vector2Int(_displaySettings[0], _displaySettings[1]);
                }
                else Log.Warn("Plugin.cs: Failed to retrieve display settings from plugin.");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 == result);
        }
    }

    public class DisplayHelper
    {
        private static Matrix4x4 Frustum(ARProjectionFrustum f)
        {
            return Frustum(f.m_Left, f.m_Right, f.m_Bottom, f.m_Top, f.m_Near, f.m_Far);
        }

        /***********************************************************************
         * This is our interpretation of glFrustum. CalculateObliqueMatrix
         * has some params that I haven't figured out yet, so I'll use this
         * instead.
        ***********************************************************************/
        public static Matrix4x4 Frustum(float L, float R, float B, float T, float n, float f)
        {
            Matrix4x4 m = new Matrix4x4();

            m[0, 0] = (2 * n) / (R - L);
            m[1, 1] = (2 * n) / (T - B);
            m[0, 2] = (R + L) / (R - L);
            m[1, 2] = (T + B) / (T - B);
            m[2, 2] = -(f + n) / (f - n);
            m[2, 3] = -(2 * f * n) / (f - n);
            m[3, 2] = -1.0f;
            m[3, 3] = 0.0f;

            return m;
        }
    }
}
