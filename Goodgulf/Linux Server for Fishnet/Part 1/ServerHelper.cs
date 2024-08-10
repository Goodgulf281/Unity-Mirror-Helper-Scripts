using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using System.Linq;
using FishNet.Object;
using Goodgulf.Azure;
using Goodgulf.Utilities;

namespace Goodgulf.Networking
{
    // This script contains the core Fishnet server functionality we have seen in earlier tutorials.
    // It uses the standard set of Fishnet events (see AddEvents method) to run the server. 
    
    public class ServerHelper : MonoBehaviour
    {
        [Tooltip("Simplify tracelogs to a single line.")] 
        [SerializeField]
        private bool _simplifyTracelog = true;
        
        [Tooltip("Prefab to spawn for the player.")] 
        [SerializeField]
        private NetworkObject _playerPrefab;
        
        [Tooltip("Randomize the spawn positions in any game scene.")]
        [SerializeField]
        private bool randomizeSpawns=true;
        
        // Next 4 properties are passed through the command line arguments 
        [SerializeField]
        private string _serverid;
        
        [SerializeField]
        private string _sceneName;
        
        [SerializeField]
        private int _port;

        [SerializeField] 
        private string _ipAddress;
        
        private NetworkManager _networkManager;
        private int _connectedNumberOfPlayers;
        
        // clientCount is used to count up and down when clients connect so we'll know when no more clients are connected
        private int _clientCount = 0;
        
        // These are properties needed to gather and store the spawn positions in the match scene
        private Transform[] _spawns;
        private int _spawnIndex = 0;
        private bool _spawnsCollected = false;

        
        public static ServerHelper Instance { get; private set; }

        private ServerData serverData;
        
        
        private void Awake()
        {
            // To prevent the server logs exploding I'm reducing the stack trace log while not debugging.
            
#if UNITY_SERVER
            if(_simplifyTracelog)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            }
#endif
            
            Debug.Log("ServerHelper.Awake(): method called");
            
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // We need to change some NetworkManager settings before it calls its Start method. This is why I use OnEnable which gets called earlier. 
            
            _networkManager = InstanceFinder.NetworkManager;    

            // Use a helper script to read the command line arguments.  
            
            CommandLineArguments cla = CommandLineArguments.Instance;
            string mapName;
            
            if (cla.arguments.TryGetValue("-scene", out string myScene))
            {
                Debug.Log($"ServerHelper.OnEnable(): using scene {myScene}");
                mapName = myScene;
            }
            else
            {
                mapName = "Island";
                Debug.LogWarning($"ServerHelper.OnEnable(): using default scene {mapName}");
            }
            
            if (cla.arguments.TryGetValue("-port", out string myPort))
            {
                Debug.Log($"ServerHelper.OnEnable(): using port {myPort}");

                if (Int32.TryParse(myPort, out _port))
                {
                    _networkManager.TransportManager.Transport.SetPort((ushort)_port);
                }
            }

            if (cla.arguments.TryGetValue("-ipaddress", out string myIPAddress))
            {
                Debug.Log($"ServerHelper.OnEnable(): using ip address {myIPAddress}");
                _ipAddress = myIPAddress;
            }
            else _ipAddress = "0.0.0.0";

            if (cla.arguments.TryGetValue("-serverid", out string myID))
            {
                Debug.Log($"ServerHelper.OnEnable(): using server ID {myID}");
                _serverid = myID;
            }
            else _ipAddress = "1";

            
#if UNITY_SERVER
            Debug.Log("ServerHelper.OnEnable(): server build detected, starting up server");
            
            AddEvents();
            StartMatch(mapName);
           
#else
            Debug.Log("ServerHelper.OnEnable(): no server build");
#endif
        }
        
        private void AddEvents()
        {
            // Add this event in order to load the scene:
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            // Add this event so we can spawn the player:
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            // Add this event so we can keep track of clients disconnecting and stop the server if no clients are left:
            _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            // Add this event so we can track if a client has (un)loaded a scene:
            _networkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
        }

        private void OnDisable()
        {
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
            _networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            _networkManager.SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;
        }

        private void StartMatch(string mapName)
        {
            _sceneName = mapName;
            _networkManager.ServerManager.StartConnection();
        }
        
        
#region Spawns

        // Find the spawn positions (objects tagged as "spawn") in the scene loaded for this match:
        public void FindSpawns(bool checkExisting)
        {
            if (checkExisting && _spawnsCollected)
                return;
                    
            GameObject[] spawners = GameObject.FindGameObjectsWithTag("Spawn");
            _spawns = new Transform[spawners.Length];

            if (randomizeSpawns)
            {
                System.Random rnd = new System.Random();
                GameObject[] spawnersRandomized = spawners.OrderBy(x => rnd.Next()).ToArray();
                for (int i = 0; i < spawners.Length; i++)
                {
                    _spawns[i] = spawnersRandomized[i].transform;
                }
            }
            else
            {
                for (int i = 0; i < spawners.Length; i++)
                {
                    _spawns[i] = spawners[i].transform;
                }
            }
            // Set this flag so spawns are not recollected for any client entering the match after the first
            _spawnsCollected = true;
        }

#endregion
        
#region ServerEvents

        // When the client finishes unloading the scene (see MatchClientManager.SceneManager_OnUnloadEnd) it disconnects
        // itself from the server. This is where the OnRemoteConnectionState comes to play since it will reduce the
        // count of connected clients. If it reaches zero it can safely stop the server.
        private void OnRemoteConnectionState(NetworkConnection con, RemoteConnectionStateArgs obj)
        {
            if (obj.ConnectionState == RemoteConnectionState.Started)
            {
                // A client just connected so let's increase the client count: 
                _clientCount++;
                Debug.Log("ServerHelper.OnRemoteConnectionState(): remote connection "+con.ClientId+" started, clientCount="+_clientCount);


                if (serverData!=null)
                {
                    // Update the player count for this server. This is stored in a Table in an Azure Storage Account.
                    // I use an Azure Function (more on that in teh 3rd part of this tutorial series) to update the record.
                    
                    serverData.playerCount=_clientCount;
                    AzureFunctions az = AzureFunctions.Instance;
                    az.UpdateServer(serverData, RegisterServerCallBack).Forget();
                }
                else Debug.LogError("ServerHelper.OnRemoteConnectionState(): serverData==null");
                
            }
            else if (obj.ConnectionState == RemoteConnectionState.Stopped)
            {
                // A client just disconnected so let's decrease the client count:
                _clientCount--;
                Debug.Log("ServerHelper.OnRemoteConnectionState(): remote connection "+con.ClientId+" stopped, clientCount="+_clientCount);

                
                if (serverData!=null)
                {
                    // Update the player count for this server. This is stored in a Table in an Azure Storage Account.
                    
                    serverData.playerCount=_clientCount;
                    AzureFunctions az = AzureFunctions.Instance;
                    az.UpdateServer(serverData, RegisterServerCallBack).Forget();
                }
                else Debug.LogError("ServerHelper.OnRemoteConnectionState(): serverData==null");

                
                
                if (_clientCount == 0)
                {
                    // When the last client disconnects we can safely stop the server:
                    Debug.Log("ServerHelper.OnRemoteConnectionState(): Client Count reached 0, stopping the Connection");
                    
                    _networkManager.ServerManager.StopConnection(true);
                    
                    // 5 minutes after the client disconnects we check if all servers can be shutdown.
                    // This gives the client the opportunity to reconnect if this turns out to be a network issue.
                    
                    Invoke("DelayedConditionalCheck", 300);
                    
#if UNITY_EDITOR
                    // UnityEditor.EditorApplication.isPlaying = false;
#else
                    // Application.Quit();
#endif
                }
            }
        }


        public void DelayedConditionalCheck()
        {
            // Send an Azure Function request to check if there are more clients, if not, shutdown all servers
            AzureFunctions az = AzureFunctions.Instance;
            az.ConditionalShutdownServers(RegisterServerCallBack).Forget();
        }
        

        public void RegisterServerCallBack(bool success, string fromAzure)
        {
            Debug.Log($"ServerHelper:RegisterServerCallBack(): registered {success} with response {fromAzure}");

        }
        
        
        // Track the status of the server. If it just started then load the match scene
        private void OnServerConnectionState(ServerConnectionStateArgs obj)
        {

            if (obj.ConnectionState == LocalConnectionState.Starting)
            {
                Debug.Log("ServerHelper.OnServerConnectionState(): Server starting");            
            }
            if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                Debug.Log("ServerHelper.OnServerConnectionState(): Server stopped");
            }
            
            // When server starts load online scene as global. Since this is a global scene clients will automatically join it when connecting.
            else if (obj.ConnectionState == LocalConnectionState.Started)
            {
                // First Register the Server in an Azure Storage Table using an Azure Function 

                serverData = new ServerData();
                serverData.id = _serverid;
                serverData.port = _networkManager.TransportManager.Transport.GetPort();
                serverData.ipAddress = _ipAddress;
                serverData.playerCount = 0;
                serverData.scene = _sceneName;
                
                AzureFunctions az = AzureFunctions.Instance;
                az.RegisterServer(serverData, RegisterServerCallBack).Forget();
                
                // Now load the global scene where the game is player. Instead of a fixed name you can also retrieve the
                // name of the game scene from the lobby meta data, making it more dynamic

                Debug.Log("ServerHelper.OnServerConnectionState(): Server started");
                
                SceneLoadData sld = new SceneLoadData(_sceneName);
                sld.ReplaceScenes = ReplaceOption.All;

                Debug.Log($"ServerHelper.OnServerConnectionState(): Loading Global Map {_sceneName}");
                
                _spawnsCollected = false;
                _networkManager.SceneManager.LoadGlobalScenes(sld);
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
                Debug.Log("ServerHelper.OnServerConnectionState(): Server stopping");
            }
        }

        // This is the original code I used in earlier videos to get then spawn positions and spawn the player prefab
        // as soon as the client finishes loading the start scene.
        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            Debug.Log("ServerHelper.OnClientLoadedStartScenes(): called asServer="+asServer);

            if (!asServer)
                return;

            _connectedNumberOfPlayers++;
           
            Debug.Log("ServerHelper.OnClientLoadedStartScenes(): connected number of players: "+_connectedNumberOfPlayers);
            
            // Check if we gave a player prefab we want to spawn in the game:
            if (_playerPrefab == null)
            {
                Debug.LogError($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
                return;
            }

            // Now find the spawn positions in the active scene:
            FindSpawns(true);

            Vector3 position;
            Quaternion rotation;

            if (_spawns.Length > 0)
            {
                position = _spawns[_spawnIndex].position;
                rotation = _spawns[_spawnIndex].rotation;

                _spawnIndex++;
                if (_spawnIndex >= _spawns.Length)
                    _spawnIndex = 0;
            }
            else
            {
                position = _playerPrefab.transform.position;
                rotation = _playerPrefab.transform.rotation;
            }

            Debug.Log("ServerHelper.OnClientLoadedStartScenes(): Spawn Player Prefab");
            
            NetworkObject nob = _networkManager.GetPooledInstantiated(_playerPrefab, true);
            nob.transform.SetPositionAndRotation(position, rotation);
            _networkManager.ServerManager.Spawn(nob, conn);
            
        }

        // This event shows the client (un)loading the scene. You could use this as an alternative method
        // of stopping the server when all clients have unloaded the scene. I decided the client disconnect
        // method to be more fail safe. Just a hunch.
        private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs obj)
        {
            if (!obj.Added)
            {
                Debug.Log("ServerHelper.OnClientPresenceChangeEnd(): Client has unloaded scene "+obj.Scene.name);
            }
            else
            {
                Debug.Log("ServerHelper.OnClientPresenceChangeEnd(): Client has loaded scene "+obj.Scene.name);

            }
        }
        

#endregion


        
    }
}