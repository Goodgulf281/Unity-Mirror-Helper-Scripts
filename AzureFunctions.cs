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

        private string _gameKey; // Use this key to encrypt/decrypt synchronously in the game.
        public string gameKey
        {
            get { return _gameKey; }
            set { _gameKey = value;  }
        }

        public Voices voices;   // Voices loaded from Azure
        
        public AudioSource audioSource; // AudioSource used for Voice Over        

        // To-do: enable after importing Steamworks
        // private UserData localUser;
        
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            GetGameKeyFunction().Forget();
            
            //EncryptString("This is a test",DebugStringLoadedCallback).Forget();
            //DecryptString("rjDMG6R0ro6/18Vjz+fBhg==",DebugStringLoadedCallback).Forget();
            
            GetAzureVoiceList().Forget();
            
            //Speak("This is a test").Forget();
            
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

        public async UniTaskVoid Test()
        {
            UnityWebRequest webRequest =
                UnityWebRequest.Get(
                    "https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==");
            var op = await webRequest.SendWebRequest();
            string result = op.downloadHandler.text;
                    
            Debug.Log("AzureFunctions.Test(): returns = "+result);
        }

        public async UniTaskVoid RegisterServer(ServerData serverData, ServerCallBack serverCallBack)
        {
            string json = JsonUtility.ToJson(serverData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=RegisterServer", "POST");

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
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=UpdateServer", "POST");

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
        
        
        public async UniTaskVoid StartServers(ServerCallBack serverCallBack)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=StartServers");

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
                    Debug.LogError("AzureFunctions.StartServers(): Timeout");
                }
            }     

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.StartServers():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server start signal sent successfully, "+webRequest.downloadHandler.text);
            }
            
        }
        
        
        public async UniTaskVoid StartServersWait(ServerCallBack serverCallBack)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=StartServersWait");

            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(120)); // 2min timeout.

            try
            {
                var result = await webRequest.SendWebRequest().WithCancellation(cts.Token);
                
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    Debug.LogError("AzureFunctions.StartServersWait(): Timeout");
                }
            }     

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.StartServersWait():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server start signal sent successfully, "+webRequest.downloadHandler.text);
            }
            
        }
        
        public async UniTaskVoid ConditionalShutdownServers(ServerCallBack serverCallBack)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=ConditionalShutdownServer");

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
                    Debug.LogError("AzureFunctions.ConditionalShutdownServers(): Timeout");
                }
            }     

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.ConditionalShutdownServers():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server start signal sent successfully, "+webRequest.downloadHandler.text);
            }
            
        }
        
        
        public async UniTaskVoid ForceShutdownServers(ServerCallBack serverCallBack)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=ForceShutdownServers");

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
                    Debug.LogError("AzureFunctions.ForceShutdownServers(): Timeout");
                }
            }     

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.ForceShutdownServers():"+ webRequest.error);
                serverCallBack(false, webRequest.error);
            }
            else
            {
                serverCallBack(true, "Server stop signal sent successfully, "+webRequest.downloadHandler.text);
            }
        }
        
        public async UniTaskVoid ServerList(ServerListCallBack serverListCallBack)
        {
           
            UnityWebRequest webRequest = UnityWebRequest.Get("https://unitygameservers20240630121211.azurewebsites.net/api/UnityGameServer?code=mMSgLo9vW2pa0avWJn6XxEsrU5yTK5WLdCj4Ur87wjyAAzFu3iiFnQ==&cmd=ListServers");
            
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
                    Debug.LogError("AzureFunctions.ServerList(): Timeout");
                }
            }     
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.ServerList():"+ webRequest.error);
                serverListCallBack(false, null);
            }
            else
            {
                ServerDataList serverList = JsonUtility.FromJson<ServerDataList>(webRequest.downloadHandler.text);
                serverListCallBack(true, serverList);
            }
        }
        
#endregion
        
#region Encryption
        
        public void GetGameKeyEvent()
        {
            GetGameKeyFunction().Forget();
        }

        public void DebugStringLoadedCallback(string fromAzure)
        {
            Debug.Log("AzureFunctions.DebugStringLoadedCallback(): fromAzure="+fromAzure);
        }
        
        
        public async UniTaskVoid GetGameKeyFunction()
        {
            UnityWebRequest webRequest =
                UnityWebRequest.Get(
                    "https://wizardbattlesapp1.azurewebsites.net/api/GameKey?code=NLTcbnRoRE/N6Rp5tFV7boqWGRTJYy7WwVQytjvw2PtAzCkhNalVZQ==");
            var op = await webRequest.SendWebRequest();
            _gameKey = op.downloadHandler.text;
            
            #if AZDEBUG            
                Debug.Log("AzureFunctions.GetGameKeyFunction(): GameKey = "+_gameKey);
            #endif
        }


        public async UniTaskVoid EncryptString(string inputString, StringLoadedCallback stringLoadedCallback)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(inputString);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/EncryptData?code=3X0uVlvOJr1ivwhDaaFC/4gkBAHKXoD1MWGXrdM2ASh8xy28UGXfzw==", "POST");

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
                    Debug.LogError("AzureFunctions.EncryptString(): Timeout");
                }
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.EncryptString():"+ webRequest.error);
            }
            else
            {
                webRequest.uploadHandler.Dispose();
                
                string encryptedString = webRequest.downloadHandler.text;
#if AZDEBUG            
                Debug.Log("AzureFunctions.GetGameKeyFunction(): EncryptedString = "+encryptedString);
#endif
                stringLoadedCallback(encryptedString);
            }
        }
        
        public async UniTaskVoid DecryptString(string inputString, StringLoadedCallback stringLoadedCallback)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(inputString);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/DecryptData?code=mZYT55FiCol7iKnhv6fM0fa2c8aWtmRqjNWa7KNJC0A6LyNlrOfmaA==", "POST");

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
                    Debug.LogError("AzureFunctions.DecryptString(): Timeout");
                }
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.DecryptString():"+ webRequest.error);
            }
            else
            {
                webRequest.uploadHandler.Dispose();
                
                string decryptedString = webRequest.downloadHandler.text;
#if AZDEBUG            
                Debug.Log("AzureFunctions.GetGameKeyFunction(): DecryptedString = "+decryptedString);
#endif
                stringLoadedCallback(decryptedString);
            }
        }
#endregion
        
#region Voice

        private TimeSpan GetFileAge(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            DateTime lastWrite = fileInfo.LastWriteTime;
                    
            TimeSpan age = DateTime.Now - lastWrite;
            return age;
        }

        public async UniTaskVoid GetAzureVoiceList()
        {
            string voicesJson;
            
            string outputPath = Path.Combine(Application.persistentDataPath, "voices.json");
#if AZDEBUG             
            Debug.Log("AzureFunctions.GetAzureVoiceList(): cached voice file age "+GetFileAge(outputPath).TotalDays+" days.");
#endif            
            if (File.Exists(outputPath) && GetFileAge(outputPath).TotalDays<15)
            {
#if AZDEBUG                     
                Debug.Log("AzureFunctions.GetAzureVoiceList(): loading cached voices.");
#endif
                voicesJson = System.IO.File.ReadAllText(outputPath);
                voices = JsonUtility.FromJson<Voices>(voicesJson);
            }
            else
            {
#if AZDEBUG                
                Debug.Log("AzureFunctions.GetAzureVoiceList(): retrieving voices from Azure.");
#endif
                voices = await GetAzureVoices();

                if (voices == null)
                {
                    Debug.LogError("AzureFunctions.GetAzureVoiceList(): received null from Azure.");
                    return;
                }
                voicesJson = JsonUtility.ToJson(voices);
                System.IO.File.WriteAllText(outputPath, voicesJson);
            }
#if AZDEBUG 
            Debug.Log("AzureFunctions.GetAzureVoiceList(): number if neural voices = "+voices.azureNeuralVoices.Count);
#endif
        }
        
        private async UniTask<Voices> GetAzureVoices()
        {
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://texttospeechappgoodgulf.azurewebsites.net/api/TextToSpeech?code=E9yxI4ZSI_WoSf9jnClA4B0ThsVXGTyubi7s0YAd0VzpAzFuFkNeyg==&cmd=voices", "POST");

            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(30)); // 10sec timeout.

            try
            {
                var result = await webRequest.SendWebRequest().WithCancellation(cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    Debug.LogError("AzureFunctions.GetAzureVoices(): Timeout");
                }
            }            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("AzureFunctions.GetAzureVoices():"+webRequest.error);
                return null;
            }
            Voices _voices = JsonUtility.FromJson<Voices>(webRequest.downloadHandler.text);
            return _voices;
        }
        
        public async UniTaskVoid Speak(string text, string voice = "en-GB-RyanNeural", string locale="en-GB" )
        {
            // First check if this text is cached

            string hashedFilename = StringCipher.HashString(voice+text)+".wav";
            string outputPath = Path.Combine(Application.persistentDataPath, hashedFilename);

            AudioClip audioClip;
           
            if (File.Exists(outputPath) && GetFileAge(outputPath).TotalDays < 25)
            {
#if AZDEBUG 
                Debug.Log("AzureFunctions.Speak(): <color=green>Loading cached sample.</color>");
#endif
                // Load cached file instead
                audioClip = await GetAudioClip(outputPath, AudioType.WAV);
            }
            else
            {
#if AZDEBUG 
                Debug.Log("AzureFunctions.Speak(): <color=yellow>Create sample.</color>");
#endif
                audioClip = await GetAzureSpeech(text, voice, locale, outputPath);
            }
           
            if (audioSource != null && audioClip != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(audioClip, 1.0f);
            }
            else Debug.LogError("AzureFunctions.Speak(): no speech result.");
         
        }
        
        
        
        private async UniTask<AudioClip> GetAzureSpeech(string text, string voice, string locale, string outputPath)
        {
            SpeechData speechData = new SpeechData();

            if (String.IsNullOrEmpty(voice))
            {
                if(voices==null)
                    voices = await GetAzureVoices();

                if (voices != null && voices.azureNeuralVoices.Count>0)
                {
                    // pick a random voice for selected locale
                    string selectedLocale;
                    
                    if (String.IsNullOrEmpty(locale))
                    {
                        Debug.LogWarning("AzureFunctions.GetAzureSpeech(): both voice and locale are empty, selecting en-GB");
                        selectedLocale = "en-GB";
                    }
                    else selectedLocale = locale;
                    
                    List<Voice> LocaleVoices = voices.azureNeuralVoices.Where(x => x.Locale==selectedLocale).ToList();

                    int index = UnityEngine.Random.Range(0,LocaleVoices.Count);

                    Debug.Log("AzureFunctions.GetAzureSpeech(): <color=blue>picked voice index "+index+"</color>");
                    speechData.Voice = LocaleVoices[index].ShortName;
                }
                else
                {
                    Debug.LogError("AzureFunctions.GetAzureSpeech(): empty voice list");
                    return null;
                }
            }
            else speechData.Voice = voice; 
            
            speechData.Lines = text;
            string json = JsonUtility.ToJson(speechData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://texttospeechappgoodgulf.azurewebsites.net/api/TextToSpeech?code=E9yxI4ZSI_WoSf9jnClA4B0ThsVXGTyubi7s0YAd0VzpAzFuFkNeyg==&cmd=speak", "POST");
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
                    Debug.LogError("AzureFunctions.GetAzureSpeech(): Timeout");
                }
            }     

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.GetAzureSpeech(): Error = "+webRequest.error);
                return null;
            }

            webRequest.uploadHandler.Dispose();
            
            string blobBase64 = webRequest.downloadHandler.text;
#if AZDEBUG   
            Debug.Log("AzureFunctions.GetAzureSpeech(): blobBase64.Length = "+blobBase64.Length);
#endif
            var outputStream = new MemoryStream(Convert.FromBase64String(blobBase64));
            FileStream file = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            outputStream.WriteTo(file);
            file.Close();
            
            AudioClip audioClip = await GetAudioClip(outputPath, AudioType.WAV);
            return audioClip;
        }
        
        private async UniTask<AudioClip> GetAudioClip(string filePath, AudioType fileType)
        {

            using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(filePath, fileType))
            {
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
                        Debug.LogError("AzureFunctions.GetAudioClip(): Timeout");
                    }
                }       

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError("AzureFunctions.GetAudioClip(): Error = "+ webRequest.error);
                    return null;
                }
                return DownloadHandlerAudioClip.GetContent(webRequest);
            }
        }
#endregion

#region Characters

public async UniTaskVoid CharacterListForPlayer(string owner, CharacterListCallBack characterListCallBack)
        {
           
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(owner);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/Characters?code=tLlUcBU506CLJcHg35nhZ2y9OaU/M/B7Q9T/UMVrcWXPw48fBMIU2A==&cmd=list", "POST");

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
                    Debug.LogError("AzureFunctions.CharacterListForPlayer(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.CharacterListForPlayer():"+ webRequest.error);
                characterListCallBack(false, null);
            }
            else
            {
                CharacterList characterList = JsonUtility.FromJson<CharacterList>(webRequest.downloadHandler.text);
                characterListCallBack(true, characterList);
            }
        }
        
        public async UniTaskVoid CharacterUpload(CharacterData characterData, CharacterCallBack characterCallBack)
        {
            string json = JsonUtility.ToJson(characterData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/Characters?code=tLlUcBU506CLJcHg35nhZ2y9OaU/M/B7Q9T/UMVrcWXPw48fBMIU2A==&cmd=upload", "POST");

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
                    Debug.LogError("AzureFunctions.CharacterUpload(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.CharacterUpload():"+ webRequest.error);
                characterCallBack(false, webRequest.error);
            }
            else
            {
                characterCallBack(true, "Character uploaded successfully");
            }
        }

        public async UniTaskVoid CharacterDownload(string guid, CharacterCallBack characterCallBack)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(guid);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/Characters?code=tLlUcBU506CLJcHg35nhZ2y9OaU/M/B7Q9T/UMVrcWXPw48fBMIU2A==&cmd=download", "POST");

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
                    Debug.LogError("AzureFunctions.CharacterUpload(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.CharacterUpload():"+ webRequest.error);
                characterCallBack(false, webRequest.error);
            }
            else
            {
                characterCallBack(true, webRequest.downloadHandler.text);
            }
        }

        public async UniTaskVoid CharacterDelete(string guid, CharacterCallBack characterCallBack)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(guid);
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/Characters?code=tLlUcBU506CLJcHg35nhZ2y9OaU/M/B7Q9T/UMVrcWXPw48fBMIU2A==&cmd=delete", "POST");

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
                    Debug.LogError("AzureFunctions.CharacterDelete(): Timeout");
                }
            }     

            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.CharacterDelete():"+ webRequest.error);
                characterCallBack(false, webRequest.error);
            }
            else
            {
                characterCallBack(true, webRequest.downloadHandler.text);
            }
        }
        
#endregion

#region Players

        public async UniTaskVoid RegisterPlayerDataInAzure()
        {
            PlayerData player = new PlayerData();
            
            // Update after installing Steamworks properly
            //player.playerName = localUser.Name;
            //player.playerID = localUser.id.ToString();
            player.os = "WIN";
#if UNITY_EDITOR_WIN
            player.os = "WIN";
#endif
#if UNITY_EDITOR_OSX
            player.os = "OSX";
#endif
#if UNITY_EDITOR_LINUX
            player.os = "LIN";
#endif

#if UNITY_STANDALONE_WIN
            player.os = "WIN";
#endif
#if UNITY_STANDALONE_OSX
            player.os = "OSX";
#endif
#if UNITY_STANDALONE_LINUX
            player.os = "LIN";
#endif

            string json = JsonUtility.ToJson(player);
#if AZDEBUG 
            Debug.Log("AzureFunctions.RegisterPlayerDataInAzure(): Player json = "+json);
#endif
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/RegisterPlayer?code=llBdaWsZmgqJjOS5E11b8uXqNr/idMahG7mynri0Bv/QJjyM62oH8w==", "POST");

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
                    Debug.LogError("AzureFunctions.RegisterPlayerDataInAzure(): Timeout");
                }
            }     
            webRequest.uploadHandler.Dispose();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.RegisterPlayerDataInAzure(): Error = "+webRequest.error);
            }
            else
            {
                Debug.Log("AzureFunctions.RegisterPlayerDataInAzure(): successfully posted Player Data: " + webRequest.downloadHandler.text);
            }
        }



        private async UniTaskVoid LogoutPlayerInAzure()
        {
            PlayerData player = new PlayerData();
            // Update after installing Steamworks properly 
            //player.playerName = localUser.Name;
            //player.playerID = localUser.id.ToString();
            
            string json = JsonUtility.ToJson(player);
#if AZDEBUG 
            Debug.Log("AzureFunctions.LogoutPlayerInAzure(): Player json = "+json);
#endif
            
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("https://wizardbattlesapp1.azurewebsites.net/api/LogoutPlayer?code=87LdzbS8vKMzZsdamWRFRsEVqcyL9Kuik/F7a3W69W2thnnt67Kyaw==", "POST");
            
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
                    Debug.LogError("AzureFunctions.LogoutPlayerInAzure(): Timeout");
                }
            }     
            webRequest.uploadHandler.Dispose();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("AzureFunctions.LogoutPlayerInAzure(): Error = "+webRequest.error);
            }
            else
            {
                Debug.Log("AzureFunctions.LogoutPlayerInAzure(): Successfully logged out Player: " + webRequest.downloadHandler.text);
            }
        }           
        
#endregion


    }

}