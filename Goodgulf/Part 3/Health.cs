using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Goodgulf.Networking
{

    [System.Serializable]
    public class MyDeathEvent : UnityEvent<PlayerScript>// Define a Unity event with a parameter
    {
    }


    public class Health : NetworkBehaviour
    {
        public TextMesh hitPointsText;                  // The text floating above the player object showing hitpoints
                                                        // This text mesh was added to the Player prefab
        public TMP_Text myHitpointsText;                // The text showing local client's hitpoints in the top rigth of the UI

        public int maxHitPoints = 1000;                 // The maximum hit points the player gets at the start of the match

        [SyncVar(hook = nameof(OnHitPointsChanged))]    // The actual hit points with a hook to make changes on the client when the
        public int hitPoints = 0;                       //  hitpoints change

        public MyDeathEvent OnDeath;                    // The event called when the Health reaches zero

        private PlayerScript playerScript;              // A reference to the associated playerScript which we pass as a parameter to the
                                                        //  OnDeath event

        public override void OnStartServer()
        {
            base.OnStartServer();

            // On the server, award the hitpoints at the start of the game
            hitPoints = maxHitPoints;
            playerScript = GetComponent<PlayerScript>();
        }


        void OnHitPointsChanged(int _Old, int _New)
        {
            // Change Hitspoints on clients, update the hitpoints text floating above the other players
            hitPointsText.text = _New.ToString();
        }

        [Server]
        public void AddHitPoints (int value)
        {
            // We only allow the server at add or remove hitpoints
            if(value>0)
                hitPoints = Math.Max(hitPoints + value, maxHitPoints);
        }

        [Server]
        public void RemoveHitPoints(int value)
        {
            if (value > 0)
                hitPoints = Math.Max(hitPoints - value, 0);


            if(hitPoints == 0 && playerScript)
            {
                // Player Death, invoke the event and pass this player as an argument
                OnDeath.Invoke(playerScript);
            }
        }

        private void Update()
        {
            if(isLocalPlayer)
            {
                if(myHitpointsText)
                {
                    // Update the local player's hitpoints in the top right of the screen
                    myHitpointsText.text = "Hitpoints: " + hitPoints.ToString();
                }
            }
        }

        public override void OnStartLocalPlayer()
        {
            // Find the UI object representing the local player's hitpoints
            GameObject textHitpoints = GameObject.Find("textHitpoints");
            if (textHitpoints)
            {
                myHitpointsText = textHitpoints.GetComponent<TMP_Text>();

                if (myHitpointsText == null)
                    Debug.LogError("Health.Awake(): Cannot find TMP_Text.");
            }
            else Debug.LogError("Health.Awake(): Cannot find textHitpoints.");
        }

    }
}