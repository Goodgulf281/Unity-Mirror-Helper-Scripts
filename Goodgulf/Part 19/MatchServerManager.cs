using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Goodgulf.UI;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ConsoleUtility;

namespace Goodgulf.Networking
{
    /*
     * The MatchServerManager script is used to mange the start and end of a game match. So basically it starts
     * the match when the Steam lobby initiates a match and ends it when the match/lobby owner ends the match.
     * This script should be paired with the MatchClientManager which deals with the client events.
     *
     * The basic server match start workflow looks like:
     *
     * Server - start server
     * Client - connect to server
     * Server - load scene
     * Client - load scene
     * Server - if client loaded scene then instantiate player prefab
     *
     * The basic server match end workflow:
     *
     * Server - unload scene
     * Client - unload scene
     * Client - when unload scene completes, disconnect client
     * Server - whenever a client disconnects reduce connected client count, if it reaches zero then stop server
     * 
     * Note that this script makes use of the ConsoleUtility script: https://github.com/peeweek/net.peeweek.console
     */
    
    
    public class MatchServerManager : MonoBehaviour
    {
        public static MatchServerManager Instance { get; private set; }

        // These are the prefab references originally from the HostGame Script (which is now obsolete in my project)
        
        [Tooltip("Prefab to spawn for the player.")] 
        [SerializeField]
        private NetworkObject playerPrefab;
        
        [Tooltip("Prefab to spawn match timer.")] 
        [SerializeField]
        private NetworkObject matchTimerPrefab;

        [SerializeField]
        private bool randomizeSpawns=true;

        // These are the references to the UI for the ServerStatus which you can show using the F1 key:
        public Canvas serverCanvas;
        public Image serverStatus;
        public TMP_Text serverStatusText;
        public TMP_Text connectionsText;
        public TMP_Text serverLogLine;
        public TMP_Text connectionDetails;

        // To keep track of the client connections to the server we'll store them in this list:
        private List<NetworkConnection> clientConnections;
        // This is the index of the connection shown in the ServerStatus UI
        private int connectionShown = 0;
        
        // The scene name for this match which is passed on from the Lobby into the StartGame script:
        private string sceneName;

        private bool matchStarted = false;

        private GamePlayers gamePlayers;
        
        private NetworkManager networkManager;
        
        // I started to keep track of the network objects spawned in the match since I expected to have to clean them
        // up myself. It turns out this is not needed, see below in the ServerManager_OnServerConnectionState method:
        private List<NetworkObject> matchSpawnedNetworkObjects;

        private NetworkObject matchTimer;
        
        // These are properties needed to gather and store the spawn positions in the match scene
        private Transform[] spawns;
        private int spawnIndex = 0;
        private bool spawnsCollected = false;
        
        private bool matchTimerSpawned = false;

        // A reference to the Steam user hosting the match:
        private UserData hostUser;

        // As explained in the code below we need to track the number of connected players and the number of expected
        // players, the latter is passed to this script in the StartMatch method from the Lobby.
        private int expectedNumberOfPlayers;
        private int connectedNumberOfPlayers;

        // clientCount is used to count up and down when clients connect so we'll know when no more clients are connected
        private int clientCount = 0;
        
        private void Awake()
        {
#if MatchServerDebug
            Debug.Log("MatchServerManager.Awake(): method called");
#endif
            
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        // Set the color in the ServerStatus UI and update its status field
        private void SetServerStatusColor(Color color, string status)
        {
            if (serverStatus)
                serverStatus.color = color;

            if (serverStatusText)
                serverStatusText.text = status;
        }

        // Show the latest server log entry on the ServerStatus UI and log it to the console
        private void ServerLog(string entry)
        {
            if (serverLogLine)
            {
                serverLogLine.text = entry;
            }

            ConsoleUtility.Console.Log("server",entry);
        }

        // Show the connection details in the ServerStatus UI for the connected client at a specific index
        private void ShowConnectionDetails(int index)
        {
            if (connectionDetails && index<=clientConnections.Count)
            {
                if (clientConnections.Count > 0)
                {

                    connectionShown = index;
                    NetworkConnection conn = clientConnections[index];

                    connectionDetails.text = "ClientId=" + conn.ClientId.ToString() + Environment.NewLine +
                                             " IsOnHost=" + conn.IsHost;
                }
                else connectionDetails.text = "";
            }
        }

        // Cycle through the client connections (used by the button on the ServerStatus UI)
        public void ShowNextConnection()
        {
            if (connectionDetails)
            {
                int index = connectionShown + 1;
                if (index >= clientConnections.Count)
                    index = 0;
                
                ShowConnectionDetails(index);
            }
            
        }
        
        void Start()
        {
            // Get the NetworkManager instance
            networkManager = InstanceFinder.NetworkManager;
            
            gamePlayers = GamePlayers.Instance;
            
            // Add this event in order to load the scene:
            networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            // Add this event so we can spawn the player:
            networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
            // Add this event so we can keep track of clients disconnecting and stop the server if no clients are left:
            networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            // Add this event so we can track if a client has (un)loaded a scene:
            networkManager.SceneManager.OnClientPresenceChangeEnd += SceneManager_OnClientPresenceChangeEnd;
            
            matchSpawnedNetworkObjects = new List<NetworkObject>();
            clientConnections = new List<NetworkConnection>();

            // Hide the ServerStatus UI since at start the server will not be active
            if (serverCanvas)
                serverCanvas.enabled = false;
            
            // Default status of the server = stopped
            ServerLog("Initiated Server HUD");
            SetServerStatusColor(Color.red, "Stopped");
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && networkManager.IsHost)
            {
                // Only enable the ServerStatus UI when on the host machine 
                if (serverCanvas)
                    serverCanvas.enabled = !serverCanvas.enabled;
            }
        }
        
        
        private void OnDisable()
        {
            networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
            networkManager.SceneManager.OnClientPresenceChangeEnd -= SceneManager_OnClientPresenceChangeEnd;
        }
        
        // Find the spawn positions (objects tagged as "spawn") in the scene loaded for this match:
        public void FindSpawns(bool checkExisting)
        {
            if (checkExisting && spawnsCollected)
                return;
            
            GameObject[] spawners = GameObject.FindGameObjectsWithTag("spawn");
            spawns = new Transform[spawners.Length];

            if (randomizeSpawns)
            {
                System.Random rnd = new System.Random();
                GameObject[] spawnersRandomized = spawners.OrderBy(x => rnd.Next()).ToArray();
                for (int i = 0; i < spawners.Length; i++)
                {
                    spawns[i] = spawnersRandomized[i].transform;
                }
            }
            else
            {
                for (int i = 0; i < spawners.Length; i++)
                {
                    spawns[i] = spawners[i].transform;
                }
            }
            // Set this flag so spawns are not recollected for any client entering the match after the first
            spawnsCollected = true;
        }

        // Check if the Steam User is the sam eas teh one hosting this match
        public bool DoesUserHostTheGame(UserData user)
        {
            return (hostUser == user);
        }

        // This is the key method which gets called from the Lobby when the match starts.
        // The map, hosting steam user and number fo expected players are passed from the lobby to this method
        public void StartMatch(string mapName, UserData host, int numberPlayers)
        {
            hostUser = host;
            sceneName = mapName;
            connectedNumberOfPlayers = 0;
            expectedNumberOfPlayers = numberPlayers;

            // We're going to start the server now so set teh server status accordingly on the ServerStatus UI
            if (serverCanvas)
                serverCanvas.enabled = true;
            
            SetServerStatusColor(Color.cyan, "Initialize");
            
            // Start the server:
            networkManager.ServerManager.StartConnection();
        }
        
        // This function can be called by the UI related scripts to end the match
        public void EndMatchByHost(UserData host)
        {
            if (DoesUserHostTheGame(host))
            {
                if (networkManager != null)
                {
                    // Remove all Game Players. This is the class tracking all players and their connections in the match
                    gamePlayers.RemoveAllPlayers();

                    // Send a message to all clients that we are ending the match. It will be shown in the chat:
                    MatchChat.Instance.RpcSendChatLine(host.Name+" is ending the match in 10 seconds.", null, ChatType.ChatShout);

                    StartCoroutine(EndMatchAfterTime(10f));
                }
                else
                {
                    Debug.LogError("HostGame.StopHostingTheGame(): networkmanager == null.");
                }
            }
            else
            {
                Debug.LogError("HostGame.StopHostingTheGame(): someone who is not the host tries to end the game.");
            }            
        }
        
        
        IEnumerator EndMatchAfterTime(float time)
        {
            // First just wait for 9 seconds:
            yield return new WaitForSeconds(time-1.0f);

            // Repeat the message to all clients stating the end of match is imminent:
            MatchChat.Instance.RpcSendChatLine("Ending the match now.", null, ChatType.ChatShout);

            // Wait for 1 more second to ensure clients will see the message:
            yield return new WaitForSeconds(1.0f);
            
            // What happens next is key to the workflow of the match end. Originally this is where I just stopped the server.
            // If you do that the clients get disconnected and server to client communications is obviously stopped. This
            // created a lot of errors in my case and randomly froze the game for some clients, probably because of the
            // mess I left.
            // So now instead we keep the connection open but first tell the clients to unload the match scene we
            // previously loaded (see ServerManager_OnServerConnectionState in the obj.ConnectionState == LocalConnectionState.Started
            // section of the code.
            
            ServerLog("Unload the scene");  // Do this before the Client is already stopped, otherwise it will not start or complete.
                
            // Unload the scene:
            SceneUnloadData sud = new SceneUnloadData(sceneName);
            networkManager.SceneManager.UnloadGlobalScenes(sud);

            // This yield statement shouldn't be needed anymore, it used to be important when I stopped the server here to get the unload started
            // however, it doesn't properly complete if the server to client connection is severed. 
            yield return null;
            
            // ServerLog("Stopping the Connection");
            //networkManager.ServerManager.StopConnection(true);
            matchStarted = false;
        }


        // When the client finishes unloading the scene (see MatchClientManager.SceneManager_OnUnloadEnd) it disconnects
        // itself from the server. This is where the OnRemoteConnectionState comes to play since it will reduce the
        // count of connected clients. If it reaches zero it can safely stop the server.
        private void ServerManager_OnRemoteConnectionState(NetworkConnection con, RemoteConnectionStateArgs obj)
        {
            if (obj.ConnectionState == RemoteConnectionState.Started)
            {
                // A client just connected so let's increase the client count: 
                clientCount++;
                ServerLog("Server event - remote connection "+con.ClientId+" started, clientCount="+clientCount);
            }
            else if (obj.ConnectionState == RemoteConnectionState.Stopped)
            {
                // A client just disconnected so let's decrease the client count:
                clientCount--;
                ServerLog("Server event - remote connection "+con.ClientId+" stopped, clientCount="+clientCount);

                if (clientCount == 0)
                {
                    // When the last client disconnects we can safely stop the server:
                    ServerLog("Client Count reached 0, stopping the Connection");
            
                    matchTimerSpawned = false;
                    
                    networkManager.ServerManager.StopConnection(true);
                    serverCanvas.enabled = false;
                }
            }
        }
        
        
        // Track the status of the server. If it just started then load the match scene
        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {

            if (obj.ConnectionState == LocalConnectionState.Starting)
            {
                SetServerStatusColor(Color.blue, "Starting");
                ServerLog("Server starting");
            }
            if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                ServerLog("Server stopped");
                SetServerStatusColor(Color.red, "Stopped");
            }
            
            // When server starts load online scene as global. Since this is a global scene clients will automatically join it when connecting.
            else if (obj.ConnectionState == LocalConnectionState.Started)
            {
#if Host_Debug                
                Debug.Log("Server Connection state = Started");
#endif
                // Now load the global scene where the game is player. Instead of a fixed name you can also retrieve the
                // name of the game scene from the lobby meta data, making it more dynamic

                ServerLog("Server started");
                SetServerStatusColor(Color.green, "Started");
                
#if Host_Debug
                Debug.Log("MatchServerManager.ServerManager_OnServerConnectionState(): Loading scene -> " + sceneName);
#endif
                SceneLoadData sld = new SceneLoadData(sceneName);
                sld.ReplaceScenes = ReplaceOption.All;

#if Host_Debug                
                Debug.Log("MatchServerManager.ServerManager_OnServerConnectionState(): Now load the global map");
#endif
                ServerLog("Loading global map");
                
                spawnsCollected = false;
                networkManager.SceneManager.LoadGlobalScenes(sld);
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
#if Host_Debug                
                Debug.Log("Server Connection state = Stopping");
#endif
                
                ServerLog("Server stopping");
                
                // Despawn player objects in the scene

                SetServerStatusColor(Color.yellow, "Stopping");

                /* This is where I tried to despawn the network objects when the server is stopping
                 * We don't need to despawn objects, looks like the server is doing that for us.
                
                ServerLog("Despawn network objects");
                
                foreach (NetworkObject nob in matchSpawnedNetworkObjects)
                {
                    nob.Despawn();
                }

                */

                // Trying to unload the scene here isn;t working either, the client is already stopped at this time
                // if you force the server to stop before the clients.
                
                //ServerLog("Unload the scene"); 
                
                // Unload the scene
                //SceneUnloadData sud = new SceneUnloadData(sceneName);
                //networkManager.SceneManager.UnloadGlobalScenes(sud);

                if (matchTimer)
                {
                    matchTimerSpawned = false;
                    
                    // The Match timer is also being despawned by the server
                    // matchTimer.Despawn();
                }
            }
        }

        // This is the original code I used in earlier videos to get then spawn positions and spawn the player prefab
        // as soon as the client finishes loading the start scene.
        private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
#if Host_Debug            
            Debug.Log("SceneManager_OnClientLoadedStartScenes called asServer="+asServer);
#endif
            if (!asServer)
                return;

            
            ServerLog("Client loaded start scenes");
            
            clientConnections.Add(conn);
            ShowConnectionDetails(connectedNumberOfPlayers);
            
            connectedNumberOfPlayers++;
            if (connectionsText)
                connectionsText.text = connectedNumberOfPlayers.ToString() + "/" + expectedNumberOfPlayers.ToString();

            
            ServerLog("Connected number of players: "+connectedNumberOfPlayers);
            
            // So the scene has loaded and this call is made on the server.
#if Host_Debug            
            Debug.Log("SceneManager_OnClientLoadedStartScenes running asServer");
#endif
            // Create the match timer once

            if (!matchTimerSpawned && matchTimerPrefab!=null)
            {
                matchTimer = networkManager.GetPooledInstantiated(matchTimerPrefab, true);
                networkManager.ServerManager.Spawn(matchTimer);
                matchTimerSpawned = true;
            }
            
            // Check if we gave a player prefab we want to spawn in the game:
            if (playerPrefab == null)
            {
                Debug.LogWarning($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
                return;
            }
#if Host_Debug
            Debug.Log("MatchServerManager.SceneManager_OnClientLoadedStartScenes():PlayerPrefab found");
#endif
            // Now find the spawn positions in the active scene:
            FindSpawns(true);

            Vector3 position;
            Quaternion rotation;

            if (spawns.Length > 0)
            {
                position = spawns[spawnIndex].position;
                rotation = spawns[spawnIndex].rotation;

                spawnIndex++;
                if (spawnIndex >= spawns.Length)
                    spawnIndex = 0;
            }
            else
            {
                position = playerPrefab.transform.position;
                rotation = playerPrefab.transform.rotation;
            }

            ServerLog("Spawn Player Prefab");
            
            NetworkObject nob = networkManager.GetPooledInstantiated(playerPrefab, true);
            nob.transform.SetPositionAndRotation(position, rotation);
            networkManager.ServerManager.Spawn(nob, conn);
            matchSpawnedNetworkObjects.Add(nob);
            
        }

        // This event shows the client (un)loading the scene. You could use this as an alternative method
        // of stopping the server when all clients have unloaded the scene. I decided the client disconnect
        // method to be more fail safe. Just a hunch.
        private void SceneManager_OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs obj)
        {
            if (!obj.Added)
            {
                ServerLog("Client has unloaded scene "+obj.Scene.name);
            }
            else
            {
                ServerLog("Client has loaded scene "+obj.Scene.name);

            }
        }
    }
}