using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace Goodgulf.Graphics
{
    public class MatchChatLine : MonoBehaviour
    {
        public float duration;
        public float fadeDuration;
        public bool deleteMe = false;

        private bool startCounting = false;
        private bool startFading = false;
        private float currentTime = 0f;
        private float currentFadeTime = 0f;
        private float alpha;

        private TMP_Text matchLine;
        
        public void StartDuration()
        {
            startCounting = true;
        }
        
        // Start is called before the first frame update
        void Start()
        {
            matchLine = GetComponent<TMP_Text>();
        }

        // Update is called once per frame
        void Update()
        {
            if(!startCounting)
                return;
            
            currentTime += Time.deltaTime;
            if (currentTime > duration)
                startFading = true;

            if (startFading)
            {
                if (currentFadeTime < fadeDuration)
                {
                    alpha = Mathf.Lerp(1f, 0f, currentFadeTime / fadeDuration);

                    matchLine.color = new Color(matchLine.color.r, matchLine.color.g, matchLine.color.b,
                        alpha);
                    currentFadeTime += Time.deltaTime;
                }
                else
                {
                    deleteMe = true;
                }
            }
        }
    }
}
