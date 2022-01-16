using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Goodgulf.Networking
{
    public class Startup : MonoBehaviour
    {
        public AudioMixer audioMixer;

        public void SetMasterVolume (float mvolume)
        {
            audioMixer.SetFloat("masterVol", mvolume);
        }
        
        void Awake()
        {
            // Get all the command line arguments for the executable
            // I'm using this target value for the shortcut I created for the game executable: "D:\Build\Mirror Networking.exe" -volume zero
            Dictionary<string, string> args = GetCommandLineArgs();

            if(args.TryGetValue("-volume",out string myarg)) 
            {
                if(myarg == "zero")
                {
                    // If we use "-volume zero" then the master volume is set to zero.
                    SetMasterVolume(-80.0f);
                }
            }
        }


        // Get command line arguments, code from:
        // https://pauliom.medium.com/command-line-arguments-in-unity-b30a5815cd88
        private Dictionary<string, string> GetCommandLineArgs()
        {
            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();

            var commandLineArgs = System.Environment.GetCommandLineArgs();

            for (int argumentIndex = 0; argumentIndex < commandLineArgs.Length; ++argumentIndex)
            {
                var arg = commandLineArgs[argumentIndex].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = argumentIndex < commandLineArgs.Length - 1 ?
                                commandLineArgs[argumentIndex + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;

                    argumentDictionary.Add(arg, value);
                }
            }
            return argumentDictionary;
        }

    }
}