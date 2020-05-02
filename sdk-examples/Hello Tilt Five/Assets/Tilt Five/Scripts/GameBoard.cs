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
using System.Collections.Generic;
using UnityEngine;

namespace TiltFive
{
    /// <summary>
    /// Represents the game board.
    /// </summary>
    [ExecuteInEditMode]
    public class GameBoard : UniformScaleTransform
    {
        #region Public Fields

        /// <summary>
        /// Shows the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Show/Hide the Board Gizmo in the Editor.")]
        public bool ShowGizmo;

        /// <summary>
        /// Sets the opacity of the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Sets the Alpha transparency of the Board Gizmo in the Editor.")]
        [Range(0f, 1f)]
        public float GizmoOpacity = 0.75f;

        #endregion Public Fields


        #region Private Fields

#if UNITY_EDITOR

        /// <summary>
        /// <b>EDITOR-ONLY</b> The board gizmo.
        /// </summary>
		private TableTopGizmo boardGizmo = new TableTopGizmo();

#endif

        #endregion Private Fields


        #region Public Functions

#if UNITY_EDITOR

        /// <summary>
        /// Draws the game board gizmo in the Editor Scene view.
        /// </summary>
		public void DrawGizmo(GlassesSettings glassesSettings)
        {
            UnifyScale();

            if (ShowGizmo)
            {
                boardGizmo.Draw(glassesSettings, GizmoOpacity);
            }
        }

#endif

        #endregion Public Functions
    }
}