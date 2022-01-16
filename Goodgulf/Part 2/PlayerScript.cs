using Invector.vCharacterController;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Networking
{

    public class PlayerScript : NetworkBehaviour
    {
        public TextMesh playerNameText;                 // The text floating above the player object showing its name
        public GameObject floatingInfo;                 // The placeholder object containing above playerNameText

                                                        // SyncVars are properties of classes that inherit from NetworkBehaviour,
                                                        //  which are synchronized from the server to clients. 
        [SyncVar(hook = nameof(OnNameChanged))]         // You can consider the hook to be similar to the setter, it gets called when the value changes on the client
        public string playerName;                       // Player name which we'll float over the player object

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;         // The color of the floating playerNameText

        public KeyCode spellKey1 = KeyCode.Alpha1;      // The keyboard commands we'll use (for now) to trigger spells
        public KeyCode spellKey2 = KeyCode.Alpha2;
        public KeyCode spellKey3 = KeyCode.Alpha3;

        public GameObject spell1Prefab;                 // The prefabs to be instantiated when we trigger a spell
        public GameObject spell2Prefab;                 // Note: these prefabs also need to be registered in the NetworkManager as spawnable prefab
        public GameObject spell3Prefab;

        public AudioSource announce;                    // This clip will be played when a player enters the game. The audio source needs to be tagged as "Announce".

        public vThirdPersonCamera cameraPrefab;         // Assign a reference to the Invector camera prefab to this property.

        private vThirdPersonInput _vThirdPersonInput;   // Reference to the Invector Input system which we disable on all players except the Local Player.
        private vThirdPersonCamera _vThirdPersonCamera; // Reference to the Invector camera we spawn based on the above prefab.

        void Awake()
        {
            Debug.Log("PlayerScript.Awake(): event fired");

            // Find the announcement audio source component in the hierarchy sicne we can't assign this to the non instantiated prefab.
            announce = GameObject.FindGameObjectWithTag("Announce").GetComponent<AudioSource>();

            // Find the Invector Input Manager component
            _vThirdPersonInput = GetComponent<vThirdPersonInput>();
            if (_vThirdPersonInput)
            {
                // Disable input by default, enable later for local players in OnStartLocalPlayer() event.
                // If we don't do this the input handler script will mess up the 3rd person camera setup.
                _vThirdPersonInput.enabled = false;
            }
            else Debug.LogError("PlayerScript.Start: could not find vThirdPersonInput.");
        }

        public override void OnStartLocalPlayer()
        {
            Debug.Log("PlayerScript.OnStartLocalPlayer(): event fired");

            // Deactivate the main camera (which we see at the start of the scene) since the 3rd person camera needs to take over now.
            Camera.main.gameObject.SetActive(false);

            // Debug.Log("PlayerScript.OnStartLocalPlayer(): isLocalPlayer = "+isLocalPlayer);

            // Now we instanatiate the third person camera for the local player only:
            _vThirdPersonCamera = Instantiate<vThirdPersonCamera>(cameraPrefab, new Vector3(0, 0, 0), Quaternion.identity);

            if (_vThirdPersonCamera)
            {
                Debug.Log("PlayerScript.OnStartLocalPlayer(): setting main camera to localPlayer");

                // Now link the camera to this player:
                _vThirdPersonCamera.SetMainTarget(this.transform);
            }
            else Debug.Log("PlayerScript.OnStartLocalPlayer(): _vThirdPersonCamera = null");

            if (_vThirdPersonInput)
            {
                // Since this is the local player we'll need to enable the input handler
                _vThirdPersonInput.enabled = true;
            }

            if(announce)
            {
                announce.Play();
            }


            // For the local player - hide the the placeholder object for the playerNameText (above the player)
            floatingInfo.transform.localPosition = new Vector3(0, -10.0f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // Pick a random name and color for the player
            string name = "Player" + Random.Range(100, 999);
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            CmdSetupPlayer(name, color);
        }

        // These two "events" are called by the SyncVar hook when the value changes
        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
        }

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            // Command: call this from a client to run this function on the server.
            // So the player sends info to server, then server updates sync vars which handles it on all clients
            playerName = _name;
            playerColor = _col;
        }

        void Update()
        {
            if (!isLocalPlayer)
            {
                // make non-local players run this
                floatingInfo.transform.LookAt(Camera.main.transform);
                return;
            }
            else
            {
                // Check the input from the user. A bit of dirty code to be updated later
                if (Input.GetKeyDown(spellKey1))
                {
                    CmdSpell(1);
                }
                else
                if (Input.GetKeyDown(spellKey2))
                {
                    CmdSpell(2);
                }
                if (Input.GetKeyDown(spellKey3))
                {
                    CmdSpell(3);
                }

            }
        }

        // This command is run on the server: instantiate one of the spell prefabs then spawn these on all clients
        [Command]
        void CmdSpell(int s)
        {
            GameObject spell;
            if (s == 1)
            {
                spell = Instantiate(spell1Prefab, this.transform.position, this.transform.rotation);
            }
            else if(s == 2)
            {
                spell = Instantiate(spell2Prefab, this.transform.position+this.transform.forward*2, this.transform.rotation);
            }
            else spell = Instantiate(spell3Prefab, this.transform.position + this.transform.forward * 3, this.transform.rotation);

            NetworkServer.Spawn(spell); // Spawn on all clients
            RpcOnSpell();               // Call any code that needs to run on the clients
        }

        // this is called on the player that cast a spell for all observers
        [ClientRpc]
        void RpcOnSpell()
        {
            // The server uses a Remote Procedure Call (RPC) to run that function on clients.
            // Do something
        }


    }
}