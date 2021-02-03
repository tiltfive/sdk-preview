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
using System.Runtime.InteropServices;

namespace TiltFive
{
    public class NativePlugin
    {

#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
        public const string PLUGIN_LIBRARY = @"__Internal";
#else
        public const string PLUGIN_LIBRARY = @"TiltFiveUnity";
#endif

        // Glasses Availability
        public static int RefreshGlassesAvailable()
        {
            // Implementation will be included in a future non-preview version of the SDK.
            // For now, this function will return a result consistent with an unavailable HMD.
            return 1;
        }

        // Head Pose
        public static int GetGlassesPose(
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] rotToGLS_WRLD,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] float[] posGLS_WRLD)
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        // Wand Availability
        public static int GetWandAvailability(
            ref bool wandAvailable,
            [MarshalAs(UnmanagedType.I4)] Input.WandTarget wandTarget)
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        // Scan for Wands
        public static int ScanForWands()
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        // Swap Wand Handedness
        public static int SwapWandHandedness()
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        // Wand Controls State
        public static int GetControllerState(
            [MarshalAs(UnmanagedType.I4)] Input.WandTarget wandTarget,
            ref UInt32 buttons,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] float[] stick,
            ref float trigger,
            ref Int64 timestamp)
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        // Submit Render Textures
        public static int QueueStereoImages(
                System.IntPtr leftEyeTextureHandle,
                System.IntPtr rightEyeTextureHandle,
                ushort texWidth_PIX,
                ushort texHeight_PIX,
                bool isSrgb,
                float startX_VCI,
                float startY_VCI,
                float width_VCI,
                float height_VCI,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] rotToULVC_UGBL,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] float[] posULVC_UGBL,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] rotToURVC_UGBL,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] float[] posURVC_UGBL)
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return 0;
        }

        public static IntPtr GetSendFrameCallback()
        {
            // Implementation will be included in a future non-preview version of the SDK.
            return IntPtr.Zero;
        }

        public static int GetMaxDisplayDimensions(
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] int[] displayDimensions)
        {
            // Implementation will be included in a future non-preview version of the SDK.
            displayDimensions[0] = 2432;
            displayDimensions[1] = 768;
            return 0;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        public static void RegisterPlugin()
        {
            // Implementation will be included in a future non-preview version of the SDK.
        }
#endif

    }
}
