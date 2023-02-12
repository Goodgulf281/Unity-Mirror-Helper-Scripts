using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class Cheat : MonoBehaviour
{
    /*
     * This is the new version of the cheat code script which uses the Unity's Input System.
     * Recommended viewing on how to use the system: https://youtu.be/m5WsmlEOFiA 
     * 
     * Attach this script to the player prefab.
     */
    private PlayerInput playerInput;
    private InputAction cheatAction;

    private void Awake()
    {
        // Get a reference to the Player Input component attached to the player prefab:
        playerInput = GetComponent<PlayerInput>();
        
        // Create a reference to the action label "Cheat" as defined StarterAssets (Input Action Asset) 
        cheatAction = playerInput.actions["Cheat"];

        // Add a listener to the Key Pressed event for the "Cheat" action (linked to the F2 key)
        cheatAction.performed += CheatKeyPressed;
    }

    private void OnDisable()
    {
        // Remove the listener on exit:
        cheatAction.performed -= CheatKeyPressed;
    }
    
    
    private void CheatKeyPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Cheat key pressed");
        
        // The cheat key is pressed so move the character forwards
        transform.position += transform.forward*3;
        
    }

}
