using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using HeathenEngineering.SteamworksIntegration;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using API = HeathenEngineering.SteamworksIntegration.API;
using UserAPI = HeathenEngineering.SteamworksIntegration.API.User.Client;
using FriendsAPI = HeathenEngineering.SteamworksIntegration.API.Friends.Client;

public class SteamLobby : MonoBehaviour
{

    public LobbyManager lobbyManager;

    // Store the lobby created by this player / searched & joined by this player:
    public Lobby selectedLobby;

    // References to the buttons on the UI:
    public Button startGameButton;
    public Button browseButton;
    public Button readyButton;

    private NetworkManager networkManager;
    private HostGame hostGame;
    
    // Start is called before the first frame update
    private void Start()
    {
        lobbyManager.evtCreated.AddListener(LobbyCreated);
        lobbyManager.evtUserJoined.AddListener(SomeOneJoinedTheLobby);
        
        // Get the NetworkManager instance
        networkManager = InstanceFinder.NetworkManager;

        hostGame = GameObject.FindObjectOfType<HostGame>();
        if(hostGame == null)
            Debug.LogError("Could not find HostGame object!");

    }

    // This event gets called whenever a player joins the lobby. It's linked to the LobbyManager in the Start() method.
    private void SomeOneJoinedTheLobby(UserData arg0)
    {
        // The argument contains the user who joined the lobby:
        Debug.Log(arg0.Name + " joined the lobby.");

        // Now iterate through all lobby members:
        Debug.Log("The lobby members are:");
        var lobbyReference = lobbyManager.Lobby;
        foreach (var member in lobbyReference.Members)
        {
            Debug.Log(member.user.Name);
        }

        // Now check if all players are ready so we can enable the startGameButton (linked to in this component)
        if (startGameButton)
        {
            if (selectedLobby.AllPlayersReady && selectedLobby.IsOwner)
            {
                // All players are ready and I am the Lobby owner so I can now start the game
                startGameButton.interactable = true;
            }
            else
            {
                startGameButton.interactable = false;
            }
        }
    }

    // This event gets called when you enter the lobby
    public void EnterLobby(Lobby lobby)
    {
        Debug.Log("You entered Lobby: " + lobby.Name);
        selectedLobby = lobby;

        // Show the lobby details
        ReportLobbyDetails(lobby);
    }


    private void LobbyCreated(Lobby lobby)
    {
        var id = lobby.id;
        Debug.Log("On Handle Lobby Created: a new lobby has been created with CSteamID = " + lobby.ToString()
            + "\nThe CSteamID can be broken down into its parts such as :"
            + "\nAccount Type = " + id.GetEAccountType()
            + "\nAccount Instance = " + id.GetUnAccountInstance()
            + "\nUniverse = " + id.GetEUniverse()
            + "\nAccount Id = " + id.GetAccountID());

        // When the lobby is created the owner also starts with a IsReady = false status:
        if (lobby.IsOwner)
        {
            lobby.IsReady = false;
            selectedLobby = lobby;
        }
    }

    // This method is used by each player to flag they are ready.
    public void PlayerIsReady()
    {
        // First check if we have created or joined a lobby
        if (selectedLobby != null)
        {
            // OK, we are ready to indicate this to the lobby:
            selectedLobby.IsReady = true;

            if (selectedLobby.AllPlayersReady && selectedLobby.IsOwner)
            {
                // All players are ready and I am the Lobby owner so I can now start the game
                if (startGameButton)
                {
                    startGameButton.interactable = true;
                }
            }
        }
    }

    // This event gets called whenever any lobby meta data (incl. IsReady) is changed.
    public void MetaDataUpdated()
    {
        Debug.Log("Lobby Meta Data has been updated");
        if (selectedLobby != null)
        {
            if (selectedLobby.IsOwner)
            {
                // Only if you are the owner...
                if (startGameButton)
                {
                    // ... enable the startGameButton if all players have reported to be ready
                    startGameButton.interactable = selectedLobby.AllPlayersReady;
                }
            }
        }
    }

    // This event is called when a Lobby Search has been initated on the Lobby Manager using the Lobby Search settings
    public void ReportSearchResults(Lobby[] results)
    {
        // Do we have anyh results?
        if (results.Length > 0)
        {
            // Just pick teh first lobby
            Lobby firstLobby = results[0];

            Debug.Log("Joining Lobby: " + firstLobby.Name);

            // Join the lobby
            lobbyManager.Join(firstLobby);

            if (browseButton)
                browseButton.interactable = false;

            if (readyButton)
                readyButton.interactable = true;
        }
        else Debug.Log("No lobbies found!");

    }

    public void ReportLobbyDetails(Lobby lobby)
    {
        if (lobby != null)
        {
            // retrieve the Lobby owner from the Lobby:
            CSteamID owner = API.Matchmaking.Client.GetLobbyOwner(lobby);
            // Then get the owner name:
            UserData ownerData = new CSteamID(owner.m_SteamID);

            Debug.Log("Lobby name: " + lobby.Name);
            Debug.Log("Lobby owner: " + ownerData.Name);

            // Get all lobby members and show some of their details:
            LobbyMember[] members = lobby.Members;
            foreach (LobbyMember lobbyMember in members)
            {
                Debug.Log("Lobby member: " + lobbyMember.user.Name + ", isReady = " + lobbyMember.IsReady);
            }
        }
    }

 
    public void StartGame()
    {
        if (selectedLobby != null)
        {
            // We have an active lobby

            if (selectedLobby.IsOwner)
            {
                // I am the owner so I can start the game

                if(hostGame)
                    hostGame.StartHostingGame(selectedLobby);
                 
            }
            else Debug.LogWarning("StartGame not by the owner.");

        }
    }

    public void LobbyGameServerEvent(LobbyGameServer lobbyGameServer)
    {
        // Everyone in the lobby gets this message (including the player hosting the game)
        // In the LobbyGameServer parameter we can find the steamId to connect to
        
       if (selectedLobby != null)
       {
            // Get the SteamId of the player hosting the game and use it as the host address:
            string hostAddress = lobbyGameServer.id.ToString();
            
            // Now connect to the host. This will kick off the OnRemoteConnectionState event on the server
            Debug.Log("Connecting to: "+hostAddress);
            networkManager.ClientManager.StartConnection(hostAddress);
                
        }
    }


    public void CreateMyLobby()
    {
        // Having your own Lobby creation methods add some flexibility, for example adding custom meta data to the lobby
        
        var user = UserAPI.Id;

        string lobbyName = user.Name + "'s lobby";                          // Use a more dynamic lobby name
        lobbyManager.createArguments.name = lobbyName;                      
        lobbyManager.createArguments.type = ELobbyType.k_ELobbyTypePublic;  // Example: add a switch to the UI which toggles between public and friends only 
        
        LobbyManager.MetadataTempalate scene = new LobbyManager.MetadataTempalate(); // Spelling error here in Heathen Steamworks code I guess.
        
        // Now we add a meta data field for the game scene to be loaded
        // Each meta data entry is using a key/value pair:
        scene.key = "scene";
        scene.value = "test scene 1";                       // Example: read the scene from a dropdown box instead
        lobbyManager.createArguments.metadata.Add(scene);
        
        lobbyManager.Create();
    }
    
}
