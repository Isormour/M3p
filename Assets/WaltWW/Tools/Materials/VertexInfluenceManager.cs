

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
	[ImageEffectAllowedInSceneView]

	public class VertexInfluenceManager {

	    #if UNITY_EDITOR

		public static VertexInfluenceManager Instance = new VertexInfluenceManager();

	    public enum ParentedTo { World, Parent, Object };
		public bool startPainting = false;

		public List<VertexInfluenceSphere> vertexInfluenceSpheres = new List<VertexInfluenceSphere>();
		public List<MeshFilter> staticMeshFilters = new List<MeshFilter>();
	    public List<MeshStorage> meshStorages = new List<MeshStorage>();
	    private List<MeshRenderer> MeshRenderers = new List<MeshRenderer>();
		private List<Bounds> staticMeshBounds = new List<Bounds>();
		public ParentedTo parentedTo;

	    public bool areSpheresAutoRefreshing = false;

	    private bool fullExecute = false;

	    public void DetectVertexInfluenceSpheres(){
			//Clears the old list
			vertexInfluenceSpheres.Clear ();

			//Gets every sphere and checks if this is the manager. TODO: Should be for world and object only.
			VertexInfluenceSphere[] allSpheres = GameObject.FindObjectsOfType(typeof(VertexInfluenceSphere)) as VertexInfluenceSphere[];
			foreach (VertexInfluenceSphere sphere in allSpheres) {
				vertexInfluenceSpheres.Add (sphere);
			}
		}

		public void RevertOldMeshes(){
	        //Reverts all the meshed to what is stored
	        for (int i = 0; i < staticMeshFilters.Count; i++){
				if (staticMeshFilters[i] != null){
				    if( staticMeshFilters[i].sharedMesh != null ){
				        if( staticMeshFilters[i].sharedMesh.name == "VPaintedMesh" )
				        {
					        if (!staticMeshFilters[i].TryGetComponent( out MeshStorage _ ) )
					        {
						        Debug.LogError( $"Object {staticMeshFilters[i].gameObject.name} is VPainted but has no mesh storage", staticMeshFilters[i].gameObject  );
					        }

					        if( meshStorages[i] == null )
					        {
						        Debug.LogError( $"Object {staticMeshFilters[i].gameObject.name} is VPainted but has no mesh storage", staticMeshFilters[i].gameObject  );
					        }
				            staticMeshFilters[i].sharedMesh = meshStorages[i].originalMesh;
				        }
				    }
				}
			}
		}

		public void GetStaticMeshes(){
			//Clears the old lists
			staticMeshFilters.Clear ();
			meshStorages.Clear ();
			staticMeshBounds.Clear ();
			MeshRenderers.Clear();

			//Gets all mesh storages and gets their data
	        MeshStorage[] allStorages = GameObject.FindObjectsOfType(typeof(MeshStorage)) as MeshStorage[];
	        foreach (MeshStorage meshStorage in allStorages)
	        {
	            if( !meshStorage.ignoreVertexInfluenceSpheres )
	            {
	                MeshFilter meshFilter = meshStorage.GetComponent<MeshFilter>();
	                MeshRenderer meshRender = meshStorage.GetComponent<MeshRenderer>();
	                staticMeshFilters.Add( meshFilter );
	                meshStorages.Add( meshStorage );
	                MeshRenderers.Add( meshRender );
	                meshRender.additionalVertexStreams = null;
	                staticMeshBounds.Add( meshRender.bounds );
	                meshFilter.sharedMesh = meshStorage.originalMesh;
	            }
	        }
	    }

		public void PaintMeshes(){
	        //This makes the undo state right before we actually paint stuff.
	        MeshFilter[] undoObjects = staticMeshFilters.ToArray();
	        Undo.RecordObjects(undoObjects, "Meshes Vertexes Painted");

	        for( int j = 0; j < staticMeshFilters.Count; ++j )
	        {
		        EditorUtility.DisplayProgressBar( "Painting meshes", $"Mesh {j}/{staticMeshFilters.Count}", (float)j/(float)staticMeshFilters.Count );
		        if (staticMeshFilters[j].sharedMesh != null){
					Mesh modifiedMesh;
					modifiedMesh = GameObject.Instantiate (staticMeshFilters[j].sharedMesh);

					Vector3[] vertices = modifiedMesh.vertices;
	                Color[] colors = modifiedMesh.colors;
					Transform objectTransform = staticMeshFilters[j].transform;
					
					modifiedMesh.name = "VPaintedMesh";

					//This sets the colors of this mesh to be the proper painted values
					for (int z = 0; z < vertexInfluenceSpheres.Count; z++){
						
						//Test if this sphere is relevant.
						if( !vertexInfluenceSpheres[z].GetBounds().Intersects( staticMeshBounds[j] ) )
						{
							continue;
						}
						
						int i = 0;
						Vector3 vtxSpherePos = vertexInfluenceSpheres[z].transform.position;
						float vtxSphereRadius = vertexInfluenceSpheres[z].radius;
						int vtxSphereColor = vertexInfluenceSpheres[z].color;
						float vtxSphereMaxValue = vertexInfluenceSpheres[z].maxValue;
						AnimationCurve vtxSphereAnimationCurve = vertexInfluenceSpheres[z].curve;
	                    //Debug for broken meshes
					    if( vertices.Length == colors.Length )
					    {
					        while( i < vertices.Length )
					        {

					            //The maths for linking the Vertex Influence Sphere's fields to the actual painting process
					            Vector3 worldVertex = objectTransform.TransformPoint( vertices[i] );
					            float value = Vector3.Distance( worldVertex, vtxSpherePos ) / vtxSphereRadius;
					            value = Mathf.Clamp01( value );
					            value = Mathf.Abs( 1 - value );
					            if( value > 0f && value < 1f )
					            {
					                value = vtxSphereAnimationCurve.Evaluate( value );
					            }
					            value = Mathf.Clamp( value, 0, vtxSphereMaxValue );

					            //Changes the vertices depending on if it's supposed to be the red, green or blue channel
					            if( vtxSphereColor == 0 )
					            {
					                colors[i] = new Color( Mathf.Max( value, colors[i].r ), colors[i].g, colors[i].b, colors[i].a );
					            }
					            if( vtxSphereColor == 1 )
					            {
					                colors[i] = new Color( colors[i].r, Mathf.Max( value, colors[i].g ), colors[i].b, colors[i].a );
					            }
					            if( vtxSphereColor == 2 )
					            {
					                colors[i] = new Color( colors[i].r, colors[i].g, Mathf.Max( value, colors[i].b ), colors[i].a );
					            }
					            i++;
					        }
					    }
					    else
					    {
					        Transform debugParent = staticMeshFilters[j].transform.parent;
					        if( debugParent != null )
					        {
	                            Debug.LogAssertion( debugParent.name + "'s child does not have the same vertex color count as it's vertex count", staticMeshFilters[j].sharedMesh );
					        }
					        else
					        {
	                            Debug.LogAssertion( staticMeshFilters[j].transform.name + "does not have the same vertex color count as it's vertex count", staticMeshFilters[j].sharedMesh );
					        }
					    }
					}

					//Once we're done with everything, lets put the color into our new mesh and replace the old mesh
					modifiedMesh.colors = colors;

					staticMeshFilters[j].sharedMesh = modifiedMesh;
					//MeshRenderers[j].additionalVertexStreams = modifiedMesh;
				}
	        }
	        
	        EditorUtility.ClearProgressBar();
		}

		public void StartPaintingRaw()
		{
		    if( !Application.isPlaying )
		    {
		        //Runs all the functions to paint everything from scratch
		        fullExecute = true;
		        double time = EditorApplication.timeSinceStartup;
		        DetectVertexInfluenceSpheres();
		        GetStaticMeshes();
		        RevertOldMeshes();
		        PaintMeshes();

		        fullExecute = false;
		        Debug.Log( $"Painted {staticMeshFilters.Count} meshes and {vertexInfluenceSpheres.Count} spheres, with a total of {vertexInfluenceSpheres.Count * staticMeshFilters.Count} operations. Done in {EditorApplication.timeSinceStartup - time} seconds." );
		    }
		}
		

	    public static void PaintAll()
	    {
		    if( Instance == null )
		    {
			    Instance = new VertexInfluenceManager();
		    }
		    Instance.StartPaintingRaw();
	    }
	    
	    public static void RevertAll()
	    {
		    if( Instance == null )
		    {
			    Instance = new VertexInfluenceManager();
		    }
		    Instance.GetStaticMeshes();
		    Instance.RevertOldMeshes();
	    }
	#endif
	}
}



