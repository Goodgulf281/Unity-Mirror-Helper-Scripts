using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Goodgulf.Networking
{
    public class NetworkManagerUI : MonoBehaviour
    {
        
        [SerializeField]
        private Button btnHost;             // Host button
        
        [SerializeField]
        private Button btnClient;           // Client button 

        [SerializeField]
        private InputField inputAddress;    // Address field (defaults to localhost)

        NetworkManager manager;

        void Awake()
        {
            // This component is attached to the NetworkManager (or in this case: the NetworkRoomManagerMyGame of which NetworkManager is the base class
            // The Networkmanager is used in events when a button is clicked so store a reference during Awake.
            manager = GetComponent<NetworkManager>();
        }

        public void DisableUI ()
        {
            // Disable the GUI in one go
            btnClient.interactable = false;
            btnHost.interactable = false;
            inputAddress.interactable = false;
        }

        public void StartHost()
        {
            // This is -more or less- the same code which Mirror used in the OnGUI() calls
            if (!NetworkClient.active && !NetworkServer.active)
            {
                DisableUI();
                manager.StartHost();
            }
            else Debug.LogWarning("NetworkManagerUI.StartHost(): client already active.");
        }

        public void StartClient()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                manager.networkAddress = inputAddress.text;
                DisableUI();
                manager.StartClient();
            }
            else Debug.LogWarning("NetworkManagerUI.StartClient(): client already active.");
        }
    }
}