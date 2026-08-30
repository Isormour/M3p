using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WaltWW
{
    [ExecuteInEditMode]
    public class MaterialBlendTypeSetter : MonoBehaviour
    {
        public enum MaterialBlendType
        {
            Snow,
            Moss,
            Sand
        }
    
        public MaterialBlendType SceneMaterialBlendType;
        public bool IsDesert;
        
        private void OnValidate()
        {
            SetMaterialBlendType();
        }
    
        private void Awake()
        {
            SetMaterialBlendType();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
    
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetMaterialBlendType();
        }
    
        private void SetMaterialBlendType()
        {
            if (Shader.IsKeywordEnabled ("_CDT_BLENDTYPE_SNOW" )) Shader.DisableKeyword( "_CDT_BLENDTYPE_SNOW" );
            if (Shader.IsKeywordEnabled ("_CDT_BLENDTYPE_MOSS" ))Shader.DisableKeyword( "_CDT_BLENDTYPE_MOSS" );
            if (Shader.IsKeywordEnabled ("_CDT_BLENDTYPE_SAND" ))Shader.DisableKeyword( "_CDT_BLENDTYPE_SAND" );
            
            switch( SceneMaterialBlendType )
            {
                case MaterialBlendType.Snow:
                    Shader.EnableKeyword( "_CDT_BLENDTYPE_SNOW" );
                    break;
                case MaterialBlendType.Moss:
                    Shader.EnableKeyword( "_CDT_BLENDTYPE_MOSS" );
                    break;
                case MaterialBlendType.Sand:
                    Shader.EnableKeyword( "_CDT_BLENDTYPE_SAND" );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    
            if( IsDesert )
            {
                Shader.EnableKeyword( "_ISDESERT" );
            }
            else
            {
                Shader.DisableKeyword( "_ISDESERT" );
            }
            
        }
    }
}

