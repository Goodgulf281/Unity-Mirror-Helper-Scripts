using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkInputControl : MonoBehaviour
{
    /*
     * This is a simple script which listens to the key bound to the "Network" action (F1 key).
     * Since we haven't instantiated a player prefab in the scene yet (the network hasn't started),
     * we'll need an alternative object to host the PlayerInput script. This is what the GetInput 
     * object in the scene is for: temporarily host the PlayerInput.
     * Once the network is started this object can be deactivated.
     *
     * Attach this script to the GetInput object.
     * 
     * This script uses the Unity Input System:
     * To see how the New Input System works, check out this video: https://youtu.be/m5WsmlEOFiA
     */

    private PlayerInput playerInput;
    private InputAction networkAction;
   
    private void Awake()
    {
        // Get a reference to the Player Input component attached to the GetInput object:
        playerInput = GetComponent<PlayerInput>();
        
        // Create a reference to the action label "Network" as defined StarterAssets (Input Action Asset) 
        networkAction = playerInput.actions["Network"];

        // Add a listener to the Key Pressed event for the "Network" action (linked to the F1 key)
        networkAction.performed += NetworkKeyPressed;
    }

    private void OnDisable()
    {
        networkAction.performed -= NetworkKeyPressed;
    }

    private void NetworkKeyPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Network key pressed");
        
        // now start Fishnet network
        GameObject sn = GameObject.Find("NetworkManager");
        
        StartNetwork startNetwork = sn.GetComponent<StartNetwork>();
        startNetwork.StartNetworking();

        // Deactivate this object since we want to stop the temporary PlayerInput script on the GetInput object.
        this.gameObject.SetActive(false);
        
    }
    

}
