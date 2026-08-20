using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Preview of a tile that a Recolor will turn into another colour. Uses the prefab's ghost
    /// material and the destination type's sprite; the extra sprite's material takes that type's colour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TileGhost : MonoBehaviour
    {
        static readonly int GhostId = Shader.PropertyToID("_Ghost");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColId = Shader.PropertyToID("_Col");

        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] SpriteRenderer _colorRenderer;

        MaterialPropertyBlock _block;

        void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            if (_colorRenderer == null)
                _colorRenderer = FindColorRenderer();

            _block = new MaterialPropertyBlock();
        }

        public void Present(Vector3 worldPosition, Sprite sprite, Color color)
        {
            if (_block == null)
                _block = new MaterialPropertyBlock();

            transform.position = worldPosition;
            gameObject.SetActive(true);

            if (_renderer != null)
            {
                _renderer.sprite = sprite;
                _renderer.enabled = sprite != null;
                ApplyGhostMaterial(sprite);
            }

            ApplyColor(color);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void ApplyGhostMaterial(Sprite sprite)
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(GhostId, 1f);

            Texture texture = sprite != null ? sprite.texture : null;
            if (texture != null)
                _block.SetTexture(MainTexId, texture);

            _renderer.SetPropertyBlock(_block);
        }

        void ApplyColor(Color color)
        {
            if (_colorRenderer == null)
                return;

            color.a = 0.5f;
            _colorRenderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColId, color);
            _colorRenderer.SetPropertyBlock(_block);
        }

        SpriteRenderer FindColorRenderer()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i] != _renderer)
                    return renderers[i];
            }

            return null;
        }
    }
}
