using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using Invector;
using Invector.vCamera;
using Invector.vCharacterController;
using Invector.vCharacterController.vActions;
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
    
    
    private void Awake()
    {
        Debug.Log("PlayerScript.Awake(): event fired");

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

        }
    }
}
