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
        private GameObject ListItemUIPrefab;        // Holds the prefab containing the RoomPlayerUI script

        [Header("Runtime assigned instance of the list item:")]
        [SerializeField]
        private GameObject listItemUI;

        private RoomPlayerUI roomPlayerUI;          // Used to store the reference to the RoomPlayerUI element (the list item) associated with this player

        [SyncVar(hook = nameof(OnNameChanged))]
        public string roomPlayerName;               // Store the player's name in this SyncVar


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
                // When the client stops and we're in the RoomScene then destroy the list item associated with this Player.

                if (listItemUI)
                {
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
            if (ListItemUIPrefab)
            {
                if (listItemUI)
                {
                    Debug.LogWarning("OnClientEnterRoom.OnClientEnterRoom(): listItemUI not null");
                }
                else
                {
                    // This is where we create the list item with the UI elements.
                    listItemUI = Instantiate(ListItemUIPrefab.gameObject);
                    listItemUI.name = "ListItem" + String.Format("{0,4:D4}", UnityEngine.Random.Range(0, 1000));

                    roomPlayerUI = listItemUI.GetComponent<RoomPlayerUI>();

                    if (roomPlayerUI)
                    {
                        roomPlayerUI.ownerID = netId;   // Later on we'll need to be able to verify the list item is linked to this player. We can do that using this player's NetId

                        roomPlayerUI.SetPlayerNameLabel(PlayerLabel()); // Use a standardized method to create the label for this player.

                        roomPlayerUI.GetPlayerReadyButton().onClick.AddListener(PlayerReady);       // Add the button on click events
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
        /// This is a hook that is invoked on clients when the index changes.
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
                roomPlayerUI.SetPlayerReadyButtonInteractable(!newReadyState);
                roomPlayerUI.SetPlayerCancelButtonInteractable(newReadyState);
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

        // In the update method we check if the Ready and Cancel buttons need to change interactable state, based on the readyToBegin state of the player.
        public void Update()
        {
            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;

            if (!NetworkManager.IsSceneActive(room.RoomScene))
                return;

            if (roomPlayerUI)
            {
                if (NetworkClient.active && isLocalPlayer)
                {
                    roomPlayerUI.SetPlayerReadyButtonInteractable (!readyToBegin);
                    roomPlayerUI.SetPlayerCancelButtonInteractable(readyToBegin);
                }
                else
                {
                    roomPlayerUI.SetNotLocalPlayer();   // If not the local player then make all UI elements for the item non interactable
                }
            }
        }



        public string PlayerLabel()
        {
            string label = $"Player [{index + 1}] name:";
            return label;
        }

        public void PlayerReady()
        {
            if (isLocalPlayer)
            {
                if (roomPlayerUI)
                {
                    CmdChangeReadyState(true);

                    // If the player is ready we'll also update the player's name based on teh Input Field

                    string newName = roomPlayerUI.GetPlayerNameInput();
                    if (newName.Length < 2)
                        newName = PlayerLabel();

                    Debug.Log("NetworkRoomPlayerUI.PlayerReady(): calling CmdUpdateRoomPlayerName");

                    CmdUpdateRoomPlayerName(newName);
                }
                else Debug.LogError("NetworkRoomPlayerUI.PlayerReady(): no RoomPlayerUI Component on list item");
            }
            else Debug.LogWarning("NetworkRoomPlayerUI.PlayerReady(): called by non LocalPlayer");
        }

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


        [Command]
        public void CmdUpdateRoomPlayerName(string _newname)
        {
            Debug.Log("NetworkRoomPlayerUI.CmdUpdateRoomPlayerName(): " + _newname);

            roomPlayerName = _newname;

            // When the player's name changes we'll also need to send an "update request" to all clients
            RpcUpdatePlayerNames(netId, _newname);
        }


        [ClientRpc(includeOwner = false)]
        public void RpcUpdatePlayerNames(uint _owner, string _name)
        {
            // get parent
            //  iterate through all of its children = list items
            //      get item's RoomPlayerUI
            //          if the owner matches the RPC paramater _owner then update the name in the input field


            Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames("+_owner+", "+_name+") called.");

            if (roomPlayerUI)
            {
                GameObject parent = roomPlayerUI.GetContainerParent();
                if (parent)
                {
                    for (int i = 0; i < parent.transform.childCount; i++)
                    {

                        GameObject child = parent.transform.GetChild(i).gameObject;

                        Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): Child[" + i + "]= " + child.name);

                        RoomPlayerUI childRoomPlayerUI = child.GetComponent<RoomPlayerUI>();

                        if (childRoomPlayerUI)
                        {
                            if(childRoomPlayerUI.ownerID == _owner)
                            {
                                Debug.Log("NetworkRoomPlayerUI.RpcUpdatePlayerNames(): item for owner found");

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