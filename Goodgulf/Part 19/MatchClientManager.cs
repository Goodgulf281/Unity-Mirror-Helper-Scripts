using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ConsoleUtility;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using Goodgulf.UI;
using UnityEngine.SceneManagement;

namespace Goodgulf.Networking
{
    /*
     * The MatchClientManager is used to manage the match from the client's perspective. Next to the Client Status UI
     * it also deals (mostly) with the end of the match.
     * 
     * This script should be paired with the MatchServerManager which deals with the server events.
     *
     * The match workflow looks like this from a client perspective:
     *
     * Server - unload scene
     * Client - unload scene
     * Client - when unload scene completes, disconnect client
     *
     * Note that this script makes use of the ConsoleUtility script: https://github.com/peeweek/net.peeweek.console 
     */
    
    public class MatchClientManager : MonoBehaviour
    {
        public static MatchClientManager Instance { get; private set; }

        // These are the references to the UI for the ClientStatus which you can show using the F2 key:
        public Canvas clientCanvas;
        public Image clientStatus;
        public TMP_Text clientStatusText;
        public TMP_Text connectionText;
        public TMP_Text clientLogLine;
        public TMP_Text connectionDetails;

        private NetworkManager networkManager;

        private void Awake()
        {
#if MatchServerDebug
            Debug.Log("MatchClientManager.Awake(): method called");
#endif

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        // Set the color in the ClientStatus UI and update its status field
        private void SetClientStatusColor(Color color, string status)
        {
            if (clientStatus)
                clientStatus.color = color;
            
            if (clientStatusText)
                clientStatusText.text = status;
        }

        // Show the latest client log entry on the ClientStatus UI and log it to the console
        private void ClientLog(string entry)
        {
            if (clientLogLine)
            {
                clientLogLine.text = entry;
            }

            ConsoleUtility.Console.Log("client", entry);
        }

        // Show the connection details in the ClientStatus UI for the local client.
        // Note that the clientID always shows as -1
        // See: https://firstgeargames.com/FishNet/api/api/FishNet.Connection.NetworkConnection.html#FishNet_Connection_NetworkConnection_UNSET_CLIENTID_VALUE
        private void ShowConnectionDetails()
        {
            NetworkConnection connection = networkManager.ClientManager.Connection;

            connectionText.text = connection.ClientId.ToString();
            
            connectionDetails.text = "Started=" + networkManager.ClientManager.Started +  "ClientId=" + connection.ClientId.ToString() + Environment.NewLine +
                                     " IsOnHost=" + connection.IsHost;
        }

        void Start()
        {
            // Get the NetworkManager instance
            networkManager = InstanceFinder.NetworkManager;

            // Hide the ClientStatus UI since at start the client will not be active
            if (clientCanvas)
                clientCanvas.enabled = false;

            ClientLog("Initiated Client HUD");
            SetClientStatusColor(Color.red, "Stopped");
            
            // Use this event to show the various connection states of the client (Starting, Started, etc,..):
            networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            
            // This event is called when the client has loaded the start scenes:
            networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;

            // This event is called when the client starts unloading a scene:
            networkManager.SceneManager.OnUnloadStart += SceneManager_OnUnloadStart;
            
            // This event is called when the client ends unloading a scene.
            // We use this event to disconnect from the server:
            networkManager.SceneManager.OnUnloadEnd += SceneManager_OnUnloadEnd;
            
            // This event is called when the client starts loading a scene:
            networkManager.SceneManager.OnLoadStart += SceneManager_OnLoadStart;
            
            // This event is called when the client ends loading a scene:
            networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;

        }


        private void OnDisable()
        {
            networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            networkManager.SceneManager.OnUnloadStart -= SceneManager_OnUnloadStart;
            networkManager.SceneManager.OnUnloadEnd -= SceneManager_OnUnloadEnd;
            
            networkManager.SceneManager.OnLoadStart -= SceneManager_OnLoadStart;
            networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2) && networkManager.IsClient)
            {
                // Only enable the ClientStatus UI when the client is running
                // (the script may be active before the client is running, in my case since I put it in the bootstrap scene)
                if (clientCanvas)
                    clientCanvas.enabled = !clientCanvas.enabled;

            }
        }

        private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (asServer)
                return;
            
            ClientLog("Loaded Start Scenes");
        }

        
        // https://fish-networking.gitbook.io/docs/manual/guides/scene-management/scene-events

        private void SceneManager_OnLoadStart(SceneLoadStartEventArgs obj)
        {
            bool AsServer = obj.QueueData.AsServer;
            
            ClientLog("Start Load Scene AsServer="+AsServer);
        }
        
        
        private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs obj)
        {
            bool AsServer = obj.QueueData.AsServer;
            
            ClientLog("End Load Scene AsServer="+AsServer);
        }
        
        private void SceneManager_OnUnloadStart(SceneUnloadStartEventArgs obj)
        {
            bool AsServer = obj.QueueData.AsServer; 
            
            ClientLog("Start Unload Scene AsServer="+AsServer);
        }

        // This is the key event I'm using in the end of Match workflow. The server has sent the unload scene command
        // and when unloading is ready we're disconnecting this client 
        private void SceneManager_OnUnloadEnd(SceneUnloadEndEventArgs obj)
        {
            bool AsServer = obj.QueueData.AsServer;
            
            ClientLog("End Unload Scene AsSever="+AsServer);
            
            // So now the match scene has been unloaded,
            // let's return to the lobby scene
            // Step 1: disconnect
            
            networkManager.ClientManager.StopConnection();
            clientCanvas.enabled = false;
            
            // Step 2: go to the lobby scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(2); // Load the Lobby scene
            SceneUIFlowManager.Instance.ShowMenu(Menus.LobbyMenu); // make sure any other menus are invisible
            
        }
        
        // Use this event to show the state of the client connection in the Client Status UI.
        // Usually you can't see it switching between Starting and Started on the client sitting on the host PC.
        // However it can be useful if the client is somehow stuck in the process.
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Starting)
            {
                SetClientStatusColor(Color.blue, "Starting");
                ClientLog("Client starting");   
            }
            else if (obj.ConnectionState == LocalConnectionState.Started)
            {
                SetClientStatusColor(Color.green, "Started");
                ShowConnectionDetails();
                ClientLog("Client started");
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
                SetClientStatusColor(Color.yellow, "Stopping");
                ClientLog("Client stopping");
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                SetClientStatusColor(Color.red, "Stopped");
                ClientLog("Client stopped");

            }
        }
        
        
        
        
}
}
