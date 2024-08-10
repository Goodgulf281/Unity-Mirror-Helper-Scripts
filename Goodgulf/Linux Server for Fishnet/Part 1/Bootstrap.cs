using System;
using System.Collections;
using System.Collections.Generic;
using Goodgulf.Azure;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Goodgulf.SceneWorkflow 
{
    // This script starts the game workflow:
    // If it is running as a server, do nothing and let the NetworkManager start the show using the StartOnHeadless switch.
    // If it is running as a client, do some actions then move to the connection scene, see Start()
    
    public class Bootstrap : MonoBehaviour
    {
        public static Bootstrap Instance { get; private set; }

        public TMP_Text _statusLine;
        
        public float _waitingTime = 2.0f;
        public int _sceneToLoad = 1;
        
        private float timer = 0f;
        private bool timerSwitch = true;


        private void Awake()
        {
            Debug.Log("Bootstrap.Awake(): method called");
            
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

        }

        public void StartServersCallBack(bool success, string fromAzure)
        {
            Debug.Log($"Bootstrap:StartServersCallBack(): registered {success} with response {fromAzure}");

            if (success)
            {
                timerSwitch = true;
                
                if (_statusLine)
                    _statusLine.text = "Portals to worlds are now open";

            }
            else
            {
                Debug.LogError("Bootstrap:StartServersCallBack(): servers did not start");
            }
        }
        void Start()
        {
            #if UNITY_SERVER
                timerSwitch = false;            
                Debug.Log("Bootstrap.Start(): server build detected, switching off bootstrap timer");
            #else
                Debug.Log("Bootstrap.Start(): client build");

                if (_statusLine)
                    _statusLine.text = "Opening portals to worlds...";
                
                // Here I use an Azure Function to do a Text to Speech action: 
                AzureFunctions az = AzureFunctions.Instance;
                az.Speak("Client started").Forget();

                // Now call an Azure Function to startup all servers hosting the maps (if they are not already running):

                // Make sure the timer is not running to load the connection screen.
                // Once this Azure Function reports back in the StartServesCallback we'll start the timer which runs in the Update() method.
                timerSwitch = false;
                az.StartServersWait(StartServersCallBack).Forget();
                
            #endif
        }
        
        void Update()
        {
            if (!timerSwitch)
                return;

            timer += Time.deltaTime;

            if (timer > _waitingTime)
            {
                timerSwitch = false;
                LoadNextScene();
            }
        }

        public void LoadNextScene()
        {
            SceneManager.LoadScene(_sceneToLoad);
        }



        
    }
}
