using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Goodgulf.Networking
{

    public class RoomPlayerUI : MonoBehaviour
    {
        /*
         * Attach this script to a Canvas UI prefab which is: 
         * 
         * an Image and underneath it
         *  a Text (acts as a label "Player [1]")
         *  an InputField where the player can enter a player name
         *  a button for Player Ready
         *  a button for Player Cancel (ready)
         */

        private Text        playerLabel;        // These are the references to each of the Canvas UI sub elements
        private InputField  playerNameInput;
        private Button      playerReadyButton;
        private Button      playerCancelButton;

        private GameObject  panelContainer;     // This is where a reference is stored to the parent (panel) to which the list items are parented

        [SerializeField]
        private uint        _ownerID;           // Store the NetID of the owner (NetworkRoomPplayerUI) of this list item
        public uint         ownerID
        {
            get { return _ownerID; }
            set { _ownerID = value; }
        }

        public void Awake()
        {
            // Attach the list item to the parent panel:
            panelContainer = GameObject.Find("ListOfPlayers");
            if (panelContainer)
            {
                transform.SetParent(panelContainer.transform);
            }
            else Debug.LogError("RoomPlayerUI.Start(): UI parent container not found");

            // Now try and find the references to the Canvas UI list item's sub elements
            try
            {
                playerLabel         = transform.GetChild(0).GetComponent<Text>();
                playerNameInput     = transform.GetChild(1).GetComponent<InputField>();
                playerReadyButton   = transform.GetChild(2).GetComponent<Button>();
                playerCancelButton  = transform.GetChild(3).GetComponent<Button>();
            }
            catch(Exception e)
            {
                Debug.LogError("RoomPlayerUI.Start(): UI Child items not found - "+e.Message);
            }
        }

        // The following methods are used to simplify setting/getting the list item's sub elements

        public GameObject GetContainerParent()
        {
            return panelContainer;
        }

        public void SetPlayerNameLabel(string name)
        {
            playerLabel.text = name;
        }

        public void SetPlayerNameInput(string name)
        {
            playerNameInput.text = name;
        }

        public string GetPlayerNameInput()
        {
            return playerNameInput.text;
        }

        public void SetNotLocalPlayer()
        {
            playerReadyButton.interactable = false;
            playerCancelButton.interactable = false;
            playerNameInput.interactable = false;
        }


        public void SetPlayerReadyButtonInteractable(bool flag)
        {
            playerReadyButton.interactable = flag;
        }

        public void SetPlayerCancelButtonInteractable(bool flag)
        {
            playerCancelButton.interactable = flag;
        }

        public Button GetPlayerReadyButton()
        {
            return playerReadyButton;
        }

        public Button GetPlayerCancelButton()
        {
            return playerCancelButton;
        }


    }

}
