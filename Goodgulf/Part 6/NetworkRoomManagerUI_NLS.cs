using UnityEngine;
using System;
using UnityEngine.UI;
using Mirror;
using NodeListServer;
/* 
 * This NetworkRoomManager is created from the Mirror template and supports:
 * 
 * - The NodeListServer by Matt Coburn, see  below link and notice. I updated Debug code.
 * - Canvas based UI instead of the OnGUI() draw commands which are the default in Mirror.
 * 
 * NodeListServer methods based on:
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

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomManager.html

	See Also: NetworkManager
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

namespace Goodgulf.Networking
{

    /// <summary>
    /// This is a specialized NetworkManager that includes a networked room.
    /// The room has slots that track the joined players, and a maximum player count that is enforced.
    /// It requires that the NetworkRoomPlayer component be on the room player objects.
    /// NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class.
    /// </summary>
    public class NetworkRoomManagerUI_NLS : NetworkRoomManager
    {

        #region Unity Callbacks
        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Is there a NodeLS Communication Manager in the scene? If not, spawn one in.
            // The NodeListServerCommunicationManager provides methods for the host to list itself on the NodeListServer
            if (NodeListServerCommunicationManager.Instance == null)
            {
                GameObject nlsCommMan = new GameObject();
                nlsCommMan.name = "NodeLS Communication Manager";
                nlsCommMan.AddComponent<NodeListServerCommunicationManager>();

                DontDestroyOnLoad(nlsCommMan);
            }
        }

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// </summary>
        public override void LateUpdate()
        {
            base.LateUpdate();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion

        #region Server Callbacks

        /// <summary>
        /// Called on the server when a client is ready.
        /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerReady(NetworkConnection conn)
        {
            base.OnServerReady(conn);

            Debug.Log("NetworkRoomManagerUI_NLS.OnServerReady("+ conn.connectionId +"): method called");
            
            // The server is ready so now add it to the hosts list on the NodeListServer
            if (NodeListServerCommunicationManager.Instance != null)
            {
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.PlayerCount = NetworkServer.connections.Count;
                // Stops client host from double calling AddUpdateServerEntry
                if (conn.connectionId != 0)
                {
                    Debug.Log("NetworkRoomManagerUI_NLS.OnServerReady(): AddUpdateServerEntry() to be called");
                    NodeListServerCommunicationManager.Instance.AddUpdateServerEntry();
                }
            }
            else Debug.LogError("NetworkRoomManagerUI_NLS.OnServerReady(): NodeListServerCommunicationManager.Instance = null");
        }


        /// <summary>
        /// This is called on the server when the server is started - including when a host is started.
        /// </summary>
        public override void OnRoomStartServer() { }

        /// <summary>
        /// This is called on the server when the server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnRoomStopServer() { }

        /// <summary>
        /// This is called on the host when a host is started.
        /// </summary>
        public override void OnRoomStartHost() { }

        /// <summary>
        /// This is called on the host when the host is stopped.
        /// </summary>
        public override void OnRoomStopHost() { }

        /// <summary>
        /// This is called on the server when a new client connects to the server.
        /// </summary>
        /// <param name="conn">The new connection.</param>
        public override void OnRoomServerConnect(NetworkConnection conn) { }

        /// <summary>
        /// This is called on the server when a client disconnects.
        /// </summary>
        /// <param name="conn">The connection that disconnected.</param>
        public override void OnRoomServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);

            // If we have a NodeLS Communication Manager, then update the player counts.
            if (NodeListServerCommunicationManager.Instance != null)
            {
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.PlayerCount = NetworkServer.connections.Count;
                // Fix for client host
                if (conn.connectionId != 0) { NodeListServerCommunicationManager.Instance.AddUpdateServerEntry(); }
            }
        }


        /// <summary>
        /// This is called on the server when a networked scene finishes loading.
        /// </summary>
        /// <param name="sceneName">Name of the new scene.</param>
        public override void OnRoomServerSceneChanged(string sceneName) { }

        /// <summary>
        /// This allows customization of the creation of the room-player object on the server.
        /// <para>By default the roomPlayerPrefab is used to create the room-player, but this function allows that behaviour to be customized.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        /// <returns>The new room-player object.</returns>
        public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection conn)
        {
            return base.OnRoomServerCreateRoomPlayer(conn);
        }

        /// <summary>
        /// This allows customization of the creation of the GamePlayer object on the server.
        /// <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        /// <param name="roomPlayer">The room player object for this connection.</param>
        /// <returns>A new GamePlayer object.</returns>
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
        {
            return base.OnRoomServerCreateGamePlayer(conn, roomPlayer);
        }

        /// <summary>
        /// This allows customization of the creation of the GamePlayer object on the server.
        /// <para>This is only called for subsequent GamePlay scenes after the first one.</para>
        /// <para>See OnRoomServerCreateGamePlayer to customize the player object for the initial GamePlay scene.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        public override void OnRoomServerAddPlayer(NetworkConnection conn)
        {
            base.OnRoomServerAddPlayer(conn);
        }

        /// <summary>
        /// This is called on the server when it is told that a client has finished switching from the room scene to a game player scene.
        /// <para>When switching from the room, the room-player is replaced with a game-player object. This callback function gives an opportunity to apply state from the room-player to the game-player object.</para>
        /// </summary>
        /// <param name="conn">The connection of the player</param>
        /// <param name="roomPlayer">The room player object.</param>
        /// <param name="gamePlayer">The game player object.</param>
        /// <returns>False to not allow this player to replace the room player.</returns>
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            // Now get the room player name entered through the list item and assign it to the game player.

            PlayerScript playerScript = gamePlayer.GetComponent<PlayerScript>();
            if (playerScript)
            {
                // Get the room Player name for the roomPlayer's NetworkRoomPlayerUI component:
                string playerName = roomPlayer.GetComponent<NetworkRoomPlayerUI>().roomPlayerName;

                Debug.Log("NetworkRoomManagerUI.OnRoomServerSceneLoadedForPlayer(): Updating player name to " + playerName);
                // Assign it to the player:
                playerScript.playerName = playerName;
            }
            else Debug.LogError("NetworkRoomManagerUI.OnRoomServerSceneLoadedForPlayer(): cannot find PlayerScript on object");

            return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
        }

        /// <summary>
        /// This is called on the server when all the players in the room are ready.
        /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
        /// </summary>
        bool showStartButton = false;

        public override void OnRoomServerPlayersReady()
        {
#if UNITY_SERVER
            base.OnRoomServerPlayersReady();
#else
            Debug.Log("NetworkRoomManagerUI.OnRoomServerPlayersReady() called.");

            showStartButton = true;

            if (startButton)
                startButton.interactable = showStartButton;
#endif
        }

        /// <summary>
        /// This is called on the server when CheckReadyToBegin finds that players are not ready
        /// <para>May be called multiple times while not ready players are joining</para>
        /// </summary>
        public override void OnRoomServerPlayersNotReady()
        {
            Debug.Log("NetworkRoomManagerUI.OnRoomServerPlayersNotReady() called.");

            showStartButton = false;

            if (startButton)
                startButton.interactable = showStartButton;
        }

        #endregion

        #region Client Callbacks

        /// <summary>
        /// This is a hook to allow custom behaviour when the game client enters the room.
        /// </summary>
        public override void OnRoomClientEnter() { }

        /// <summary>
        /// This is a hook to allow custom behaviour when the game client exits the room.
        /// </summary>
        public override void OnRoomClientExit() { }

        /// <summary>
        /// This is called on the client when it connects to server.
        /// </summary>
        public override void OnRoomClientConnect() { }

        /// <summary>
        /// This is called on the client when disconnected from a server.
        /// </summary>
        public override void OnRoomClientDisconnect() { }

        /// <summary>
        /// This is called on the client when a client is started.
        /// </summary>
        public override void OnRoomStartClient() { }

        /// <summary>
        /// This is called on the client when the client stops.
        /// </summary>
        public override void OnRoomStopClient() { }

        /// <summary>
        /// This is called on the client when the client is finished loading a new networked scene.
        /// </summary>
        public override void OnRoomClientSceneChanged() { }

        /// <summary>
        /// Called on the client when adding a player to the room fails.
        /// <para>This could be because the room is full, or the connection is not allowed to have more players.</para>
        /// </summary>
        public override void OnRoomClientAddPlayerFailed() { }

        #endregion

        #region Optional UI

        public override void OnGUI()
        {
            // DKE: Remove the OnGUI() commands
            // base.OnGUI();
        }

        #endregion


        #region NodeListServer

        [Header("NodeListServer Specific Settings")]
        [SerializeField] private string ServerName = "My Server";
        [Tooltip("NOT the Mirror server port. This is what will be advertised to the list server.")]
        [SerializeField] private int ServerPublicPort = 7777;

        [Tooltip("Should we auto-update the List Server periodically?")]
        [SerializeField] private bool UpdateServerPeriodically = true;
        [Tooltip("Value in minutes. How often the server should phone home to update it's status.")]
        [SerializeField] private int UpdateServerPeriod = 5;

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            Debug.Log("NetworkRoomManagerUI_NLS.OnStartServer(): method called.");

            // If we have a NodeLS Communication Manager, then announce the server to the list server.
            if (NodeListServerCommunicationManager.Instance != null)
            {
                // Adding a random string to -more or less- have unique server names in the list
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.Name = ServerName + String.Format("{0,4:D4}", UnityEngine.Random.Range(0, 1000));
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.Port = ServerPublicPort;
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.PlayerCount = 0;
                NodeListServerCommunicationManager.Instance.CurrentServerInfo.PlayerCapacity = maxConnections;

                // Now send the information to the NodeListServer
                NodeListServerCommunicationManager.Instance.AddUpdateServerEntry();
            }
            else Debug.LogError("NetworkRoomManagerUI_NLS.OnStartServer(): CommunicationManager.Instance = null");

            // If also specified, we should start the regular scheduled update.
            if (UpdateServerPeriodically)
            {
                Debug.Log("NetworkRoomManagerUI_NLS.OnStartServer(): start InvokeRepeating.");
                InvokeRepeating(nameof(NodeListServerCommunicationManager.Instance.AddUpdateServerEntry), Time.time + (UpdateServerPeriod * 60), UpdateServerPeriod * 60);
            }
        }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            // If we have a NodeLS Communication Manager, remove the server from the list.
            if (NodeListServerCommunicationManager.Instance != null)
            {
                NodeListServerCommunicationManager.Instance.RemoveServerEntry();
            }

            CancelInvoke();
        }

        #endregion


        #region Goodgulf Code


        private Button startButton = null;

        private void Update()
        {
            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;

            if (NetworkManager.IsSceneActive(room.RoomScene))
            {
                if (startButton == null)
                {
                    // Find the Start Button (when we are in the Room Scene) which is in the top panel of the canvas
                    startButton = GameObject.Find("btnStartGame").GetComponent<Button>();

                    // Add a listener which to switch from the room to the game scene

                    if (startButton)
                        startButton.onClick.AddListener(GoAllPlayersReady);
                }
            }
        }

        // Use this method to switch from the room scene to the game scene
        private void GoAllPlayersReady()
        {

            if (startButton)
                startButton.interactable = false;

            ServerChangeScene(GameplayScene);
        }

        #endregion
    }

}