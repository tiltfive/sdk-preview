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

        [Tooltip("Show/Hide the Unit Grid on the Board Gizmo in the Editor.")]
        public bool ShowGrid;

        public float GridHeightOffset = 0f;
        public bool StickyHeightOffset = true;


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

        

        /// <summary>
        /// <b>EDITOR-ONLY</b> The Y offset of the grid, taking snapping into account.
        /// </summary>
        private float gridOffsetY => StickyHeightOffset ? Mathf.RoundToInt(GridHeightOffset) : GridHeightOffset;
        
        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale unit (e.g. inches, cm, snoots, etc) from the glasses settings.
        /// </summary>
        private LengthUnit currentContentScaleUnit;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale value (e.g. 1.0 inch|centimeter|etc) from the glasses settings.
        /// </summary>
        private float currentContentScaleRatio;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current local scale of the attached GameObject's Transform.
        /// </summary>
        private Vector3 currentScale;


        // TODO: Implement separate configurations for LE and XE kits.

        /// <summary>
        /// <b>EDITOR-ONLY</b> The width of the usable area of the game board.
        /// </summary>
        private const float usableGameBoardWidthInMeters = 0.7f;
        /// <summary>
        /// <b>EDITOR-ONLY</b> The length of the usable area of the game board.
        /// </summary>
        private const float usableGameBoardLengthInMeters = 0.7f;
        /// <summary>
        /// <b>EDITOR-ONLY</b> The total width of the game board.
        /// </summary>
        private const float totalGameBoardWidthInMeters = 0.8f;
        /// <summary>
        /// <b>EDITOR-ONLY</b> The total length of the game board.
        /// </summary>
        private const float totalGameBoardLengthInMeters = 0.8f;

        private const float MIN_SCALE = 0.00001f;



#endif // UNITY_EDITOR

        #endregion Private Fields

        
        #region Public Functions

#if UNITY_EDITOR

        new public void Awake()
        {
            base.Awake();            
            currentScale = transform.localScale;
        }

        /// <summary>
        /// Draws the game board gizmo in the Editor Scene view.
        /// </summary>
		public void DrawGizmo(GlassesSettings glassesSettings)
        {
            UnifyScale();

            if (ShowGizmo)
            {
                boardGizmo.Draw(glassesSettings, GizmoOpacity, ShowGrid, 
                totalGameBoardWidthInMeters, totalGameBoardLengthInMeters, 
                usableGameBoardWidthInMeters, usableGameBoardLengthInMeters, gridOffsetY);
            }

            var sceneViewRepaintNecessary = ScaleCompensate(glassesSettings);
            sceneViewRepaintNecessary |= ContentScaleCompensate(glassesSettings);
            
            if(sceneViewRepaintNecessary)
            {
                boardGizmo.ResetGrid(glassesSettings);     // This may need to change once separate game board configs are in.
                UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
        }
        


#endif  // UNITY_EDITOR

        #endregion Public Functions 

        #region Private Functions

#if UNITY_EDITOR

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the game board is scaled.
        ///</summary>
        ///<remarks>
        /// This function enforces a minumum scale value for the attached GameObject transform.
        ///</remarks>
        private bool ScaleCompensate(GlassesSettings glassesSettings)
        {            
            if(currentScale == transform.localScale) { return false; }

            // Prevent negative scale values for the game board.
            if( transform.localScale.x < MIN_SCALE)
            {
                transform.localScale = Vector3.one * MIN_SCALE;
            }

            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            
            sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * glassesSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);
            
            currentScale = transform.localScale;
            return true;
        }

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the content scale is modified.
        ///</summary>
        private bool ContentScaleCompensate(GlassesSettings glassesSettings)
        {
            if(currentContentScaleRatio == glassesSettings.contentScaleRatio 
            && currentContentScaleUnit == glassesSettings.contentScaleUnit) { return false; }

            var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            currentContentScaleUnit = glassesSettings.contentScaleUnit;
            currentContentScaleRatio = glassesSettings.contentScaleRatio;
              
            sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * glassesSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);
            
            return true;
        }
        

#endif  // UNITY_EDITOR

        #endregion Private Functions
    }
}