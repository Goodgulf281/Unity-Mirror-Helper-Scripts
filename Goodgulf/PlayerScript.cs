using Invector.vCharacterController;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Networking
{

    public class PlayerScript : NetworkBehaviour
    {
        public AudioSource announce;                    // This clip will be played when a player enters the game. The audio source needs to be tagged as "Announce".

        public vThirdPersonCamera cameraPrefab;         // Assign a reference to the Invector camera prefab to this property.

        private vThirdPersonInput _vThirdPersonInput;   // Reference to the Invector Input system which we disable on all players except the Local Player.
        private vThirdPersonCamera _vThirdPersonCamera; // Reference to the Invector camera we spawn based on the above prefab.

        void Awake()
        {
            Debug.Log("PlayerScript.Awake(): event fired");

            // Find the announcement audio source component in the hierarchy.
            announce = GameObject.FindGameObjectWithTag("Announce").GetComponent<AudioSource>();

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

        }
    }
}