using Match3;
using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The burst that plays where a match dropped shards. Owns its per-colour art, so the spawner only
    /// has to say which tile type was cleared and how much it paid.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShardVFX : MonoBehaviour
    {
        [Serializable]
        struct TileTypeAppearance
        {
            public Match3TileTypeDefinition TileType;
            public Color TrailColor;
        }

        [SerializeField] ParticleSystem _particles;

        [Tooltip("Art per tile type. A type listed here overrides the tile's own colour.")]
        [SerializeField] TileTypeAppearance[] _appearanceByTileType;

        [Tooltip("Emit one shard per point earned, so a match of 5 throws two.")]
        [SerializeField] bool _emitOnePerShard = true;

        /// <summary>Dresses the effect for the colour that was matched. Call before the effect plays.</summary>
        public void Setup(Match3TileTypeDefinition tileType, int amount)
        {
            Color color = tileType != null ? tileType.Color : Color.white;

            if (TryGetAppearance(tileType, out TileTypeAppearance appearance))
            {
                color = appearance.TrailColor;
            }

            ApplyParticles(color, amount);
        }

        bool TryGetAppearance(Match3TileTypeDefinition tileType, out TileTypeAppearance appearance)
        {
            appearance = default;

            if (tileType == null || _appearanceByTileType == null)
                return false;

            for (int i = 0; i < _appearanceByTileType.Length; i++)
            {
                if (_appearanceByTileType[i].TileType != tileType)
                    continue;

                appearance = _appearanceByTileType[i];
                return true;
            }

            return false;
        }


        void ApplyParticles(Color color, int amount)
        {
            if (_particles == null)
                return;

            ParticleSystem.MainModule main = _particles.main;
            main.startColor = color;

            if (_emitOnePerShard)
            {
                ParticleSystem.EmissionModule emission = _particles.emission;
                if (emission.burstCount > 0)
                {
                    ParticleSystem.Burst burst = emission.GetBurst(0);
                    burst.count = Mathf.Max(1, amount);
                    emission.SetBurst(0, burst);
                }
            }

            // Restart, otherwise a prefab set to play on awake has already fired its burst uncoloured.
            _particles.Clear(true);
            _particles.Play(true);
        }
    }
}
