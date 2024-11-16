using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;
using EmeraldAI;

namespace Goodgulf.Networking
{

    /*
     * This class is added to the networked prefab of your NPC. This acts in a similar manner as the playerscripts I created for earlier videos. 
     * It basically disables the AI (and this the movement) on the client using a kill switch. Only when the code is run on a server will
     * the AI be enabled.
     */
    
    public class AIScript : NetworkBehaviour
    {
        private EmeraldSystem _emeraldSystem;
        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;
            _emeraldSystem = GetComponent<EmeraldSystem>();

            EnableEmeraldAI(false);
        }

        public override void OnStartServer()
        {
            EnableEmeraldAI(true);
            
        }

        // This is the kill switch. Ideally this should be a method in the EmeraldSystem class
        // since this may change with every asset update.
        private void EnableEmeraldAI(bool state = false)
        {
            if (_emeraldSystem)
            {
                if(_emeraldSystem.MovementComponent)
                    _emeraldSystem.MovementComponent.enabled = state;
                if(_emeraldSystem.AnimationComponent)
                    _emeraldSystem.AnimationComponent.enabled = state;
                if(_emeraldSystem.SoundComponent)
                    _emeraldSystem.SoundComponent.enabled = state;
                if(_emeraldSystem.DetectionComponent)
                    _emeraldSystem.DetectionComponent.enabled = state;
                if(_emeraldSystem.BehaviorsComponent)
                    _emeraldSystem.BehaviorsComponent.enabled = state;
                if(_emeraldSystem.CombatComponent)
                    _emeraldSystem.CombatComponent.enabled = state;
                if(_emeraldSystem.HealthComponent)
                    _emeraldSystem.HealthComponent.enabled = state;
                if(_emeraldSystem.OptimizationComponent)
                    _emeraldSystem.OptimizationComponent.enabled = state;
                if(_emeraldSystem.EventsComponent)
                    _emeraldSystem.EventsComponent.enabled = state;
                if(_emeraldSystem.DebuggerComponent)
                    _emeraldSystem.DebuggerComponent.enabled = state;
                if(_emeraldSystem.UIComponent)
                    _emeraldSystem.UIComponent.enabled = state;
                if(_emeraldSystem.ItemsComponent)
                    _emeraldSystem.ItemsComponent.enabled = state;
                if(_emeraldSystem.SoundDetectorComponent)
                    _emeraldSystem.SoundDetectorComponent.enabled = state;
                if(_emeraldSystem.InverseKinematicsComponent)
                    _emeraldSystem.InverseKinematicsComponent.enabled = state;
                if(_emeraldSystem.TPMComponent)
                    _emeraldSystem.TPMComponent.enabled = state;

                _emeraldSystem.enabled = state;

            }
            else Debug.LogError("AIScript.EnableEmeraldAI(): _emeraldSystem=null");
        }
        
        
        
    }

}