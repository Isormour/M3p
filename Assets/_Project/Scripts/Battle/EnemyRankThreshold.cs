using System;
using UnityEngine;

namespace M3P
{
    /// <summary>Display rank that applies once the effective floor reaches <see cref="MinFloor"/>.</summary>
    [Serializable]
    public struct EnemyRankThreshold
    {
        [Min(1)] public int MinFloor;
        public string RankName;
        [Tooltip("Main model texture for this rank. Empty keeps the prefab texture.")]
        public Texture2D MainTexture;
        [Tooltip("Optional textures for specific prefab materials, applied after MainTexture.")]
        public EnemyMaterialTexture[] MaterialTextures;
        [UnityEngine.Serialization.FormerlySerializedAs("AdditionalBodyMaterial")]
        [Tooltip("Extra material appended to the body for elite encounters. Empty disables the extra layer.")]
        public Material EliteBodyMaterial;
        [Tooltip("Extra material appended to the body for boss encounters. Empty disables the extra layer.")]
        public Material BossBodyMaterial;
    }

    [Serializable]
    public struct EnemyMaterialTexture
    {
        public Material Material;
        public Texture2D Texture;
    }
}
