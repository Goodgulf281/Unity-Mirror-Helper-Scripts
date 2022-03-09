using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/* The NodeListServerCommunicationManager component connects to the NodeListServer to add/update/delete
 * hosts to the list it hosts. The NodeListServer keeps track of all game hosts and allows clients
 * to query and select a host to join. Note: it only assists with keeping track of hosts and doesn't
 * deal with clients requesting a list of all hosts. Check NetworkListHosts.cs for that functionality.
 *
 * Based on:
 * 
 * This version is updated to support a newer version of the NodeListServer:
 * 
 * - In order to add a server the serverUuid cannot be part of the post data
 * - Changed various print commands into Debug.LogX commands
 * 
 * 
 * https://github.com/SoftwareGuy/NodeListServer-Example
 * 
 * MIT License
 * Copyright (c) 2020 Matt Coburn
 * This software uses dependencies (Mirror and other libraries) 
 * licensed under other different licenses.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

namespace NodeListServer
{
    public class NodeListServerCommunicationManager : MonoBehaviour
    {
        public static NodeListServerCommunicationManager Instance;

        // You can modify this data.
        public ServerInfo CurrentServerInfo = new ServerInfo()
        {
            Name = "Untitled Server",
            Port = 7777,
            PlayerCount = 0,
            PlayerCapacity = 0,
            ExtraInformation = string.Empty
        };

        private const string AuthKey = "NodeListServerDefaultKey";

        // Change this to your NodeLS Server instance URL.
        private const string Server = "http://YOURSERVERHERE.com:8889";

        // Don't modify, this is randomly generated.
        private string InstanceServerId = string.Empty;

        private void Awake()
        {
            // If singleton was somehow loaded twice...
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("NodeListServerCommunicationManager.Awake(): Duplicate NodeLS Communication Manager detected in scene. This one will be destroyed.");
                Destroy(this);
                return;
            }

            Instance = this;

            // DKE: This needs to be removed since the latest version of the NodeList Server generates the GUID for any newly added server.
            // Generate a new identification string. 
            // Guid randomGuid = Guid.NewGuid();
            // InstanceServerId = randomGuid.ToString();
        }


        public void AddUpdateServerEntry()
        {
            StartCoroutine(nameof(AddUpdateInternal));
        }

        public void RemoveServerEntry()
        {
            StartCoroutine(nameof(RemoveServerInternal));
        }


        private IEnumerator AddUpdateInternal()
        {
            WWWForm serverData = new WWWForm();

            Debug.Log("NodeListServerCommunicationManager.AddUpdateInternal(): Adding/Updating Server Entry to " + Server);
            Debug.Log("NodeListServerCommunicationManager.AddUpdateInternal(): serverKey=" + AuthKey + ", serverUuid=" + InstanceServerId + ", serverName=" + CurrentServerInfo.Name + ", serverPort=" + CurrentServerInfo.Port + ", serverPlayers=" + CurrentServerInfo.PlayerCount + ", serverCapacity=" + CurrentServerInfo.PlayerCapacity + ", serverExtras=" + CurrentServerInfo.ExtraInformation);

            serverData.AddField("serverKey", AuthKey);

            // DKE: Due to a newer version of the NodeListServer do not add the InstanceServerId to the POST serverData or
            //      your request will be denied by the NodeListServer:
            bool addingServer = false;

            if(String.IsNullOrEmpty(InstanceServerId))
            {
                addingServer = true;
                Debug.Log("NodeListServerCommunicationManager.AddUpdateInternal(): Command = Add a server");
            }
            else
            {
                Debug.Log("NodeListServerCommunicationManager.AddUpdateInternal(): Command = Update a server");
                serverData.AddField("serverUuid", InstanceServerId);
            }
            
            serverData.AddField("serverName", CurrentServerInfo.Name);
            serverData.AddField("serverPort", CurrentServerInfo.Port);
            serverData.AddField("serverPlayers", CurrentServerInfo.PlayerCount);
            serverData.AddField("serverCapacity", CurrentServerInfo.PlayerCapacity);
            serverData.AddField("serverExtras", CurrentServerInfo.ExtraInformation);

            // Now all the information we want to post is collected setup the request and submit it:
            UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(Server + "/add", serverData);
            // DKE: add a download handler since the response of the post will be the new GUID for the InstanceServerId
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            Debug.Log("NodeLS Communication Manager: InstanceServerId = " + InstanceServerId);

            if (www.responseCode == 200)
            {
                print("Successfully registered server with the NodeListServer instance!");

                // DKE: Now take Uuid from result if we are adding a server. Update a server does not return additional data
                Debug.Log("NodeListServerCommunicationManager.AddUpdateInternal(): new Uuid=" + www.downloadHandler.text);
                if(addingServer)
                    InstanceServerId = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"NodeListServerCommunicationManager.AddUpdateInternal(): Failed to register the server with the NodeListServer instance: {www.error}");                    
            }

            yield break;
        }

        // If we quit the host then it needs to be removed so us ethe RemoveServerInternal method for this
        private IEnumerator RemoveServerInternal()
        {
            WWWForm serverData = new WWWForm();
            Debug.Log("NodeListServerCommunicationManager.RemoveServerInternal(): Removing Server Entry");

            // Assign all the fields required.
            serverData.AddField("serverKey", AuthKey);
            serverData.AddField("serverUuid", InstanceServerId);

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(Server + "/remove", serverData))
            {
                yield return www.SendWebRequest();

                if (www.responseCode == 200)
                {
                    Debug.Log("NodeListServerCommunicationManager.RemoveServerInternal(): Successfully deregistered server with the NodeListServer instance!");
                }
                else
                {
                    Debug.LogError($"NodeListServerCommunicationManager.RemoveServerInternal(): Failed to deregister the server with the NodeListServer instance: {www.error}");
                }
            }

            yield break;
        }
    }

}
