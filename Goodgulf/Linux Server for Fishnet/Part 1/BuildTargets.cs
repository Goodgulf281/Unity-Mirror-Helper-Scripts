using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Goodgulf.Editor
{



    public class BuildTargets : IActiveBuildTargetChanged
    {

        [MenuItem("Build/All Goodgulf Builds")]
        public static void MyBuild()
        {
            CreateWindowsServerBuild();
            CreateLinuxServerBuild();
            CreateWindowsBuild();
        }

        [MenuItem("Build/Goodgulf Linux Server Build")]
        public static void MyLinuxServerBuild()
        {
            CreateLinuxServerBuild();
        }

        [MenuItem("Build/Goodgulf Windows Server Build")]
        public static void MyWindowsServerBuild()
        {
            CreateWindowsServerBuild();
        }

        [MenuItem("Build/Goodgulf Windows Standalone Build")]
        public static void MyWindowsBuild()
        {
            CreateWindowsBuild();
        }


        private static List<string> RequiredScenes()
        {
            // I'm using a scriptable object which contains all the scenes we need to put into the builds.
            // This way I can update it in one spot instead of each method below.
            
            ScenesRequiredInBuildSO scenesRequiredInBuildSo = Resources.Load<ScenesRequiredInBuildSO>("buildScenesSO");

            List<string> result = new List<string>();

            foreach (var requiredScene in scenesRequiredInBuildSo.requiredScenes)
            {
                result.Add("Assets/Scenes/"+requiredScene.name+".unity");
                Debug.Log("Adding scene "+requiredScene.name+"  to build list");
            }

            return result;
        }
        
        // This function was intended to switch back to teh default Windows Standalone client build after each build action.
        // Unfortunately that is not possible yet using Unity code so always check the build target after running a series of builds. 
       
        private static void ResetBuildTarget()
        {
            /*
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/Bootstrap.unity", "Assets/Scenes/ServerSelect.unity", "Assets/Scenes/Island.unity" };
            buildPlayerOptions.locationPathName = "D:/Build/Wiba3-startV2/WiBa3-startV2.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;

            buildPlayerOptions.options = BuildOptions.DetailedBuildReport;
            */

            bool result =  EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows);

        }

        public int callbackOrder { get { return 0; } }
        
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            Debug.Log("Active platform changed from " + previousTarget + " to " + newTarget);
        }

        

        private static void CreateWindowsBuild()
        {
            Debug.Log("Creating Windows Client Build");
            
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            //buildPlayerOptions.scenes = new[]
            //    {"Assets/Scenes/Bootstrap.unity", "Assets/Scenes/ServerSelect.unity", "Assets/Scenes/Island.unity"};

            buildPlayerOptions.scenes = RequiredScenes().ToArray();
            
            buildPlayerOptions.locationPathName = "D:/Build/Wiba4-client/WiBa4-client.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;

            buildPlayerOptions.options = BuildOptions.DetailedBuildReport;
            buildPlayerOptions.subtarget = (int) StandaloneBuildSubtarget.Player;

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            PostBuild(buildReport);
        }

        private static void CreateWindowsServerBuild()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            //buildPlayerOptions.scenes = new[]
            //    {"Assets/Scenes/Bootstrap.unity", "Assets/Scenes/ServerSelect.unity", "Assets/Scenes/Island.unity"};
            
            buildPlayerOptions.scenes = RequiredScenes().ToArray();
            
            buildPlayerOptions.locationPathName = "D:/Build/Wiba4-windows-server/WiBa4-server.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.subtarget = (int) StandaloneBuildSubtarget.Server;
            buildPlayerOptions.options = BuildOptions.Development;

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            PostBuild(buildReport);
            //ResetBuildTarget();

        }

        private static void CreateLinuxServerBuild()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            //buildPlayerOptions.scenes = new[]
            //    {"Assets/Scenes/Bootstrap.unity", "Assets/Scenes/ServerSelect.unity", "Assets/Scenes/Island.unity"};
            
            buildPlayerOptions.scenes = RequiredScenes().ToArray();
            
            
            buildPlayerOptions.locationPathName = "D:/Build/Wiba4-linux-server/WiBa4.x86_64";
            buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
            buildPlayerOptions.subtarget = (int) StandaloneBuildSubtarget.Server;
            buildPlayerOptions.options = BuildOptions.Development;

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            PostBuild(buildReport);
            //ResetBuildTarget();
        }

        private static void PostBuild(BuildReport report)
        {
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Build failed");
            }
        }

    }


}