using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WaltWW
{
    [ExecuteInEditMode]
    public class LightMultiplier : MonoBehaviour
    {
#if UNITY_EDITOR
        public float Multiplier = 0.5f;
        public bool Apply;

        private void OnValidate()
        {
            if( Apply )
            {
                Apply = false;

                Light[] lights = FindObjectsOfType<Light>();
            
                Undo.RecordObjects( lights, "changed intensity" );

                for( int i = 0; i < lights.Length; ++i )
                {
                    lights[i].intensity *= Multiplier;
                }
            }
        }
#endif
    }

}

