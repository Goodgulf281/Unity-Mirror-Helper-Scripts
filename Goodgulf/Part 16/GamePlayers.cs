using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using FishNet.Connection;
using HeathenEngineering.SteamworksIntegration;
using Steamworks;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Goodgulf.Networking
{
    [Serializable]
    public class GamePlayer
    {
        /*
         * This class is used to collect both NetworkConnection and Steam UserData in order for the server
         * to be able to match steam users to their network connections.
         * 
         */
        public NetworkConnection networkConnection;
        public UserData userData;

        public GamePlayer(NetworkConnection conn, UserData user)
        {
            networkConnection = conn;
            userData = user;
        }
    }
    
    public class GamePlayers : MonoBehaviour
    {
        /*
         * This is the class where we collect all GamePlayer instances into a dictionary.
         * You can expand this class to query player and connection data.  
         * 
         */
        
        private Dictionary<CSteamID, GamePlayer> AllPlayers = new Dictionary<CSteamID, GamePlayer>();

        public static GamePlayers Instance { get; private set; }

        private LobbyManager lobbyManager = null;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            
            lobbyManager = GameObject.FindObjectOfType<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogError("GamePlayers.Awake(): cannot find LobbyManager");
                return;
            }
            
        }

        /*
         * The AddGamePlayer method is called to add a new player to the dictionary.
         * Typically it will be called from a ServerRPC, collecting player data on the server.
         */
        public void AddGamePlayer(NetworkConnection conn, UserData user)
        {
            CSteamID key = user.id;
            if (AllPlayers.ContainsKey(key))
            {
                Debug.LogWarning("GamePlayers.AddGamePlayer(): trying to add a player with the same key "+key.ToString());
            }
            else
            {
                GamePlayer gamePlayer = new GamePlayer(conn, user);
                AllPlayers.Add(key, gamePlayer);
                Debug.Log("GamePlayers.AddGamePlayer: just added a player to the game list ("+user.Name+",connection client id="+conn.ClientId+")");
            }
        }

        public GamePlayer GetGamePlayer(CSteamID key)
        {
            if (AllPlayers.ContainsKey(key))
            {
                return AllPlayers[key];
            }
            return null;
        }

        /*
         * This is an example function to gather player information. It iterates through the dictionary and returns
         * a string with all player information including its network connection.
         *
         * Since I defined a meta data field "character" on Steam Player level in my lobby manager script I also
         * can show that data by linking up to the Lobby Manager.
         */
        public string GetPlayersDebugLog()
        {
            string result = "";
            
            foreach (KeyValuePair<CSteamID, GamePlayer> entry in AllPlayers)
            {
                // do something with entry.Value or entry.Key

                result += "Player ID "+entry.Key.ToString() + " on connection " +entry.Value.networkConnection.ClientId.ToString()
                          + ": name = "+entry.Value.userData.Name
                          + Environment.NewLine;

                // Find this player in the lobby manager members
                LobbyMember player = Array.Find(lobbyManager.Lobby.Members, element => element.user.id.Equals(entry.Key));

                result += "* LobbyMember: " + player.user.Name+ Environment.NewLine;
                
                // If you know the key for a user meta data field you can also retrieve it:
                result += "* Member Meta Data: character = " + player["character"];

            }

            return result;
        }
        
    }
}