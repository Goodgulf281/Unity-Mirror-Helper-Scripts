using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Goodgulf.Utilities;
//using HeathenEngineering.SteamworksIntegration;

namespace Goodgulf.Azure
{
    
    [Serializable]
    public class ServerData
    {
        public string   id;
        public string   ipAddress;
        public int      port;
        public string   scene;
        public int      playerCount;
    }

    [Serializable]
    public class ServerDataList
    {
        public List<ServerData> data;
    }
    
    
    [Serializable]
    public class CharacterData
    {
        public string characterName;
        public string characterOwner;
        public string characterGuid;
        
        public string characterRace;
        public string characterGender;
        public string characterProfession;      
    
        public string characterBlob;
        public string characterCreationDate;
    }

    [Serializable]
    public class CharacterList
    {
        public List<CharacterData> characters;
    }
    
    

    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public string playerID;
        public string os;
    }
    
    [Serializable]
    public class SpeechData
    {
        public string Voice;
        public string Lines;
    }
    
    [Serializable]
    public class Voice
    {
        public string Name;
        public string DisplayName;
        public string ShortName;
        public string Gender;
        public string Locale;
        public string VoiceType;
        public string[] StyleList;
        public string Status;
    }

    [Serializable]
    public class Voices
    {
        public List<Voice> azureVoices;
        public List<Voice> azureNeuralVoices;
        public List<Voice> azureNeuralVoicesWithStyles;
        public List<string> locales;
    }

    #region CallBackDefinitions
        
    public delegate void StringLoadedCallback(string fromAzure);

    public delegate void GetGameKeyCallBack();

    public delegate void CharacterCallBack(bool success, string fromAzure);
        
    public delegate void CharacterListCallBack(bool success, CharacterList characterList);

    public delegate void ServerCallBack(bool success, string fromAzure);

    public delegate void ServerListCallBack(bool success, ServerDataList listOfServerData);
    
    #endregion
    
    
    public class AzureFunctions : MonoBehaviour
    {
        // https://gamedevbeginner.com/singletons-in-unity-the-right-way/
        public static AzureFunctions Instance { get; private set; }

        public Voices voices;   // Voices loaded from Azure
        
        public AudioSource audioSource; // AudioSource used for Voice Over        
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;


        }

#region ApplicationQuit
        
        public void GenericServersCallBack(bool success, string fromAzure)
        {
            Debug.Log($"AzureFunctions:GenericServersCallBack(): registered {success} with response {fromAzure}");
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            Debug.Log("On Destroy called");
            OnApplicationQuit();
#endif    
        }
        
        void OnApplicationQuit()
        {
            Debug.Log("Application ending after " + Time.time + " seconds");
            
            // Send an azure function request to check if there are more clients, if not, shutdown all servers
            AzureFunctions az = AzureFunctions.Instance;
            az.ConditionalShutdownServers(GenericServersCallBack).Forget();
        }
#endregion
        
#region ServerManagement


        public async UniTaskVoid RegisterServer(ServerData serverData, ServerCallBack serverCallBack)
        {
            string json = JsonUtility.ToJson(serverData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://", "POST"); //Removed the Azure function connection string

            webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
            
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(10)); // 10sec timeout.

            try
            {
                var result = await webRequest.SendWebRequest().WithCancellation(cts.Token);
                
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    Debug.LogError("AzureFunctions.RegisterServer(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.RegisterServer():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server registered successfully, "+webRequest.downloadHandler.text);
            }
            
        }

        public async UniTaskVoid UpdateServer(ServerData serverData, ServerCallBack serverCallBack)
        {
            string json = JsonUtility.ToJson(serverData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://", "POST"); //Removed the Azure function connection string

            webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
            
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(10)); // 10sec timeout.

            try
            {
                var result = await webRequest.SendWebRequest().WithCancellation(cts.Token);
                
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    Debug.LogError("AzureFunctions.UpdateServer(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.UpdateServer():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server updated successfully, "+webRequest.downloadHandler.text);
            }
            
        }
        
    }

}