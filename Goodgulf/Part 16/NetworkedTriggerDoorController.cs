using System.Collections;
using System.Collections.Generic;
using Crest;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;


namespace Goodgulf.Networking
{

    public class NetworkedTriggerDoorController : MonoBehaviour
    {
        // These triggers indicate the action of the trigger: open or close the door when the player
        // enters this trigger
        [SerializeField] private bool openTrigger = false;
        [SerializeField] private bool closeTrigger = false;

        // Link to the NetworkDoor script which will do the actual opening/closing of the door
        [SerializeField] private NetworkedDoor networkedDoor = null;
        
        private void Awake()
        {
            if(networkedDoor==null)
                Debug.LogError("NetworkedTriggerDoorController.Awake(): networkedDoor not setup.");
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Door trigger entered by "+other.tag);
            
            if (other.CompareTag("Player"))
            {
                // We've made sure it's a player triggering the event
                bool IsDoorOpen = networkedDoor.GetIsDoorOpen();
                
                // Now check if we actually need to do something (so we don't try to open an already open door)
                if (openTrigger && !IsDoorOpen)
                {
                    Debug.Log("Request Opening Door from trigger");
                    
                    // Call the server RPC:
                    networkedDoor.RpcRequestDoorChangeState(true);
                }
                else if (closeTrigger && IsDoorOpen)
                {
                    Debug.Log("Request Closing Door from trigger");
                    networkedDoor.RpcRequestDoorChangeState(false);

                }
            }
        }
    }
}
