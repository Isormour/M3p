using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaltWW
{
    public class SceneManager : MonoBehaviour
    {
        private static bool EnableUi = true;

        public List<GameObject> EnabledObjects = new List<GameObject>();
    
        private void OnGUI()
        {
            if( Input.GetKeyDown( KeyCode.Escape ) )
            {
                EnableUi = true;
            }
        
            if (!EnableUi) return;
        
            GUILayout.BeginVertical(  );

            for( int i = 0; i < EnabledObjects.Count; ++i )
            {
                if( GUILayout.Button( EnabledObjects[i].name) )
                {
                    DisableAll();
                    EnabledObjects[i].SetActive( true );
                    EnableUi = false;
                }
            }
            GUILayout.EndVertical();
        }

        private void DisableAll()
        {
            for( int i = 0; i < EnabledObjects.Count; ++i )
            {
                EnabledObjects[i].SetActive( false );
            }
        }
    }
}

