using System;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public struct StatusVFXParams
    {
        public float OutlineSize;
        public Vector2 OutlineMultNPower;
        public Color OutlineMidColor;
        public Color OutlineHighColor;

        public static StatusVFXParams Off => new StatusVFXParams
        {
            OutlineSize = 0f,
            OutlineMultNPower = Vector2.zero,
            OutlineMidColor = Color.clear,
            OutlineHighColor = Color.clear
        };
    }

    [Serializable]
    public struct StatusPalette
    {
        public EStatusType StatusType;
        public StatusVFXParams Params;
    }
}
