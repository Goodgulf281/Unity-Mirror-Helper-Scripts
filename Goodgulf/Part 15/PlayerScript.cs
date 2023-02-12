using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using FishNet.Object;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : NetworkBehaviour
{
    /*
     * This is an updated version of the PlayerScript from "Unity Networking - Mirror Part 5" which I converted
     * to Fishnet Networking. See: https://youtu.be/-6DS9wJH8fw
     *
     * This script is attached to the Player prefab and makes sure that the inputs and character controller
     * are only active on the client which owns the network instantiated player object. 
     * 
     */
    
    private CharacterController characterController;
    private PlayerInput playerInput;
    private ThirdPersonControllerCSP thirdPersonController;
    private Cheat cheat;

    private void Awake()
    {
        Debug.Log("PlayerScript.Awake(): event fired");

        // In the Awake function we disable all third party controller components

        thirdPersonController = GetComponent<ThirdPersonControllerCSP>();
        if (thirdPersonController)
        {
            thirdPersonController.enabled = false;
        }
        else Debug.LogError("PlayerScript.Awake(): ThirdPersonControllerCSP not found.");
        
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

        cheat = GetComponent<Cheat>();
        if (cheat)
        {
            cheat.enabled = false;
        }
        else Debug.LogError("PlayerScript.Awake(): Cheat not found.");
        
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("PlayerScript.OnStartClient(): event fired");

        if (base.IsOwner)
        {
            // Now we know this is the network instantiated object owned by the local client.
            // So let's enable the components to give the local client control over this object.
            
            if (playerInput)
            {
                Debug.Log("PlayerScript.OnStartClient(): enabling playerInput on local player.");
                playerInput.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartClient(): PlayerInput not found.");

            if (characterController)
            {
                Debug.Log("PlayerScript.OnStartClient(): enabling CharacterController on local player.");
                characterController.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartClient(): CharacterController not found.");

            if (thirdPersonController)
            {
                Debug.Log("PlayerScript.OnStartClient(): enabling ThirdPersonControllerCSP on local player.");
                thirdPersonController.enabled = true;
            }
            else Debug.LogError("PlayerScript.OnStartClient(): ThirdPersonControllerCSP not found.");

            if (cheat)
            {
                Debug.Log("PlayerScript.OnStartClient(): enabling cheat on local player.");
                cheat.enabled = true;
            }
            
            // Now we need to link the Cinemachine camera to this gameobject:
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
                else Debug.LogError("PlayerScript.OnStartClient(): CinemachineVirtualCamera component found.");

            }
            else Debug.LogError("PlayerScript.OnStartClient(): PlayerCameraRoot or PlayerFollowCamera not found.");

        }

    }
}
