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

#if UNITY_EDITOR
using UnityEditor;

namespace TiltFive
{
	/// <summary>
	/// The gizmo for the Tilt Five game board.
	/// </summary>
	public class TableTopGizmo {

		private string boardMeshAssetPath = "Assets/Tilt Five/Meshes/BoardGizmo.fbx";
		private string borderMeshChildPath = "Border/Object_1";
		private string surfaceMeshChildPath = "Retro/Object_2";

		private GameObject meshObj;
		private Mesh meshBorder;
		private Mesh meshSurface;

		private void Configure()
		{
			
			if( null == meshObj )
			{
				meshObj = AssetDatabase.LoadAssetAtPath<GameObject>(boardMeshAssetPath);
			}

			if( null == meshBorder )
			{
				var transform = meshObj.transform.Find( borderMeshChildPath );
				if( transform != null )
				{					
					if(transform.TryGetComponent<MeshFilter>(out var meshFilter))
					{
						meshBorder = meshFilter.sharedMesh;
					}
				}
			}

			//if( null != meshSurface )
			{
				var transform = meshObj.transform.Find( surfaceMeshChildPath );
				if( transform != null )
				{
					if(transform.TryGetComponent<MeshFilter>(out var meshFilter))
					{
						meshSurface = meshFilter.sharedMesh;
					}
				}
			}
		}

		public void Draw(GlassesSettings glassesSettings, float alpha)
		{
			Configure ();

			if (null == glassesSettings || null == glassesSettings.headPoseCamera)
				return;



			Vector3 vOriginTranslation = Vector3.zero;
			Quaternion qOriginOrientation = Quaternion.identity;

			Matrix4x4 mtxOrigin = Matrix4x4.TRS( vOriginTranslation, qOriginOrientation, Vector3.one );

			Matrix4x4 mtxWorld = Matrix4x4.TRS( glassesSettings.gameBoardCenter,
				Quaternion.Euler(glassesSettings.gameBoardRotation),
				Vector3.one / (glassesSettings.physicalMetersPerWorldSpaceUnit * glassesSettings.gameBoardScale) );

			Matrix4x4 mtxPreTransform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler(-90.0f, 0.0f, 0.0f), Vector3.one );

			Matrix4x4 mtxGizmo = mtxOrigin * mtxWorld * mtxPreTransform;

			Color oldColor = Gizmos.color;
			Matrix4x4 oldMatrix = Gizmos.matrix;

			Gizmos.matrix = mtxGizmo;

			if( meshBorder != null )
			{
				Gizmos.color = new Color(0.0f, 0.0f, 0.0f, alpha);
				Gizmos.DrawMesh( meshBorder, 0 );
			}

			if( meshSurface != null )
			{
				Gizmos.color = new Color (0.5f, 0.5f, 0.5f, alpha);
				Gizmos.DrawMesh( meshSurface, 0 );
			}

			Gizmos.matrix = oldMatrix;
			Gizmos.color = oldColor;
		}
	}
}
#endif
