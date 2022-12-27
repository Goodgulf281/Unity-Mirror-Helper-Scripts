using System.Collections;
using System.Collections.Generic;
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
    
    // Start is called before the first frame update
    private void Start()
    {
        lobbyManager.evtCreated.AddListener(LobbyCreated);
        lobbyManager.evtUserJoined.AddListener(SomeOneJoinedTheLobby);
    }

    // This event gets called whenever a player joins the lobby. It's linked to the LobbyManager in the Start() method.
    private void SomeOneJoinedTheLobby(UserData arg0)
    {
        // The argument contains the user who joined the lobby:
        Debug.Log(arg0.Name + " joined the lobby.");

        // Now iterate through all lobby members:
        Debug.Log("The lobby members are:");
        var lobbyReference = lobbyManager.Lobby;
        foreach(var member in lobbyReference.Members)
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
        Debug.Log("You entered Lobby: "+lobby.Name);
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
            
            Debug.Log("Joining Lobby: "+firstLobby.Name);
            
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
                Debug.Log("Lobby member: "+lobbyMember.user.Name + ", isReady = "+lobbyMember.IsReady);
            }
        }
    }
    
    
    
}
