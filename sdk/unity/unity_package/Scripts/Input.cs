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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TiltFive.Logging;
using TiltFive;

namespace TiltFive
{
    /// <summary>
    /// The button states for the wand at a moment in time.
    /// </summary>
    internal struct WandControlsState
    {
        public Int64 Timestamp;

        public bool System;
        public bool One;
        public bool Two;
        public bool Y;
        public bool B;
        public bool A;
        public bool X;
        public bool Z;

        public Vector2 Stick;
        public float Trigger;

        public WandControlsState(Int64 timestamp, UInt32 buttons, Vector2 stick, float trigger)
        {
            Timestamp = timestamp;

            System  = (buttons & (UInt32)Input.WandButton.System)   == (UInt32)Input.WandButton.System;
            One     = (buttons & (UInt32)Input.WandButton.One)      == (UInt32)Input.WandButton.One;
            Two     = (buttons & (UInt32)Input.WandButton.Two)      == (UInt32)Input.WandButton.Two;
            Y       = (buttons & (UInt32)Input.WandButton.Y)        == (UInt32)Input.WandButton.Y;
            B       = (buttons & (UInt32)Input.WandButton.B)        == (UInt32)Input.WandButton.B;
            A       = (buttons & (UInt32)Input.WandButton.A)        == (UInt32)Input.WandButton.A;
            X       = (buttons & (UInt32)Input.WandButton.X)        == (UInt32)Input.WandButton.X;
            Z       = (buttons & (UInt32)Input.WandButton.Z)        == (UInt32)Input.WandButton.Z;
            
            Stick = new Vector2(stick[0], stick[1]);
            Trigger = trigger;
        }

        public bool GetButtonState(Input.WandButton button)
        {
            switch (button)
            {
                case Input.WandButton.System:
                    return System;
                case Input.WandButton.One:
                    return One;
                case Input.WandButton.Two:
                    return Two;
                case Input.WandButton.Y:
                    return Y;
                case Input.WandButton.B:
                    return B;
                case Input.WandButton.A:
                    return A;
                case Input.WandButton.X:
                    return X;
                default:
                    return Z;
            }
        }

        public readonly static WandControlsState DefaultState = new WandControlsState(0, 0, Vector2.zero, 0f);
    }

    public static class Input
    {
        #region Private Fields

        private static Dictionary<WandTarget, WandControlsState> currentWandStates;
        private static Dictionary<WandTarget, WandControlsState> previousWandStates;

        // Scan for new wands every half second.
        private static DateTime lastScanAttempt = System.DateTime.MinValue;

        // This should likely become a query into the native library.
        private static readonly double wandScanRate = 0.5d;

        #endregion


        #region Public Enums

        public enum WandButton : UInt32
        {
            System  = 1 << 0,
            One     = 1 << 1,
            Two     = 1 << 2,
            Y       = 1 << 3,
            B       = 1 << 4,
            A       = 1 << 5,
            X       = 1 << 6,
            Z       = 1 << 7
        }

        /// <summary>
        /// Since wands are all physically identical (they have no "handedness"), it doesn't make sense to address them using "left" or "right". 
        /// Instead we use hand dominance, and allow applications to swap the dominant and offhand wand according to the user's preference.
        /// </summary>
        public enum WandTarget : Int32
        {
            /// <summary>
            /// The wand held in the player's dominant hand.
            /// </summary>
            Primary,

            /// <summary>
            /// The wand held in the player's non-dominant hand.
            /// </summary>
            Secondary
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Whether the indicated wand button is currently being pressed.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="targetWand">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <returns>Returns true if the button is being pressed.</returns>
        public static bool GetButton(WandButton button, WandTarget targetWand = WandTarget.Primary)
        {
            var wandState = currentWandStates[targetWand];

            return wandState.GetButtonState(button);
        }

        /// <summary>
        /// Whether the indicated wand button was pressed during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="targetWand">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <returns>Returns true if the button was pressed during this frame.</returns>
        public static bool GetButtonDown(WandButton button, WandTarget targetWand = WandTarget.Primary)
        {
            var wandState = currentWandStates[targetWand];
            var previousWandState = previousWandStates[targetWand];

            // Return true if the button is currently pressed, but was unpressed on the previous frame.
            return wandState.GetButtonState(button) && !previousWandState.GetButtonState(button);
        }

        /// <summary>
        /// Whether the indicated wand button was released during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="targetWand">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <returns>Returns true if the button was released this frame.</returns>
        public static bool GetButtonUp(WandButton button, WandTarget targetWand = WandTarget.Primary)
        {
            var wandState = currentWandStates[targetWand];
            var previousWandState = previousWandStates[targetWand];

            // Return true if the button is currently released, but was pressed on the previous frame.
            return !wandState.GetButtonState(button) && previousWandState.GetButtonState(button);
        }

        /// <summary>
        /// Gets the direction and magnitude of the stick's tilt for the indicated wand.
        /// </summary>
        /// <param name="targetWand"></param>
        public static Vector2 GetStickAxis(WandTarget targetWand = WandTarget.Primary)
        {
            return currentWandStates[targetWand].Stick;
        }

        /// <summary>
        /// Gets the analog value for the trigger, from 0.0 (released) to 1.0 (fully depressed).
        /// </summary>
        /// <param name="targetWand"></param>
        public static float GetTrigger(WandTarget targetWand = WandTarget.Primary)
        {
            return currentWandStates[targetWand].Trigger;
        }

        // TODO: We may want to change this to something like "GetWandStatus()",
        // returning a flags enum with options like "ready, disconnected, batteryLow" etc.
        public static bool GetWandAvailability(Input.WandTarget targetWand = Input.WandTarget.Primary)
        {
            bool wandAvailable = false;

            try
            {
                int result = NativePlugin.GetWandAvailability(ref wandAvailable, targetWand);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return wandAvailable;
        }

        public static bool SwapWandHandedness()
        {
            int result = 1;

            try
            {
                result = NativePlugin.SwapWandHandedness();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 == result);
        }

        #endregion


        #region Internal Functions

        internal static void Update()
        {
            var currentTime = System.DateTime.Now;
            var timeSinceLastScan = currentTime - lastScanAttempt;

            // Scan for wands if necessary.
            // TODO: Implement more robust disconnect detection, communicate wand availability events to users, offer user option to swap wands.
            if(timeSinceLastScan.TotalSeconds >= wandScanRate
                && (!GetWandAvailability(WandTarget.Primary) || !GetWandAvailability(WandTarget.Secondary)))
            {
                ScanForWands();
                lastScanAttempt = currentTime;
            }

            // Query the native plugin for wand states since the previous frame.
            var primaryWandControlsState = new WandControlsState();
            var secondaryWandControlsState = new WandControlsState();

            var previousPrimaryWandState = currentWandStates[WandTarget.Primary];
            var previousSecondaryWandState = currentWandStates[WandTarget.Secondary];

            // Get the state of the wand held in the user's dominant hand.
            if (GetWandControlsState(ref primaryWandControlsState, WandTarget.Primary))
            {
                currentWandStates[WandTarget.Primary] = primaryWandControlsState;
            }
            else Log.Verbose("Failed to obtain state for the primary wand.");

            // Get the state of the wand held in the user's non-dominant hand.
            if (GetWandControlsState(ref secondaryWandControlsState, WandTarget.Secondary))
            {
                currentWandStates[WandTarget.Secondary] = secondaryWandControlsState;
            }
            else Log.Verbose("Failed to obtain state for the secondary wand.");

            previousWandStates[WandTarget.Primary] = previousPrimaryWandState;
            previousWandStates[WandTarget.Secondary] = previousSecondaryWandState;
        }

        internal static bool ScanForWands()
        {
            int result = 1;

            try
            {
                result = NativePlugin.ScanForWands();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 == result);
        }

        internal static bool GetWandControlsState(ref WandControlsState wandButtonsState, Input.WandTarget targetWand = Input.WandTarget.Primary)
        {
            int result = 1;

            try
            {
                UInt32 buttons = 0;
                float[] stick = new float[2];
                float trigger = 0f;
                Int64 timestamp = 0;

                result = NativePlugin.GetControllerState(targetWand, ref buttons, stick, ref trigger, ref timestamp);

                wandButtonsState = new WandControlsState(timestamp, buttons, new Vector2(stick[0], stick[1]), trigger);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }


            return (0 == result);
        }

        #endregion


        #region Private Functions

        static Input()
        {
            // Query the native plugin for the max wand count and initialize the wand state queues.
            currentWandStates = new Dictionary<WandTarget, WandControlsState>() {
                { WandTarget.Primary, WandControlsState.DefaultState },
                { WandTarget.Secondary, WandControlsState.DefaultState }
            };

            previousWandStates = new Dictionary<WandTarget, WandControlsState>(){
                { WandTarget.Primary, WandControlsState.DefaultState },
                { WandTarget.Secondary, WandControlsState.DefaultState }
            };

            ScanForWands();
        }

        #endregion
    }

}
