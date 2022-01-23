using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Goodgulf.Networking
{
    public class SpellStatic : NetworkBehaviour
    {
        
        public float destroyDelaySeconds = 10;          // Use SpellStatic on the spell effect prefabs to give them a limited lifetime

        public float radius = 6.0f;                     // All within this radius will be damaged
        public float damagePeriodStartSeconds = 2.0f;   // Start handing out damage after this time
        public float damagePeriodInSeconds = 1.0f;      // Do damage at the start of every period
        public int   damagePerCycle = 100;              // The amount of damage every period


        public override void OnStartServer()
        {
            // As soon a the object is created on the server we initiate a delayed delete:
            Invoke(nameof(DestroySelf), destroyDelaySeconds);

            // Now invoke this damage dealing method on the server only
            InvokeRepeating(nameof(DoDamage), damagePeriodStartSeconds, damagePeriodInSeconds);
        }

        [Server]
        void DoDamage()
        {
            // Find all colliders withing the radius of the spell
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            foreach (var hitCollider in hitColliders)
            {
                // Find the Health script for the collider we found
                Health health = hitCollider.transform.gameObject.GetComponent<Health>();

                if(health)
                {
                    // The collider's object has a Health script so someone is inside the radius
                    // Do damage:
                    health.RemoveHitPoints(damagePerCycle);
                }
            }
        }

        // destroy for everyone on the server
        [Server]
        void DestroySelf()
        {
            NetworkServer.Destroy(gameObject);
        }
    }

}
