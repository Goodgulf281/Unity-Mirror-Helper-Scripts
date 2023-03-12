using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Goodgulf.Networking
{
    public class NetworkedDoor : NetworkBehaviour
    {
        [SerializeField] private Animator Door = null;

        // This variable is synchronized between the server and all the clients and indicates teh state of the door (open or closed)
        [SyncVar(OnChange = nameof(On_IsDoorOpenChanged))]
        private bool IsDoorOpen = false;

        [ServerRpc(RequireOwnership = false)]
        public void RpcRequestDoorChangeState(bool OpenTheDoor)
        {
            // A client requests the door to be opened (OpenTheDoor==true) or closed (OpenTheDoor==false)
            if (IsDoorOpen != OpenTheDoor)
            {
                // We only need to act if:
                //  IsDoorOpen == true and OpenTheDoor == false (we want to close the door)
                //  IsDoorOpen == false and OpenTheDoor == true (we want to open the door)

                IsDoorOpen = OpenTheDoor;
                // This should trigger the OnChange event of the SyncVar on the client
            }
        }

        // This is the OnChange event which is triggered on the client (and the server where we ignore it)
        private void On_IsDoorOpenChanged(bool prev, bool next, bool asServer)
        {
            if (!asServer)
            {
                if (prev == false && next == true)
                {
                    // We're opening the door
                    Debug.Log("Opening Door");
                    // Now play the animation
                    Door.Play("DoorOpening", 0, 0.0f);
                }
                else if (prev == true && next == false)
                {
                    // We're opening the door
                    Debug.Log("Closing Door");
                    Door.Play("DoorClosing", 0, 0.0f);
                }
            }
        }

        public bool GetIsDoorOpen()
        {
            return IsDoorOpen;
        }
    


}

}
