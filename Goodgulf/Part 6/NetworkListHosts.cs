using UnityEngine;
using UnityEngine.UI;
using Mirror;
using NodeListServer;
using System.Collections;
using System.Collections.Generic;

namespace Goodgulf.Networking
{
    /*
     * NetworkListHosts is a component which can be placed in the HostSelect scene which sits between the Offline 
     * and Room scenes for the client only. So basically when you client the start client button in the Offline scene 
     * it opens the HostSelect scene where this component connects to the NodeListSever to retrieve all hosts
     * which are running the game. The client can then select a host and join it.
     * Note: the servers list their public IP address so if you host a server behind a NAT firewall you'll need
     * to enable port forwarding and open up your firewall: this is a risk so be careful.
     * In one of the next videos we'll look at an alternative and safer method so consider this to be a proof of
     * concept only and definitely be careful and consider twice before opening up ports in your router and firewall.
     * Did I mention being careful?
     *
     * In the Youtube video associated wit this code I'm running the NodeListServer on the internet in an Azure VM. 
     *
     * 
     * This code heavily borrow from Matt Coburn's example. Since his version is not compatible with recent
     * NodeListServer updates I made a few changes.
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
    public class NetworkListHosts : MonoBehaviour
    {
        [Header("API Configuration")]
        [Tooltip("The URL to connect to the NodeListServer. For example, http://127.0.0.1:8889/list.")]
        [SerializeField] private string masterServerUrl = "http://YOURSERVERHERE:8889/list";
        [Tooltip("The key required to talk to the server. It must match. Default is NodeListServerDefaultKey.")]
        [SerializeField] private string communicationKey = "NodeListServerDefaultKey";

        [Header("Refresh Settings")]
        [Tooltip("Should we automatically refresh the server list? Set to 0 to disable. Default is 10 seconds.")]
        [SerializeField] private int refreshInterval = 30;

        [Header("UI")]
        [Tooltip("Prefab UI element being instanced in the list for each host registered on the NodeListServer.")]
        public GameObject listElementPrefab;    // This is the prefab UI element being instanced in the list for each server in the NodeListServer result set
        [Tooltip("Link to the UI element in your hierarchy to which teh instanced UI element will be parented to.")]
        public GameObject listElementContainer; // This is the parent to which each UI element instanced will be parented

        private bool isBusy = false;
        
        private WWWForm unityRequestForm;
        private List<NodeListServerListEntry> listServerListEntries = new List<NodeListServerListEntry>();

        private void Awake()
        {
            // Setup the web form to query the NodeListServer 
            unityRequestForm = new WWWForm();
            unityRequestForm.AddField("serverKey", communicationKey);

            // Sanity Checks
            if (string.IsNullOrEmpty(communicationKey))
            {
                Debug.LogError("NetworkListHosts.Awake(): The communication Key cannot be null or empty!");
                enabled = false;
            }
        }
        
        // Start is called before the first frame update
        void Start()
        {
            RefreshList();  // On start immediately request the list of hosts from the NodeListServer

            if (refreshInterval > 0)
            {
                // Now refresh the list every refreshInterval seconds
                InvokeRepeating(nameof(RefreshList), Time.realtimeSinceStartup + refreshInterval, refreshInterval);
            }
        }

        private void RefreshList()
        {
            // Don't refresh again if we're busy
            if (isBusy) return;

            StartCoroutine(RefreshServerList());
        }   
        
        private IEnumerator RefreshServerList()
        {
            Debug.Log("NetworkListHosts.RefreshServerList(): Refreshing server list");
            // First send the request to list all hosts to teh NodeListServer
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(masterServerUrl, unityRequestForm))
            {
                isBusy = true;
                Debug.Log("NetworkListHosts.RefreshServerList(): waiting on web request");
                // This will wait until the request is sent.
                yield return www.SendWebRequest();

                if (www.responseCode == 200)
                {
                    // No error so proceed to process the results. Step 1 is to convert the resulting JSON into a list of servers: 
                    NodeListServerListResponse response = JsonUtility.FromJson<NodeListServerListResponse>(www.downloadHandler.text.Trim());

                    if (response != null)
                    {
                        Debug.Log($"NetworkListHosts.RefreshServerList(): Received a response with {response.count} servers");
                        
                        listServerListEntries = response.servers;

                        if (listElementContainer != null)
                        {
                            // We have servers so show them in the UI:
                            BalancePrefabs(listServerListEntries.Count, listElementContainer.transform);
                            UpdateListElements();
                        }
                    }
                    else
                    {
                        Debug.LogError($"NetworkListHosts.RefreshServerList(): Failed to refresh the server list! The response couldn't be parsed.");
                    }

                }
                else
                {
                    Debug.LogError($"NetworkListHosts.RefreshServerList(): Failed to refresh the server list! The error returned was: {www.responseCode}\n{www.error}");
                }

                isBusy = false;
            }

            yield break;
        }

        public void BalancePrefabs(int amount, Transform parent)
        {
            // This method basically ensure the number of instantiated listElementPrefabs equals the number of servers retrieved from the NodeListServer
            // instantiate until amount
            for (int i = parent.childCount; i < amount; ++i)
            {
                // We don;t have enough prefabs to match teh amount of servers so create more
                if (listElementPrefab != null) Instantiate(listElementPrefab, parent, false);
            }

            // delete everything that's too much
            // (backwards loop because Destroy changes childCount)
            for (int i = parent.childCount - 1; i >= amount; --i)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
        
        private void UpdateListElements()
        {
            // In this method we fill the listElementPrefabs with the contents of the server list:
            for (int i = 0; i < listServerListEntries.Count; i++)
            {
                if (i >= listElementContainer.transform.childCount || listElementContainer.transform.GetChild(i) == null) continue;

                // The HostListItemUI component is attached to each instantiated listElementPrefab to make it easier to access its children (texts and button)
                HostListItemUI hostListItemUI =
                    listElementContainer.transform.GetChild(i).GetComponent<HostListItemUI>();

                string modifiedAddress = string.Empty;
                if (listServerListEntries[i].ip.StartsWith("::ffff:"))
                {
                    modifiedAddress = listServerListEntries[i].ip.Replace("::ffff:", string.Empty);
                }
                else
                {
                    modifiedAddress = listServerListEntries[i].ip;
                }

                hostListItemUI.SetServerLabel(listServerListEntries[i].name);
                hostListItemUI.SetServerAddress(modifiedAddress);

                Button joinButton = hostListItemUI.GetJoinButton();
                
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(() =>
                {
                    // If we click the Join button we'll start the client with the address of the server linked to this listElementPrefab / listServerEntries[i]
                    NetworkManager.singleton.networkAddress = modifiedAddress;
                    NetworkManager.singleton.StartClient();
                });
            }
        }
        
        
        
    }

}