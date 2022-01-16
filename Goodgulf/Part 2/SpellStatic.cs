using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Goodgulf.Networking
{
    public class SpellStatic : NetworkBehaviour
    {
        // Use SpellStatic on the spell effect prefabs to give them a limited lifetime

        public float destroyDelaySeconds = 10;

        public override void OnStartServer()
        {
            // As soon a the object is created on the server we initiate a delayed delete:
            Invoke(nameof(DestroySelf), destroyDelaySeconds);
        }

        // destroy for everyone on the server
        [Server]
        void DestroySelf()
        {
            NetworkServer.Destroy(gameObject);
        }
    }

}
