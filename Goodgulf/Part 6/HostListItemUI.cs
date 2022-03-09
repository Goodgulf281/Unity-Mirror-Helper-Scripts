using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Goodgulf.Networking
{
    
    /*
     * The HostListItemUI is attached to the prefab which when instantiated represents one (game)host on teh NodeListServer.
     * This class makes it easier to access the Canvas UI sub elements (labels and button) from the NetworkListHosts code.
     * 
     */
    public class HostListItemUI : MonoBehaviour
    {
        
        private Text   serverLabel;        // These are the references to each of the Canvas UI sub elements
        private Text   serverAddress;
        private Button joinButton;
        
        private GameObject  panelContainer;     // This is where a reference is stored to the parent (panel) to which the list items are parented
        
        public void Awake()
        {
            // Attach the list item to the parent panel:
            panelContainer = GameObject.Find("ListOfHosts");
            if (panelContainer)
            {
                transform.SetParent(panelContainer.transform);
            }
            else Debug.LogError("HostListItemUI.Awake(): UI parent container not found");

            // Now try and find the references to the Canvas UI list item's sub elements
            try
            {
                serverLabel   = transform.GetChild(0).GetComponent<Text>();
                serverAddress = transform.GetChild(1).GetComponent<Text>();
                joinButton    = transform.GetChild(2).GetComponent<Button>();
            }
            catch(Exception e)
            {
                Debug.LogError("HostListItemUI.Awake(): UI Child items not found - "+e.Message);
            }
        }
        
        // Expose the UI elements through these methods:
        
        public GameObject GetContainerParent()
        {
            return panelContainer;
        }
        
        public void SetServerLabel(string label)
        {
            serverLabel.text = label;
        }

        public void SetServerAddress(string address)
        {
            serverAddress.text = address;
        }

        public string GetServerAddress()
        {
            return serverAddress.text;
        }

        public Button GetJoinButton()
        {
            return joinButton;
        }
    }

}