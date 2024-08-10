using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Managing;
using Goodgulf.Azure;
using TMPro;

namespace Goodgulf.Networking
{
    // This is a (very) simple script in order for us to connect to a server from the client. See Start().
    public class ConnectClientLite : MonoBehaviour
    {
        
        public TMP_InputField _ipAddress;

        public void ServerListCallBack(bool success, ServerDataList listOfServerData)
        {
            // This is the callback called when the ServeList data is returned from the Azure Function.
            // This is a but messy and only shows one of the servers and needs to be updated.
            
            if (success)
            {
                foreach (ServerData server in listOfServerData.data)
                {
                    Debug.Log($"Found a server running on {server.ipAddress}:{server.port} with scene {server.scene}");
                    _ipAddress.text = server.ipAddress;
                }
            }
            else Debug.LogError("ConnectClientLite.ServerListCallBack(): could not retrieve server list.");
        }

        public void Start()
        {
            // Get a list of all servers which are running. In the previous scene we used an Azure Function to wake up all servers so
            // by now they should all be running and have registered themselves (as in ServerHelper.cs).
            
            AzureFunctions az = AzureFunctions.Instance;
            az.ServerList(ServerListCallBack).Forget();
        }

        public void Connect()
        {
#if UNITY_SERVER
            Debug.LogError("ConnectClientLite.Connect(): client connect from server attempted");            
#else
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            Debug.Log("ConnectClientLite.Connect(): attempting connect to ["+_ipAddress.text+"]");
            networkManager.ClientManager.StartConnection(_ipAddress.text);

#endif


        }

        public void StopServersCallBack(bool success, string fromAzure)
        {
            Debug.Log($"ConnectClientLite:StartServersCallBack(): stopped {success} with response {fromAzure}");
        }
        
        public void ShutdownServers()
        {
#if UNITY_SERVER
            Debug.LogError("ConnectClientLite.ShutdownServers(): server shutdown from server attempted");            
#else            
            AzureFunctions az = AzureFunctions.Instance;
            az.ForceShutdownServers(StopServersCallBack).Forget();
            
#endif
        }

        public void PingServer()
        {
#if UNITY_SERVER
            Debug.LogError("ConnectClientLite.PingServer(): server ping from server attempted");            
#else
            string target = _ipAddress.text;
            
            StartCoroutine(StartPing(target));
#endif
        }
        
 
        IEnumerator StartPing(string  ip)
        {
            WaitForSeconds f = new WaitForSeconds(0.05f);
            Ping p = new Ping(ip);
            while (p.isDone == false)
            {
                yield return f;
            }
            PingFinished(p);
        }
 
 
        public void PingFinished(Ping p)
        {
            Debug.Log($"Ping: {p.time}");
        }  
        
        
    }


}