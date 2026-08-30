

using UnityEngine;


namespace WaltWW
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]

    public class MeshStorage : MonoBehaviour
    {
    
#if UNITY_EDITOR
        public Mesh originalMesh;
        public bool ignoreVertexInfluenceSpheres = false;

        void OnValidate()
        {
            if( !Application.isPlaying )
            {
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                if( meshFilter == null ) ErrorTrack();
                if (meshFilter.sharedMesh == null) ErrorTrack();
                if (meshFilter.sharedMesh.name != "VPaintedMesh")
                {
                    originalMesh = meshFilter.sharedMesh;
                }
            }
        }

        private void ErrorTrack()
        {
            Debug.LogError( $"{this.gameObject} has a nullref. Scene is {gameObject.scene.buildIndex}", this.gameObject );
            Debug.LogError( $"Parent = {this.gameObject.transform.parent}" );
        }

#endif
    }
}



