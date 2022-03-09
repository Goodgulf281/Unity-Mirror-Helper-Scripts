using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;

namespace Goodgulf.Networking
{

    /// <summary>
    /// This component works in conjunction with the NetworkRoomManager to make up the multiplayer room system.
    /// The RoomPrefab object of the NetworkRoomManager must have this component on it.
    /// This component holds basic room player data required for the room to function.
    /// Game specific data for room players can be put in other components on the RoomPrefab or in scripts derived from NetworkRoomPlayer.
    /// </summary>
    public class NetworkRoomPlayerUI : NetworkRoomPlayer
    {
        [Header("Assign the prefab used for showing the player UI list item here:")]
        [SerializeField]
        private GameObject ListItemUIPrefab;        // Assign the prefab to be used as the list item in the Room scene. See RoomPlayerUI.cs for instructions on how this prefab should look like.

        [Header("Runtime assigned instance of the list item:")]
        [SerializeField]
        private GameObject listItemUI;              // Once the list item for this Room Player is created in OnClientEnterRoom() its reference is stored here

        private RoomPlayerUI roomPlayerUI;          // Used to store the reference to the RoomPlayerUI component (the list item) associated with this player

        [SyncVar(hook = nameof(OnNameChanged))]
        public string roomPlayerName;               // Store the player's room name in this SyncVar


        #region Start & Stop Callbacks

        /// <summary>
        /// This is invoked for NetworkBehaviour objects when they become active on the server.
        /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
        /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
        /// </summary>
        public override void OnStartServer() { }

        /// <summary>
        /// Invoked on the server when the object is unspawned
        /// <para>Useful for saving object data in persistent storage</para>
        /// </summary>
        public override void OnStopServer() { }

        /// <summary>
        /// Called on every NetworkBehaviour when it is activated on a client.
        /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
        /// </summary>
        public override void OnStartClient() 
        {
        }

        /// <summary>
        /// This is invoked on clients when the server has caused this object to be destroyed.
        /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
        /// </summary>
        public override void OnStopClient() 
        {
            Debug.LogWarning("NetworkRoomPlayerUI.OnStopClient(): called");

            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;

            if (NetworkManager.IsSceneActive(room.RoomScene))
            {
                if (listItemUI)
                {
                    // Cleanup the list item:
                    Destroy(listItemUI);
                    roomPlayerUI = null;
                }
                else Debug.LogError("NetworkRoomPlayerUI.OnStopClient(): listItemUI not found");
            }
        }

        /// <summary>
        /// Called when the local player object has been set up.
        /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
        /// </summary>
        public override void OnStartLocalPlayer() { }

        /// <summary>
        /// This is invoked on behaviours that have authority, based on context and <see cref="NetworkIdentity.hasAuthority">NetworkIdentity.hasAuthority</see>.
        /// <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
        /// <para>When <see cref="NetworkIdentity.AssignClientAuthority"/> is called on the server, this will be called on the client that owns the object. When an object is spawned with <see cref="NetworkServer.Spawn">NetworkServer.Spawn</see> with a NetworkConnection parameter included, this will be called on the client that owns the object.</para>
        /// </summary>
        public override void OnStartAuthority() { }

        /// <summary>
        /// This is invoked on behaviours when authority is removed.
        /// <para>When NetworkIdentity.RemoveClientAuthority is called on the server, this will be called on the client that owns the object.</para>
        /// </summary>
        public override void OnStopAuthority() { }

        #endregion

        #region Room Client Callbacks

        /// <summary>
        /// This is a hook that is invoked on all player objects when entering the room.
        /// <para>Note: isLocalPlayer is not guaranteed to be set until OnStartLocalPlayer is called.</para>
        /// </summary>
        public override void OnClientEnterRoom() 
        {
            // When a player enters the room (either from the offline scene or from the game scene) some tasks need to be completed
            // First check if the ListItemUIPrefab has been assigned. This prefab is used to create the list item in the player list

            if (ListItemUIPrefab)
            {
                if (listItemUI)
                {
                    // A list item already exists for this player. This should only happen if the player returns from the game scene into the room scene
                    Debug.LogWarning("OnClientEnterRoom.OnClientEnterRoom(): listItemUI not null");
                }
                else
                {
                    // No list item exists so create one
                    listItemUI = Instantiate(ListItemUIPrefab.gameObject);
                    listItemUI.name = "ListItem" + String.Format("{0,4:D4}", UnityEngine.Random.Range(0, 1000));

                    roomPlayerUI = listItemUI.GetComponent<RoomPlayerUI>();

                    if (roomPlayerUI)
                    {
                        // Assign this player's netId to the RoomPlayerUI component on the list item.
                        // We need this for the Remote Procedure Call RpcUpdatePlayerNames() in order for us to identify which list item
                        //  belongs to which player.
                        roomPlayerUI.ownerID = netId;

                        // Now set the list item UI components:
                        //      The Text label = PlayerLabel() = $"Player [{index + 1}] name:"
                        //      Add listener events to the buttons
                        roomPlayerUI.SetPlayerNameLabel(PlayerLabel());
                        roomPlayerUI.GetPlayerReadyButton().onClick.AddListener(PlayerReady);
                        roomPlayerUI.GetPlayerCancelButton().onClick.AddListener(PlayerCancel);
                    }
                    else Debug.LogError("OnClientEnterRoom.OnClientEnterRoom(): no RoomPlayerUI Component on list item");
                }
            }
            else Debug.LogError("OnClientEnterRoom.OnClientEnterRoom(): UI list item prefab not found");
        }

        /// <summary>
        /// This is a hook that is invoked on all player objects when exiting the room.
        /// </summary>
        public override void OnClientExitRoom() { }

        #endregion

        #region SyncVar Hooks

        // When the roomPlayerName changes update the Input field in the list item
        void OnNameChanged(string _Old, string _New)
        {
            Debug.Log("NetworkRoomPlayerUI.OnNameChanged() from " + _Old + " to " + _New);

            RoomPlayerUI roomPlayerUI = GetComponent<RoomPlayerUI>();

            if (roomPlayerUI)
            {
                roomPlayerUI.SetPlayerNameInput(_New);
            }
        }

        /// <summary>
        /// This is a hook that is invoked on clients when the index changes. If this happens update the Text label in the list item
        /// </summary>
        /// <param name="oldIndex">The old index value</param>
        /// <param name="newIndex">The new index value</param>
        public override void IndexChanged(int oldIndex, int newIndex) 
        {
            Debug.Log("NetworkRoomPlayerUI.IndexChanged() from " + oldIndex + " to " + newIndex);

            if (roomPlayerUI)
            {
                roomPlayerUI.SetPlayerNameLabel(PlayerLabel());
            }
            else Debug.LogError("NetworkRoomPlayerUI.IndexChanged(): roomPlayerUI = null");
        }

        /// <summary>
        /// This is a hook that is invoked on clients when a RoomPlayer switches between ready or not ready.
        /// <para>This function is called when the a client player calls SendReadyToBeginMessage() or SendNotReadyToBeginMessage().</para>
        /// </summary>
        /// <param name="oldReadyState">The old readyState value</param>
        /// <param name="newReadyState">The new readyState value</param>
        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState) 
        {
            Debug.Log("NetworkRoomPlayerUI.ReadyStateChanged() from " + oldReadyState + " to " + newReadyState);

            if (roomPlayerUI)
            {
                roomPlayerUI.SetPlayerReadyButtonInteractable(!newReadyState);  // If the player is ready then the Ready button should be disabled
                roomPlayerUI.SetPlayerCancelButtonInteractable(newReadyState);  //  and the Cancel button should be enabled
            }
            else Debug.LogError("NetworkRoomPlayerUI.ReadyStateChanged(): roomPlayerUI = null");
        }

        #endregion

        #region Optional UI

        public override void OnGUI()
        {
            // do not draw OnGUI()
        }

        #endregion


        #region Goodgulf Code

        public void Update()
        {
            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;

            if (!NetworkManager.IsSceneActive(room.RoomScene))
                return;

            // This code is run only in the Room Scene. Note that this script will remain active in the Game Scene due to the DontDestroyOnLoad status
            //  of the network manager (related) components created in the Offline scene

            if (roomPlayerUI)
            {
                if (NetworkClient.active && isLocalPlayer)
                {
                    // Enable/disable the list item buttons (Ready, Cancel) based on the readyToBegin status of this player
                    roomPlayerUI.SetPlayerReadyButtonInteractable (!readyToBegin);
                    roomPlayerUI.SetPlayerCancelButtonInteractable(readyToBegin);
                }
                else
                {
                    // Disable all UI elements of the associated list item because this is not the local player and we shouldn't be able to edit them.
                    roomPlayerUI.SetNotLocalPlayer();
                }
            }
        }

        // Create a default label for the player based on the player's index
        public string PlayerLabel()
        {
            string label = $"Player [{index + 1}] name:";
            return label;
        }

        // This event is kicked off when the player click the Ready Button in the List Item
        // See OnClientEnterRoom() where this method is added to the listener of the button.
        public void PlayerReady()
        {
            if (isLocalPlayer)
            {
                if (roomPlayerUI)
                {
                    CmdChangeReadyState(true);  // Set the player's ready state to true on the server

                    string newName = roomPlayerUI.GetPlayerNameInput(); // Get the player's name from te input field.
                    if (newName.Length < 2)
                        newName = PlayerLabel();                        // If the name isn't long enough then use the default player label instead

                    Debug.Log("NetworkRoomPlayerUI.PlayerReady(): calling CmdUpdateRoomPlayerName");

                    CmdUpdateRoomPlayerName(newName);                   // Now update the player name on the server (which calls an RPC to update all clients)
                }
                else Debug.LogError("NetworkRoomPlayerUI.PlayerReady(): no RoomPlayerUI Component on list item");
            }
            else Debug.LogWarning("NetworkRoomPlayerUI.PlayerReady(): called by non LocalPlayer");
        }

        // This event is kicked off when the player click the Cancel Button in the List Item
        // See OnClientEnterRoom() where this method is added to the listener of the button.

        public void PlayerCancel()
        {
            if (isLocalPlayer)
            {
                if (roomPlayerUI)
                {
                    CmdChangeReadyState(false);
                }
                else Debug.LogError("NetworkRoomPlayerUI.PlayerCancel(): no RoomPlayerUI Component on list item");
            }
            else Debug.LogWarning("NetworkRoomPlayerUI.PlayerCancel(): called by non LocalPlayer");
        }


        // Update the player name on the server
        [Command]
        public void CmdUpdateRoomPlayerName(string _newname)
        {
            Debug.Log("NetworkRoomPlayerUI.CmdUpdateRoomPlayerName(): " + _newname);

            roomPlayerName = _newname;

            // Now kick off updating the clients so they update the updated player name in their list item
            // Send the nedId of the playe for whom the name changed
            RpcUpdatePlayerNames(netId, _newname);
        }


        // We don't need to owner to update the owner client since that is the source of the name change
        [ClientRpc(includeOwner = false)]
        public void RpcUpdatePlayerNames(uint _owner, string _name)
        {
            // get parent panel
            //  iterate through all of its children = list item containing the RoomPlayerUI component
            //      get child roomPlayerUI component
            //      check if this child/list item roomPlayerUI.ownerid matches with the _owner parameter of the RPC call.
            //          if so then update UI contents


            Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames("+_owner+", "+_name+") called.");

            if (roomPlayerUI)
            {
                // Get parent panel, it's already stored in the roomPlayerUI during its Awake() call
                GameObject parent = roomPlayerUI.GetContainerParent();
                if (parent)
                {
                    // Now iteratie through all of its children = list items with a roomPlayerUI
                    for (int i = 0; i < parent.transform.childCount; i++)
                    {
                        GameObject child = parent.transform.GetChild(i).gameObject;

                        Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): Child[" + i + "]= " + child.name);

                        RoomPlayerUI childRoomPlayerUI = child.GetComponent<RoomPlayerUI>();

                        if (childRoomPlayerUI)
                        {
                            // Is this child the _owner of the _name (both the RPC parameters)?
                            if(childRoomPlayerUI.ownerID == _owner)
                            {
                                Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): item for owner found");

                                // Now update the name in the UI
                                childRoomPlayerUI.SetPlayerNameInput(_name);
                            }
                        }
                        else Debug.LogError("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): cannot find key components");
                    }
                }
                else Debug.LogError("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): parent = null");
            }
            else Debug.LogError("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): roomPlayerUI = null");
        }


        #endregion

    }
}