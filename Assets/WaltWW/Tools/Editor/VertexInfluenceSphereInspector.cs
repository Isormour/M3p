using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace WaltWW
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(VertexInfluenceSphere))]
	public class VertexInfluenceSphereInspector : Editor {

		public enum ColorType { Environmental, Wetness};

	    private ColorType colorType = ColorType.Environmental;
	    private ColorType storedColorType = ColorType.Wetness;

	    // Add menu item for creating a VertexInfluenceSphere
		[MenuItem("GameObject/VertexInfluenceSphere")]
		public static void CreateVTXSphere()
		{
			VertexInfluenceSphere.CreateVTXSphere();
		}

		public override void OnInspectorGUI()
		{
			//Overwrite the vanilla inspector
			VertexInfluenceSphere inspector = (VertexInfluenceSphere)target;

	        //Fixes color enum from reseting
		    if( inspector.color == 0 )
		    {
		        colorType = ColorType.Environmental;
		    }
		    else
		    {
		        colorType = ColorType.Wetness;
		    }

		    //Starts a listener for if anything is changed in the GUI
			EditorGUI.BeginChangeCheck ();

			//Any changes that happen to any of these fields is undoable
			Undo.RecordObject (inspector, "Vertex Influence Sphere modified");

			//The new fields that overwrote the old ones
			colorType = (ColorType)EditorGUILayout.EnumPopup ("Type", colorType);
			inspector.radius = EditorGUILayout.FloatField ("Radius", inspector.radius);
			inspector.maxValue = EditorGUILayout.Slider ("Max Value", inspector.maxValue, 0, 1);
			inspector.curve = EditorGUILayout.CurveField ("Falloff Curve", inspector.curve);

			//Paint
	        if( GUILayout.Button( "Paint" ) )
	        {
		        VertexInfluenceManager.PaintAll();
	        }
	        
	        if( GUILayout.Button( "Revert All" ) )
	        {
		        VertexInfluenceManager.RevertAll();
	        }

			//If the GUI was changed, do this
			if (EditorGUI.EndChangeCheck ()) {
				inspector.externalRefresh = true;
				EditorUtility.SetDirty (inspector);
			}

			//Changes elements of the inspector and VIS based on the color type.
			if (colorType != storedColorType) {
				int colorIndex = (int)(colorType);
				inspector.SetNewColor (colorIndex);
				storedColorType = colorType;
			}
		}

	}
}

