using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

namespace Goodgulf.Networking
{

    public class NetworkManagerMyGame : NetworkManager
    {
        public GameObject prefabTimer;
        private GameObject timer;

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            // Call the base method which actually spawns the players on the playe spawn locations
            base.OnServerAddPlayer(conn);

            // Now add the Death Event to the Health script
            GameObject player = conn.identity.gameObject;       // First get the player object linked to the newly added connection
            Health health = player.GetComponent<Health>();      // Get its Health Component
            if (health)
            {
                PlayerScript playerScript = player.GetComponent<PlayerScript>();
                health.OnDeath.AddListener(playerScript.PlayerDied);         // Link the event to the PlayerDied event (see below)
            }
            else Debug.Log("NetworkManagerMyGame.OnServerAddPlayer(): cannot find Health on object");



            // spawn a game timer as soon as we have two players
            if (numPlayers == 2)
            {
                Debug.Log("NetworkManagerMyGame.OnServerAddPlayer(): spawn timer.");

                // The Timer prefab is picked from the registered spawnable prefabs
                timer = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "GameTimer"));

                // Add a callback when the timer reaches 0
                GameTimer gameTimer = timer.GetComponent<GameTimer>();
                if (gameTimer)
                    gameTimer.ClockReady.AddListener(EndOfTimer);
                
                // Now spawn it on all clients
                NetworkServer.Spawn(timer);
            }
        }

        public void EndOfTimer()
        {
            Debug.Log("NetworkManagerMyGame.EndOfTimer(): timer ready.");

            // End of match code here
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // destroy time
            if (timer != null)
                NetworkServer.Destroy(timer);

            // call base functionality (actually destroys the player)
            base.OnServerDisconnect(conn);
        }


    }
}