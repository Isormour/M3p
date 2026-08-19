using UnityEngine;

namespace Match3
{
    internal class TileArrow : MonoBehaviour
    {
        [SerializeField] SpriteRenderer arrow;

        void Awake()
        {
            ResolveRenderer();
        }

        public void SetEnabled(bool enabled)
        {
            ResolveRenderer();
            if (arrow != null)
                arrow.enabled = enabled;
        }

        void ResolveRenderer()
        {
            if (arrow == null)
                arrow = GetComponent<SpriteRenderer>();
        }
    }
}
