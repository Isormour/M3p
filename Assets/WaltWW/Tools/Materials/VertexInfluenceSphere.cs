

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

namespace WaltWW
{
	[ExecuteInEditMode]

	public class VertexInfluenceSphere : MonoBehaviour {
	    #if UNITY_EDITOR

		public int color;
		public float maxValue = 0.5f;
		public AnimationCurve curve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		public float radius = 4f;
		public bool autoRefresh = false;

		private Vector3 storedPosition;
		private float storedRadius;
		public bool externalRefresh = false;
		private float storedMaxValue;
		private Transform storedPartent;

	    private string gizmoTexture = "CaveDungeon/VertexInfluenceSphereEnvironment.png";

	    public Bounds GetBounds()
	    {
			return new Bounds( transform.position, Vector3.one * radius * 2 );
	    }

	    public static void CreateVTXSphere()
	    {
	        //Create the object
	        GameObject vtxSphere = new GameObject();
	        //Name the object
	        vtxSphere.name = "VertexInfluenceSphere";
	        //Add the relevant script
	        vtxSphere.AddComponent<VertexInfluenceSphere>();
	        VertexInfluenceSphere vInfluenceSphere = vtxSphere.GetComponent<VertexInfluenceSphere>();
	        vInfluenceSphere.enabled = true;
	        //Place the object properly
	        if( Selection.activeTransform != null )
	        {
	            vtxSphere.transform.position = Selection.activeTransform.position;
	        }
	        else
	        {
	            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
	            Vector3 objectSpawnPoint = sceneCamera.transform.position + ( sceneCamera.transform.forward * 6 );
	            vtxSphere.transform.position = objectSpawnPoint;
	        }
	        //Select the object
	        GameObject[] newSelection = new GameObject[1];
	        newSelection[0] = vtxSphere;
	        Selection.objects = newSelection;
	        //Make this creation undoable
	        Undo.RegisterCreatedObjectUndo( vtxSphere, "Created Vertex Influence Sphere" );
	    }

		//Determines what changes when a new color is selected
		public void SetNewColor(int colorIndex)
		{
			if (colorIndex == 0) {
				color = 0;
			    gizmoTexture = "CaveDungeon/VertexInfluenceSphereEnvironment.png";
			}
			if (colorIndex == 1) {
				color = 1;
	            gizmoTexture = "CaveDungeon/VertexInfluenceSphereWetness.png";
			}
			if (colorIndex == 2) {
				color = 2;
			}
		}

		//Drawing gizmos for influence spheres
		void OnDrawGizmos() {
			Gizmos.DrawIcon(transform.position, gizmoTexture, true);
		}
		void OnDrawGizmosSelected() {
			Gizmos.color = new Color (0.9f, 0.6f, 0.6f, 0.35f);
			Gizmos.DrawWireSphere(transform.position, radius);
	    }

	    #endif
	}
}

