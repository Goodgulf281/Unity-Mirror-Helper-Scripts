using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Goodgulf.Gamelogic 
{
    public class Bootstrap : MonoBehaviour
    {
        public float waitingTime = 2.0f;

        private float timer = 0f;
        private bool timerSwitch = true;

        void Update()
        {
            if (!timerSwitch)
                return;

            timer += Time.deltaTime;

            if (timer > waitingTime)
            {
                timerSwitch = false;
                LoadMainMenu();
            }
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene(1);
        }
    }
}
