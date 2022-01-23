using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Goodgulf.Networking
{

    public class GameTimer : NetworkBehaviour
    {
        [SyncVar]
        public float timer = 15 * 60;  // 15 minutes countdown timer

        private int minutes;
        private int seconds;
        public string showTime;         // The actual timer countdown showing in a 09:37 format
        private bool running = true;    // Is the timer stil running?

        private TMP_Text clockText;     // The timer text shown in the UI

        public UnityEvent ClockReady;   // The event which is called when the timer reaches zero


        void Awake()
        {
            Debug.Log("GameTimer.Awake(): start");

            // Find the UI object representing the time
            GameObject texttimer = GameObject.Find("textTimer");
            if (texttimer)
            {
                clockText = texttimer.GetComponent<TMP_Text>();

                if (clockText == null)
                    Debug.LogError("GameTimer.Awake(): Cannot find TMP_Text.");
            }
            else Debug.LogError("GameTimer.Awake(): Cannot find textTimer.");
        }

        void Update()
        {
            if (!running)
            {
                return;
            }

            // Decrease the timer value as time ticks by:
            if (timer > 0)
                timer -= Time.deltaTime;

            // Convert the timer to a string
            minutes = Mathf.FloorToInt(timer / 60F);
            seconds = Mathf.FloorToInt(timer - minutes * 60);
            showTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timer < 0)
            {
                running = false;
                showTime = "00:00";
                // Callback event when the timer reaches zero
                ClockReady.Invoke();
            }

            if (clockText)
            {
                clockText.text = showTime;
            }
            else Debug.LogError("GameTimer.Update(): timer = null.");
        }
    }
}