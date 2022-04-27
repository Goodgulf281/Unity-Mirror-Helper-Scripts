using System.Collections;
using System.Collections.Generic;
using Goodgulf.Gamelogic;
using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using API = HeathenEngineering.SteamworksIntegration.API;
using UserAPI = HeathenEngineering.SteamworksIntegration.API.User.Client;
using FriendsAPI = HeathenEngineering.SteamworksIntegration.API.Friends.Client;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

namespace Goodgulf.Steam
{
    public class SteamLobby : MonoBehaviour
    {

        [BoxGroup("Join Lobby")] 
        public GameObject lobbyItemPrefab;              // This is the prefab used in the list of lobbies as a result of the LobbyManager.Search 
        
        [BoxGroup("Selected Lobby")] 
        [SerializeField]
        public Lobby selectedLobby;                     // This is the Lobby joined/created by the player. 
        [BoxGroup("Selected Lobby")]
        public GameObject selectedLobbyPlayerPrefab;    // This is the player prefab to show each player in the joined lobby

        // private references:
        private LobbyChatDirector lobbyChatDirector;
        private LobbyManager lobbyManager;

        
        // private flags:
        private bool iAmTheHost = false;
        private bool iCreatedALobby = false;
        private bool iJoinedALobby = false;

        private UserData localUser;
        
        // Start is called before the first frame update
        void Start()
        {
            // Find the LobbyManager through my Singleton. See article: https://gamedevbeginner.com/singletons-in-unity-the-right-way/ 
            lobbyManager = Singleton.Instance.LobbyManager;
            if(!lobbyManager)
                Debug.LogError("SteamLobby.Start(): cannot find LobbyManager");
                
            lobbyChatDirector = this.gameObject.GetComponent<LobbyChatDirector>();
            if(!lobbyChatDirector)
                Debug.LogError("SteamLobby.Start(): cannot find LobbyChatDirector");

            localUser = UserAPI.Id;
            
            // Start searching for lobbies and repeat every 2 seconds. 
            InvokeRepeating("SearchLobbies", 4.0f, 2.0f);
        }

        
        // This is where we create a lobby and auto join it as the owner.
        public void CreateMyLobby()
        {
            Debug.Log("SteamLobby.Start(): Creating a Lobby (with auto join).");

            var user = UserAPI.Id;
            
            // Get the name of the lobby from the UI and if the player didn't put in a name construct it using the player name:
            string lobbyName = Singleton.Instance.UIManager.LobbyName.text;
            if (lobbyName.Length < 2)
            {
                lobbyName = user.Name + "'s battle lobby";
            }
            
            // When creating a lobby using the LobbyManager just add data to the lobbyManager.createArguments properties: 
            lobbyManager.createArguments.name = lobbyName;

            // Get the number of player slots for the lobby from the UI:
            int lobbySlots = Singleton.Instance.UIManager.LobbySlots.value + 1;
            lobbyManager.createArguments.slots = lobbySlots;

            // Check the toggle for Steam friends only visibility of the lobby.
            if (Singleton.Instance.UIManager.FriendsOnly.isOn)
            {
                lobbyManager.createArguments.type = ELobbyType.k_ELobbyTypeFriendsOnly;
            }
            else
            {
                lobbyManager.createArguments.type = ELobbyType.k_ELobbyTypePublic;
            }

            // Retrieve the lobby data from the UI to be put into the Lobby meta data:
            string lobbyPassword = Singleton.Instance.UIManager.LobbyPassword.text;
            string lobbyMapname = Singleton.Instance.UIManager.LobbyMap.options[Singleton.Instance.UIManager.LobbyMap.value].text;
            
            // Now add map to meta data, see LobbyManager line 476
            // lobbyManager.createArguments.metadata.Add();

            LobbyManager.MetadataTempalate map = new LobbyManager.MetadataTempalate(); // Spelling error here in Heathen Steamworks code I guess.
            LobbyManager.MetadataTempalate pwd = new LobbyManager.MetadataTempalate();

            // Each meta data entry is using a key/value pair:
            map.key = "map";
            map.value = lobbyMapname;
            lobbyManager.createArguments.metadata.Add(map);

            if (lobbyPassword.Length > 0)
            {
                map.key = "pwd";
                map.value = lobbyPassword;                      // This stores the password in clear text so obviously not secure by design. Will update later to something better. Not used at the moment
                lobbyManager.createArguments.metadata.Add(map);
            }

            // Now create the lobby:
            lobbyManager.Create(); // This should kickoff LobbyCreation failed of LobbyCreated events.

            // Remember that this player created the lobby and will be the host for the game:
            iAmTheHost = true;
            
            // Now Open the SelectedLobbyPanel in the UI:
            Singleton.Instance.UIManager.OpenSelectedLobbyMenu();
        }

        // This event is kicked of when the lobby creation fails for some reason. In that case return to the main menu.
        public void LobbyCreationFailed()
        {
            Debug.LogWarning("SteamLobby.LobbyCreationFailed(): Lobby creation failed.");

            iAmTheHost = false;
            
            Singleton.Instance.UIManager.OpenMainMenu();
        }
        
        
        // Clear the list of lobbies shown in the Join Lobby menu.
        private void ClearLobbyList()
        {
            GameObject lobbyParentObject = Singleton.Instance.UIManager.JoinLobbyParent;
            for (int i = lobbyParentObject.transform.childCount - 1; i >-1; --i)
            {
                Destroy(lobbyParentObject.transform.GetChild(i).gameObject);
            }
        }
        
        // This event is kicked of when the player created a new lobby.
        public void LobbyCreated(Lobby lobby)
        {
            Debug.Log("SteamLobby.LobbyCreated(): Lobby created [" + lobby.Name +"].");

            iCreatedALobby = lobby.IsOwner;
            iJoinedALobby = true;

            if (iCreatedALobby)
            {
                // I will be automatically ready
                lobby.IsReady = true;
            }
            
            // No need to show lobbies since you created and autojoined the lobby
            ClearLobbyList();
            
            // Now show the lobby details:
            selectedLobby = lobby;

            // Store the game map in a "global" component so we can easily reference it in later scenes:
            Dictionary<string, string> lobbyMetaData = selectedLobby.GetMetadata();
            string mapName = lobbyMetaData["map"];
            Singleton.Instance.GameDetails.GameMap = mapName;

            Invoke("ShowLobbyDetails",0.1f);
            // ShowLobbyDetails();
        }

        public void ShowLobbyDetails(Lobby lobby)
        {
            selectedLobby = lobby;
            ShowLobbyDetails();
        }
        
        public void ShowLobbyDetailsDelayed()
        {
            Invoke("ShowLobbyDetails",0.5f);
        }
        
        // Show the details (name, map, owner) of the joined / selected Lobby 
        public void ShowLobbyDetails()
        {
            if (selectedLobby != null)
            {
                Singleton.Instance.UIManager.SelectedLobbyName.text = "Lobby Name: " + selectedLobby.Name;

                // retrieve the Lobby owner from the Lobby:
                CSteamID owner = API.Matchmaking.Client.GetLobbyOwner(selectedLobby);
                // Then get the owner name:
                UserData ownerData = new CSteamID(owner.m_SteamID);

                Singleton.Instance.UIManager.SelectedLobbyOwner.text = "Lobby Owner: " + ownerData.Name;

                // Now get the meta data from the Lobby to display it in the UI:
                Dictionary<string, string> lobbyMetaData = selectedLobby.GetMetadata();
                string mapName = lobbyMetaData["map"];
                Singleton.Instance.UIManager.SelectedLobbyMap.text = "Map: " + mapName;

                // If all players reported ready through the Steam Lobby AND I created the lobby then enable
                // the Start Game button
                if (iCreatedALobby)
                {
                    Singleton.Instance.UIManager.StartTheGame.interactable = selectedLobby.AllPlayersReady;
                }
                else
                {
                    Singleton.Instance.UIManager.StartTheGame.interactable = false;
                }

                // Now show the Lobby members in the UI List:
                ShowLobbyMembers();
            }
            else Debug.LogError("SteamLobby.ShowLobbyDetails(): selectedLobby = null");
        }
     
        
        // Show all lobby members as a UI item in the Selected Lobby members list
        private void ShowLobbyMembers()
        {
            if (selectedLobby != null)
            {
                // Should first cleanup lobby members in the list
                GameObject selectedLobbyParentObject = Singleton.Instance.UIManager.SelectedLobbyParent;
                
                for (int i = selectedLobbyParentObject.transform.childCount - 1; i >-1; --i)
                {
                    Destroy(selectedLobbyParentObject.transform.GetChild(i).gameObject);
                }
                
                // Get all lobby members:
                LobbyMember[] members = selectedLobby.Members;

                foreach (LobbyMember lobbyMember in members)
                {
                    bool isReady = lobbyMember.IsReady;

                    // Create a UI item in the lobby members UI list, based on the selectedLobbyPlayerPrefab:
                    GameObject newPlayer = Instantiate(selectedLobbyPlayerPrefab, selectedLobbyParentObject.transform);
                    SteamLobbyPlayerItemUI steamLobbyPlayerItemUI = newPlayer.GetComponent<SteamLobbyPlayerItemUI>();

                    // Assign it the user and a flag whether/not the player is ready:
                    steamLobbyPlayerItemUI.UpdateItem(lobbyMember.user,isReady);

                    // the buttons for signaling the player is ready are linked to the player on this network client:
                    if (lobbyMember.user.IsMe)
                    {
                        Singleton.Instance.UIManager.SelectedLobbyPlayerReady.interactable = !isReady;
                        Singleton.Instance.UIManager.SelectedLobbyPlayerNotReady.interactable = isReady;
                    } 
                }
                
                // If I created the lobby then I will be able to Start the Game:
                if (iCreatedALobby)
                {
                    Singleton.Instance.UIManager.StartTheGame.interactable = selectedLobby.AllPlayersReady;
                }
            }
        }

        public void PlayerIsReady(bool value)
        {
            if (selectedLobby != null)
            {
                selectedLobby.IsReady = value;

                Singleton.Instance.UIManager.SelectedLobbyPlayerReady.interactable = !value;
                Singleton.Instance.UIManager.SelectedLobbyPlayerNotReady.interactable = value;
                
                ShowLobbyMembers();
            }
        }

        private void AddChatLine(string sender, string line)
        {
            Singleton.Instance.UIManager.TextChat.text += "\n"+sender+" > "+ line;
            
            int count = Singleton.Instance.UIManager.TextChat.text.Split('\n').Length;
            if (count > 8)
            {
                Singleton.Instance.UIManager.TextChat.text = sender + " > " + line;
            }
            
        }
        
        
        public void UserJoinedLobby()
        {
            AddChatLine("Lobby","A new user joined your lobby.");
            
            // Now refresh the lobby users
            ShowLobbyMembers();            
        }

        // This method is called whenever the meta data of the lobby gets updates.
        // Since the lobby(manager) remains active in later scenes (like the game) you may want to have different
        // logic for each scene.
        public void MetaDataUpdated()
        {
            Debug.Log("SteamLobby.MetaDataUpdated(): meta data updated");
            
            Scene scene = SceneManager.GetActiveScene();
            
            // Only update the Lobby Details when the main menu scene (Loader) is shown.
            if(scene.buildIndex ==0)
                ShowLobbyDetails();
        }
      
        
        public void HandleChatMessages(LobbyChatMsg message)
        {
            AddChatLine(message.sender.Name, message.Message);
        }

        // This methods is repeatedly invoked from the Start() method.
        public void SearchLobbies()
        {
            Debug.Log("SteamLobby.SearchLobbies(): SearchLobbies called.");
            
            if (!iCreatedALobby && !iJoinedALobby)
            {
                // Search for all lobbies currently hosted through Steam using the LobbyManager Search Arguments properties 
                lobbyManager.Search(10);
            }
        }
        
        // This event is called from the LobbyManager whenever a Search is initiated (also when the result set is empty).
        public void ReportSearchResults(Lobby[] results)
        {
            Debug.Log("SteamLobby.ReportSearchResults(): "+ results.Length+" Lobbies found.");

            // If the join Lobby menu is currently not shown in the UI it doesn't make sense to continue processing the results. 
            if (!Singleton.Instance.UIManager.JoinLobby.activeInHierarchy)
                return;
           
            // Should first cleanup lobbies in the list UI
            for (int i = Singleton.Instance.UIManager.JoinLobbyParent.transform.childCount - 1; i >-1; --i)
            {
                Destroy(Singleton.Instance.UIManager.JoinLobbyParent.transform.GetChild(i).gameObject);
            }

            // There are no results so exit:
            if (results.Length < 1)
                return;
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Found: " + results.Length+" lobbies.");

            // Now iterate through each results and create a UI item -based on lobbyItemPrefab- to hold the lobby information.
            // In the lobbyItemPrefab we can show details like the lobby name, owner, map etc + a button to join the lobby.
            
            for (int i = 0; i < results.Length; i++)
            {
                Lobby _lobby = results[i];
                
                var name = _lobby.Name;
                if (string.IsNullOrEmpty(name))
                    name = "UNKNOWN";
                sb.Append("\n " + _lobby.id + ", name = " + name);
                
                GameObject newLobby = Instantiate(lobbyItemPrefab, Singleton.Instance.UIManager.JoinLobbyParent.transform);
                SteamLobbyItemUI steamLobbyItemUI = newLobby.GetComponent<SteamLobbyItemUI>();
                if (steamLobbyItemUI)
                {
                    // Assign the current lobby to the UI item so it can update itself.
                    steamLobbyItemUI.UpdateLobbyItem(_lobby);
                }
                else Debug.LogError("SteamLobby.ReportSearchResults(): Cannot fine SteamLobbyItemUI component");
            }

            Debug.Log(sb.ToString());
        }
        

        public void StartGame()
        {
            /*
             * If I am the host:
             *  - networkManager StartHost
             *  - Lobby SetGameServer       -> this will kick of event for the clients -> startClient
             */

            if (iAmTheHost)
            {
                // Stop polling for lobbies:
                CancelInvoke();
                
                // Retrieve the NetworkManager:
                NetworkManager networkManager = Singleton.Instance.NetworkPostLobbyManager;

                networkManager.StartHost();
                
                // Get the user's Steam Id and send it to the other players so they know where to connect to
                var user = UserAPI.Id;
                lobbyManager.Lobby.SetGameServer(user.cSteamId);                
            }
            else Debug.LogWarning("SteamLobby.StartGame(): game started by client.");
        }

        public void LobbyGameServerEvent(LobbyGameServer lobbyGameServer)
        {
            // Everyone in the lobby gets this message (including the player hosting the game)
            // In the LobbyGameServer parameter we can find the steamId to connect to
            
            Debug.Log("SteamLobby.LobbyGameServerEvent(): game started on "+lobbyGameServer.id.ToString());
            
            if (!iAmTheHost)
            {
                // Stop polling for lobbies:
                CancelInvoke();
                
                // Get the SteamId of the player hosting the game and use it as teh host address:
                string hostAddress = lobbyGameServer.id.ToString();
                
                // Now let Mirror take it from here:
                NetworkManager networkManager = Singleton.Instance.NetworkPostLobbyManager;
                
                networkManager.networkAddress = hostAddress;
                networkManager.StartClient();
                
            }
            else Debug.LogWarning("SteamLobby.LobbyGameServerEvent(): method called for the host.");
        }
    }


}
