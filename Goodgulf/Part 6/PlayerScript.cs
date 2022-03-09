using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Cinemachine;
using StarterAssets;
using UnityEngine.UI;

namespace Goodgulf.Networking
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkAnimator))]
    public class PlayerScript : NetworkBehaviour
    {
        /*
         * Attach this script to the PlayerArmature prefab in StarterAssets>ThirdPersonController>Prefabs
         * 
         * This script will disable the third person controller components on Awake() and only
         * enable them in the OnStartLocalPlayer. Effectively only the local player can "command" this
         * object while the other players only see it move & animate.
         * 
         * The scipt also holds the player name (shown in the UI at the top left, the txtPlayer holds the reference to the UI element)
         */

        // These three third person controller elements need to be switched off for non local player instances:
        private CharacterController characterController;
        private PlayerInput playerInput;
        private ThirdPersonController thirdPersonController;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;               // Store the player's name in this SyncVar

        private Text txtPlayerName;             // Store the reference to the UI elment showing the player's name

        private void Awake()
        {
            // Disable all third party controller components
            characterController = GetComponent<CharacterController>();
            if (characterController)
            {
                characterController.enabled = false;
            }
            else Debug.LogError("PlayerScript.Awake(): CharacterController not found.");

            playerInput = GetComponent<PlayerInput>();
            if (playerInput)
            {
                playerInput.enabled = false;
            }
            else Debug.LogError("PlayerScript.Awake(): PlayerInput not found.");

            thirdPersonController = GetComponent<ThirdPersonController>();
            if (thirdPersonController)
            {
                thirdPersonController.enabled = false;
            }
            else Debug.LogError("PlayerScript.Awake(): ThirdPersonController not found.");

            // Get the reference to the UI element which will show the Player's name
            txtPlayerName = GameObject.Find("txtPlayerName").GetComponent<Text>();
            if (!txtPlayerName)
            {
                Debug.LogError("PlayerScript.Awake(): txtPlayerName not found.");
            }
        }

        public override void OnStartLocalPlayer()
        {
            // Now we know this is the local player so re-enable the third part controller components

            if (playerInput)
            {
                Debug.Log("PlayerScript.OnStartLocalPlayer(): enabling playerInput on local player.");
                playerInput.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartLocalPlayer(): PlayerInput not found.");

            if (characterController)
            {
                Debug.Log("PlayerScript.OnStartLocalPlayer(): enabling CharacterController on local player.");
                characterController.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartLocalPlayer(): CharacterController not found.");

            if (thirdPersonController)
            {
                Debug.Log("PlayerScript.OnStartLocalPlayer(): enabling ThirdPersonController on local player.");
                thirdPersonController.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartLocalPlayer(): ThirdPersonController not found.");

            // Link the camera to this instance

            GameObject playerCameraRoot = this.transform.Find("PlayerCameraRoot").gameObject;
            
            GameObject playerFollowCamera = GameObject.Find("PlayerFollowCamera");

            if(playerCameraRoot && playerFollowCamera)
            {
                CinemachineVirtualCamera cinemachineVirtualCamera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
                if (cinemachineVirtualCamera)
                {
                    // Let the camera follow this local player
                    cinemachineVirtualCamera.Follow = playerCameraRoot.transform;

                    Vector3 newCameraPosition = new Vector3(this.transform.position.x+0.2f, 1.375f, this.transform.position.z-4.0f);

                    // change the camera position close to the player's position
                    playerFollowCamera.transform.SetPositionAndRotation(newCameraPosition, this.transform.rotation);

                }
                else Debug.LogError("PlayerScript.OnStartLocalPlayer(): CinemachineVirtualCamera component found.");

            }
            else Debug.LogError("PlayerScript.OnStartLocalPlayer(): PlayerCameraRoot or PlayerFollowCamera not found.");

            // Show the player's name (we get from the Room Player) in the UI
            if (txtPlayerName)
            {
                txtPlayerName.text = "Player: " + playerName;
            }
        }

        #region SyncVar Hooks

        void OnNameChanged(string _Old, string _New)
        {
            Debug.Log("PlayerScript.OnNameChanged() from " + _Old + " to " + _New);
        }

        #endregion

        void Update()
        {

        }
    }
}
