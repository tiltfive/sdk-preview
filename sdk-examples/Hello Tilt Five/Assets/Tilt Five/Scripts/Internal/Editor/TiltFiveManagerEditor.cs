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
using UnityEngine;
using UnityEditor;


namespace TiltFive
{
    [CustomEditor(typeof(TiltFiveManager))]
    public class TiltFiveManagerEditor : Editor
    {
		#region Properties		

        SerializedObject previousObject = null;

        // Glasses View
        SerializedProperty glassesFOVProperty = null,
                        overrideFOVProperty = null,
                        headPoseCameraProperty = null,
                        glassesMirrorModeProperty = null,
                        gameBoardProperty = null,
                        scaleRatioProperty = null,
                        physicalUnitsProperty = null;

        //Logging and Tilt Five XR View
        SerializedProperty logLevelProperty = null,
                        logTagProperty = null;
        GUIContent[] logLevelOptions = {
                    new GUIContent("VERBOSE"),
                    new GUIContent("DEBUG"),
                    new GUIContent("INFO"),
                    new GUIContent("WARN"),
                    new GUIContent("ERROR"),
                    new GUIContent("DISABLED"),};

        SerializedProperty activePanelProperty = null;

        public enum PanelView { GlassesConfig, EditorConfig };
        PanelView view = PanelView.GlassesConfig;

		#endregion Properties


		#region Unity Editor Functions

        public override void OnInspectorGUI()
        {
            // cache the properties on object change
            if (previousObject != serializedObject && serializedObject != null)
            {
                CacheSerializedProperties();
            }

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            //HEADER - Panel View Select
            DrawButtonHeader();

            if (PanelView.GlassesConfig == view) { DrawGlassesView(); }

            if (PanelView.EditorConfig == view) { DrawToolsView(); }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            previousObject = serializedObject;
        }

		#endregion Unity Editor Functions


		#region Tab Drawing

		private void DrawButtonHeader()
		{
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.margin = new RectOffset(0, 0, 2, 2);
			buttonStyle.padding = new RectOffset(0, 0, 2, 2);
			buttonStyle.border = new RectOffset(
				2, //buttonStyle.border.left, 
				2, //buttonStyle.border.right , 
				2,//buttonStyle.border.top, 
				2);//buttonStyle.border.bottom);

			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			if (true == GUILayout.Toggle((0 == activePanelProperty.intValue), "Glasses", buttonStyle))
			{
				view = PanelView.GlassesConfig;
				activePanelProperty.intValue = 0;
			}
			if (true == GUILayout.Toggle((2 == activePanelProperty.intValue), "Tools", buttonStyle))
			{
				view = PanelView.EditorConfig;
				activePanelProperty.intValue = 2;
			}
			EditorGUILayout.EndHorizontal();


			//EditorGUILayout.LabelField ("", GUI.skin.horizontalSlider);
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
			GUILayout.Space(8);
		}

        private void DrawGlassesView()
        {
            DrawHeadPoseCameraField();
            DrawGlassesMirrorModeField();
            DrawGlassesFOVField();
            EditorGUILayout.Space();

            DrawGameBoardField();
            EditorGUILayout.Space();

            DrawContentScaleField();
			EditorGUILayout.Space();

            if (EditorApplication.isPlaying) { DrawGlassesAvailabilityLabel(); }
        }

        private void DrawToolsView()
        {
            DrawLoggingFields();
        }

		#endregion Tab Drawing


		#region Helper Functions

        private void CacheSerializedProperties()
        {
            // Glasses FOV
            glassesFOVProperty = serializedObject.FindProperty("glassesSettings.customFOV");
            overrideFOVProperty = serializedObject.FindProperty("glassesSettings.overrideFOV");

            // Glasses camera
            headPoseCameraProperty = serializedObject.FindProperty("glassesSettings.headPoseCamera");

            // Glasses mirror mode
            glassesMirrorModeProperty = serializedObject.FindProperty("glassesSettings.glassesMirrorMode");

            // Game Board
            gameBoardProperty = serializedObject.FindProperty("glassesSettings.gameBoard");

            // Content scaling				
            scaleRatioProperty = serializedObject.FindProperty("glassesSettings.contentScaleRatio");
            physicalUnitsProperty = serializedObject.FindProperty("glassesSettings.contentScaleUnit");

            logLevelProperty = serializedObject.FindProperty("logSettings.level");
            logTagProperty = serializedObject.FindProperty("logSettings.TAG");

            activePanelProperty = serializedObject.FindProperty("editorSettings.activePanel");
        }

		#endregion Helper Functions


		#region Field Drawing

        private void DrawGlassesFOVField()
        {
            ++EditorGUI.indentLevel;

            overrideFOVProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("Override FOV"), overrideFOVProperty.boolValue);

            if(overrideFOVProperty.boolValue)
            {
                EditorGUILayout.HelpBox("Overriding this value is not recommended - proceed with caution." +
                    System.Environment.NewLine + System.Environment.NewLine +
                    "Changing the FOV value affects the image projected by the glasses, " +
                    "either cropping/shrinking it while boosting sharpness, or expanding it while reducing sharpness. ",
                    MessageType.Warning);

                glassesFOVProperty.floatValue = EditorGUILayout.Slider(
                    new GUIContent("Field of View", "The field of view of the eye cameras. Higher values trade perceived sharpness for increased projection FOV."),
                    glassesFOVProperty.floatValue, GlassesSettings.MIN_FOV, GlassesSettings.MAX_FOV);
            }

            --EditorGUI.indentLevel;
        }

        private void DrawHeadPoseCameraField()
        {
            bool hasCamera = headPoseCameraProperty.objectReferenceValue;

            if (!hasCamera)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Camera assignment. Changing the Camera assignment at runtime is not supported.", MessageType.Warning);
            }
            Rect theCameraRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(headPoseCameraProperty, new GUIContent("Camera"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(theCameraRect, new GUIContent("",
                "The Camera driven by the glasses head tracking system."));
        }

        private void DrawGlassesMirrorModeField()
        {
            ++EditorGUI.indentLevel;

            glassesMirrorModeProperty.enumValueIndex = EditorGUILayout.Popup("Mirror Mode", glassesMirrorModeProperty.enumValueIndex, glassesMirrorModeProperty.enumDisplayNames);

            --EditorGUI.indentLevel;
        }

        private void DrawGameBoardField()
        {
            bool hasGameBoard = gameBoardProperty.objectReferenceValue;

            if (!hasGameBoard)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Game Board assigment.", MessageType.Warning);
            }
            Rect gameBoardRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(gameBoardProperty, new GUIContent("Game Board"));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContentScaleField()
        {
            EditorGUILayout.LabelField("Content Scale");
            ++EditorGUI.indentLevel;
            Rect contentScaleRect = EditorGUILayout.BeginHorizontal();
            scaleRatioProperty.floatValue = Mathf.Clamp(EditorGUILayout.FloatField("1 world space unit:", scaleRatioProperty.floatValue, GUILayout.MinWidth(180f)), 0.0000001f, float.MaxValue);
            physicalUnitsProperty.enumValueIndex = EditorGUILayout.Popup(physicalUnitsProperty.enumValueIndex, physicalUnitsProperty.enumDisplayNames);
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(contentScaleRect, new GUIContent("",
                "Content Scale is a scalar applied to the camera translation to achieve " +
                "the effect of scaling content. Setting this may also require you to adjust " +
                "the camera's near and far clip planes."));
            --EditorGUI.indentLevel;
        }

        private void DrawGlassesAvailabilityLabel()
        {
            Rect statusRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Glasses: {(Glasses.glassesAvailable ? "Ready" : "Unavailable")}");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLoggingFields()
        {
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            Rect logTagRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(logTagProperty, new GUIContent("TAG"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(logTagRect, new GUIContent("",
                "The logging TAG prefixed to each log message."));

            Rect logLevelRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Level");
            GUILayout.Space(-20);
            logLevelProperty.intValue = EditorGUILayout.Popup(logLevelProperty.intValue, logLevelOptions);
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(logLevelRect, new GUIContent("",
                "The logging level."));
            --EditorGUI.indentLevel;
        }

		#endregion Field Drawing
    }
}
