using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using Invector;
using Invector.vCamera;
using Invector.vCharacterController;
using Invector.vCharacterController.vActions;
using Crest;
using CompassNavigatorPro;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using Goodgulf.Gamelogic;
using Goodgulf.Networking;
using HeathenEngineering.SteamworksIntegration;
using UserAPI = HeathenEngineering.SteamworksIntegration.API.User.Client;

public class PlayerScript : NetworkBehaviour
{
    private vThirdPersonController _vThirdPersonController;
    private vFootStep _vFootStep;
    private vHitDamageParticle _vHitDamageParticle;
    private vHeadTrack _vHeadTrack;
    private vLadderAction _vLadderAction;
    private vGenericAction _vGenericAction;
    private vThirdPersonInput _vThirdPersonInput;
    private vThirdPersonCamera _vThirdPersonCamera;
    private OceanRenderer oceanRenderer;
    private Camera playerCamera;
    private CompassPro compassPro;

    private NetworkManager networkManager;
    private void Awake()
    {
        Debug.Log("PlayerScript.Awake(): event fired");

        networkManager = InstanceFinder.NetworkManager;
        
        // Start with disabling all the Invector components on the instantiated object 
        
        _vThirdPersonController = GetComponent<vThirdPersonController>();
        _vFootStep = GetComponent<vFootStep>();
        _vHitDamageParticle = GetComponent<vHitDamageParticle>();
        _vHeadTrack = GetComponent<vHeadTrack>();
        _vLadderAction = GetComponent<vLadderAction>();
        _vGenericAction = GetComponent<vGenericAction>();
        _vThirdPersonCamera = GetComponentInChildren<vThirdPersonCamera>();
        _vThirdPersonInput = GetComponent<vThirdPersonInput>();
        
        _vThirdPersonController.enabled = false;
        _vFootStep.enabled = false;
        _vHitDamageParticle.enabled = false;
        _vHeadTrack.enabled = false;
        _vLadderAction.enabled = false;
        _vGenericAction.enabled = false;
        _vThirdPersonInput.enabled = false;
        
        _vThirdPersonCamera.enabled = false;
        _vThirdPersonCamera.gameObject.SetActive(false);
        
        oceanRenderer = OceanRenderer.Instance;
    }

    
    // If you are coming from Mirror instead of using OnLocalPlayer use OnStartClient with a base.IsOwner check.
    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("PlayerScript.OnStartClient(): event fired");

        if (base.IsOwner)
        {
            // So this is the locally owned player object. Enable all Invector controls for this one:
            
            _vThirdPersonController.enabled = true;
            _vFootStep.enabled = true;
            _vHitDamageParticle.enabled = true;
            _vHeadTrack.enabled = true;
            _vLadderAction.enabled = true;
            _vGenericAction.enabled = true;
            _vThirdPersonInput.enabled = true;
            
            _vThirdPersonCamera.gameObject.SetActive(true);
            _vThirdPersonCamera.enabled = true;
            _vThirdPersonCamera.SetMainTarget(this.transform);
            
            //Enviro.EnviroManager.instance.AssignAndStart(this.gameObject, playerCamera);
            playerCamera = _vThirdPersonCamera.transform.GetChild(0).GetComponent<Camera>();

            if (!playerCamera)
            {
                Debug.LogError("PlayerScript.OnStartClient(): cannot find player Camera.");
            }
            
            if (oceanRenderer)
            {
                oceanRenderer.ViewCamera = playerCamera;
            }
            else Debug.LogWarning("PlayerScript.OnStartClient(): cannot find OceanRenderer.");
            
            compassPro = CompassPro.instance;
            compassPro.cameraMain = playerCamera;
            compassPro.miniMapFollow = this.transform;
         
            
            // Now send player information to server: Steam user id and network connection (= base.Owner)
            var user = UserAPI.Id;
            SendPlayerInfo(base.Owner, user);
        }
    }

    // This is the server RPC which adds the steam userid and network connection to the GamePlayers class.
    [ServerRpc]
    private void SendPlayerInfo(NetworkConnection conn, UserData steamUser)
    {
        GamePlayers.Instance.AddGamePlayer(conn, steamUser);
    }
    
    void Update()
    {
        if (base.IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // Call an RPC when the player presses the 1-key
                RpcCastSpell(base.Owner, 0);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RpcCastSpell(base.Owner, 1);
            }

            if (Input.GetKeyDown(KeyCode.Comma))
            {
                string debug = GamePlayers.Instance.GetPlayersDebugLog();
                Debug.Log(debug);
            }
        }
        
    }

    [ServerRpc]
    public void RpcCastSpell(NetworkConnection conn, int spellIndex)
    {
        // The Spells class contains a list of spells and their attributes
        Spells spells = GetComponent<Spells>();
        if (spells != null)
        {
            Spell spell = spells.GetSpell(spellIndex);

            if (spell != null)
            {
                // some spells need a target to be defined, this target is a prefab also instantiated by the server
                // in front of the player
                NetworkObject ntarget=null;
                
                // STEP 1: instantiate the spell target object if we need it 
                if (spell.usesTarget)
                {
                    Vector3 targetPosition = transform.position + transform.forward * spell.targetOffset.z +
                                             transform.up * spell.targetOffset.y;

                    ntarget = networkManager.GetPooledInstantiated(spell.targetPrefab, true);
                    ntarget.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);

                    networkManager.ServerManager.Spawn(ntarget, conn);
                }
               
                // STEP 2: instantiate the spell itself. A prefab with the visual effects is instantiated on the server:
                
                NetworkObject networkedSpell = networkManager.GetPooledInstantiated(spell.prefab, true);
                networkedSpell.transform.SetPositionAndRotation(this.transform.position + spell.sourceOffset, this.transform.rotation);
                networkManager.ServerManager.Spawn(networkedSpell, conn);

                
                // STEP 3: link the spell to the target (if spell is targeted)
                if (spell.usesTarget && ntarget!=null)
                {
                    RFX1_Target target = networkedSpell.GetComponent<RFX1_Target>();
                    if (target != null)
                    {
                        // link the spell's target on the server side
                        target.Target = ntarget.gameObject;

                    }
                    else Debug.LogWarning("PlayerScript.RpcCastSpell(): spell is targeted but no target script found.");

                    SpellObject spellObject = networkedSpell.GetComponent<SpellObject>();
                    if (spellObject == null)
                    {
                        Debug.LogError("PlayerScript.RpcCastSpell(): spell object not found.");
                    }
                    else
                    {
                        // link the instantiated networked spell to the networked target object so the client will link up too. 
                        spellObject.targetId = ntarget.ObjectId;

                    }
                }

                // Set duration on spell and target, destroy it after duration in seconds:
                StartCoroutine(DestroySpell(networkedSpell, ntarget, spell.duration));

            }
        }
    }

    IEnumerator DestroySpell(NetworkObject nspell, NetworkObject ntarget, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
     
        nspell.Despawn(DespawnType.Pool);
        ntarget.Despawn(DespawnType.Pool);
    }
    
}
