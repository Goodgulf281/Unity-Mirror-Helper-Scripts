using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using Goodgulf.Graphics;
using Invector.vCharacterController;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Goodgulf.Networking
{

    public enum ChatType
    {
        ChatSay,
        ChatShout
    }
    

    public class MatchChat : NetworkBehaviour
    {
        public static MatchChat Instance { get; private set; }

        [BoxGroup("Input")] 
        public TMP_InputField inputLine;
        [BoxGroup("Input")] public 
        Button inputShoutButton;
        [BoxGroup("Input")] public 
        Button inputSayButton;
        
        [BoxGroup("Input")] [SerializeField] 
        private bool inputFieldEnabled = false;

        [BoxGroup("Output")] 
        public GameObject chatLinePrefab;
        [BoxGroup("Output")] 
        public Transform parentForChatLines;
        [BoxGroup("Output")] 
        public float duration;
        [BoxGroup("Output")] 
        public float fadeDuration;

        [BoxGroup("Chat Settings")] 
        public float sayRange = 25f;

        private GameObject localOwner;
        private vThirdPersonInput playerInputScriptOwner;
        private bool disableInput = false;
        
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            
            inputLine.gameObject.SetActive(inputFieldEnabled);
            inputShoutButton.gameObject.SetActive(inputFieldEnabled);
            inputSayButton.gameObject.SetActive(inputFieldEnabled);
        }


        public void AssignLocalOwner(GameObject owner)
        {
            localOwner = owner;
        }
        
        public void AssignPlayerScript(vThirdPersonInput playerInputScript)
        {
            playerInputScriptOwner = playerInputScript;
        }

        // Use DisableInput() in other scripts checking for pressed keys to pause these until inputs are enabled again
        public bool DisableInput()
        {
            return disableInput;
        }
        
        [Client]
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Period))
            {
                inputFieldEnabled = !inputFieldEnabled;
                inputLine.gameObject.SetActive(inputFieldEnabled);
                inputShoutButton.gameObject.SetActive(inputFieldEnabled);
                inputSayButton.gameObject.SetActive(inputFieldEnabled);
                
                // Also disable this player's Input
                playerInputScriptOwner.enabled = !inputFieldEnabled;
                disableInput = inputFieldEnabled;
            }
        }

        public void OnInputEnterShout()
        {
            // This method is called by the Shout button OnClick() event
            RpcSendChatLine(inputLine.text, localOwner, ChatType.ChatShout);
            inputLine.text = "";
        }

        public void OnInputEnterSay()
        {
            // This method is called by the Say button OnClick() event
            RpcSendChatLine(inputLine.text, localOwner, ChatType.ChatSay);
            inputLine.text = "";
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcSendChatLine(string line, GameObject Sender, ChatType chatType)
        {
            RpcSendChatLineToAllObservers(line, Sender, chatType);
        }

        [ObserversRpc]
        private void RpcSendChatLineToAllObservers(string line, GameObject Sender, ChatType chatType)
        {
            // Debug.Log("MatchChat.RpcSendChatLineToAllObservers(): Chat received: "+line);

            if (chatType == ChatType.ChatSay)
            {
                // Check proximity with owner and receiving network object
                if (Vector3.Distance(localOwner.transform.position, Sender.transform.position) > sayRange)
                    return;
            }
            
            
            // Collect all existing chat lines
            int childrenCount = parentForChatLines.childCount;
            
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < childrenCount; i++)
            {
                GameObject child = parentForChatLines.GetChild(i).gameObject;
                TMP_Text childText = child.GetComponent<TMP_Text>();
                if (childText != null)
                {
                    children.Add(child);
                }
            }

            // Move existing lines up and delete EOL chat lines
            for (int i=children.Count - 1; i >= 0;i--)
            {
                GameObject child = children[i];
                MatchChatLine mLine = child.GetComponent<MatchChatLine>();
                if (mLine.deleteMe)
                {
                    children.RemoveAt(i);
                    Destroy(child);
                }
                else
                {
                    // This is a chatline so move it up
                    RectTransform rectTransform = child.GetComponent<RectTransform>();
                    Vector2 position = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = new Vector2(position.x, position.y + 35);
                }
            }
            
            // Create the new chat line from the prefab chatLinePrefab
            GameObject chatLine = Instantiate(chatLinePrefab, parentForChatLines);
            TMP_Text tmpText = chatLine.GetComponent<TMP_Text>();
            tmpText.text = line;

            MatchChatLine newMatchChatLine = chatLine.GetComponent<MatchChatLine>();
            newMatchChatLine.duration = duration;
            newMatchChatLine.fadeDuration = fadeDuration;
            newMatchChatLine.StartDuration();
        }
        
    }



}