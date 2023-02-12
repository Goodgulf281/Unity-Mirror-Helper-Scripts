using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cheat : MonoBehaviour
{
    /*
     * This is a simple cheat script which waits for the 1 key to be pressed then moves the player forward.
     * This is done outside of the regular character controller's input/move process and would be a typical
     * method to cheat in a game.
     * 
     */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.LogWarning("Cheating on Client!");
            transform.position += transform.forward*3;
        }
    }
}
