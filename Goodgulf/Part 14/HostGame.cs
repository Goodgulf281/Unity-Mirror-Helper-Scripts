using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;
using API = HeathenEngineering.SteamworksIntegration.API;
using UserAPI = HeathenEngineering.SteamworksIntegration.API.User.Client;
public class HostGame : MonoBehaviour
{
    [Tooltip("Prefab to spawn for the player.")]
    [SerializeField]
    private NetworkObject playerPrefab;
    
    private NetworkManager networkManager;
    private Transform[] spawns;
    private int spawnIndex = 0;

    private string sceneName;
    
    void Start()
    {
        // Get the NetworkManager instance
        networkManager = InstanceFinder.NetworkManager;   
    }

    public void FindSpawns()
    {
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("spawn");
        spawns = new Transform[spawners.Length];

        for(int i=0;i<spawners.Length;i++)
        {
            spawns[i] = spawners[i].transform;
        }
    }
    

    public void StartHostingGame(Lobby lobby)
    {
        if (lobby != null && networkManager!=null)
        {
            // We want to read the scene name dynamically instead of using the fixed scene name
            // So we read it from the lobby meta data:

            Dictionary<string, string> lobbyMetaData = lobby.GetMetadata();

            // string asString = string.Join(Environment.NewLine, lobbyMetaData);
            // Debug.Log("Meta data: "+asString);
            
            sceneName = lobbyMetaData["scene"];
            Debug.Log("Storing Scene name:"+sceneName);
            
            // Add this event in order to load the scene
            networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;

            // Add this event so we can spawn the player
            networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;

            networkManager.ServerManager.StartConnection();

            // Get the user's Steam Id and send it to the other players so they know where to connect to
            var user = UserAPI.Id;
            lobby.SetGameServer(user.id);
            
        }
    }
    
    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        // When server starts load online scene as global. Since this is a global scene clients will automatically join it when connecting.
        if (obj.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("Server Connection state = Started");
            
            // Now load the global scene where the game is player. Instead of a fixed name you can also retrieve the
            // name of the game scene from the lobby meta data, making it more dynamic
            
            
            Debug.Log("Loading scene: "+sceneName);
            SceneLoadData sld = new SceneLoadData(sceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            
            Debug.Log("Now load the global map");
            networkManager.SceneManager.LoadGlobalScenes(sld);
        }
    }
    
    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        Debug.Log("SceneManager_OnClientLoadedStartScenes called");
        
        if (!asServer)
            return;
        
        // So the scene has loaded and this call is made on the server.
        Debug.Log("SceneManager_OnClientLoadedStartScenes running asServer");

        // Check if we gave a player prefab we want to spawn in the game:
        if (playerPrefab == null)
        {
            Debug.LogWarning($"Player prefab is empty and cannot be spawned for connection {conn.ClientId}.");
            return;
        }
        Debug.Log("PlayerPrefab found");

        // Now find the spawn positions in the active scene:
        FindSpawns();

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

        NetworkObject nob = networkManager.GetPooledInstantiated(playerPrefab, true);
        nob.transform.SetPositionAndRotation(position, rotation);
        networkManager.ServerManager.Spawn(nob, conn);

        //If there are no global scenes 
        //if (_addToDefaultScene)
        //    _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

    }
    
    
}
