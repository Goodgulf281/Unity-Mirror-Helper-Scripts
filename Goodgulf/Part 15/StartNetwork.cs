using System.Collections;
using System.Collections.Generic;
using FishNet.Managing;
using UnityEngine;

public class StartNetwork : MonoBehaviour
{
    /*
     * This is a simple script to star the Fishnet Networking server and client. The StartNetworking method is
     * called from the NetworkInputControl script which is attached to the GetInput game object.
     *
     *  Attach this script to the NetworkManager object.
     */
    
    private NetworkManager _networkManager;
    
    // Start is called before the first frame update
    void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
    }

    public void StartNetworking()
    {
        if (_networkManager == null)
            return;

        Debug.Log("Starting Server");
        
        _networkManager.ServerManager.StartConnection();

        Debug.Log("Starting Client");
        
        _networkManager.ClientManager.StartConnection();
        
    }
}
