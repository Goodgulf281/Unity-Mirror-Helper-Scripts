using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using Invector.vCharacterController;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{
    
    public vThirdPersonCamera cameraPrefab;         // Assign a reference to the Invector camera prefab to this property.

    private vThirdPersonInput _vThirdPersonInput;   // Reference to the Invector Input system which we disable on all players except the Local Player.
    private vThirdPersonCamera _vThirdPersonCamera; // Reference to the Invector camera we spawn based on the above prefab.
    
    
    private void Awake()
    {
        Debug.Log("PlayerScript.Awake(): event fired");
        
        _vThirdPersonInput = GetComponent<vThirdPersonInput>();
        if (_vThirdPersonInput)
        {
            // Disable input by default, enable later for local players in OnStartLocalPlayer() event.
            // If we don't do this the input handler script will mess up the 3rd person camera setup.
          
            // _vThirdPersonInput.enabled = false;
            
            // For the Fishnetworking version we actually have to disable the script on the prefab. It will be reenabled in the OnStartClient event.
        }
        else Debug.LogError("PlayerScript.Awake: could not find vThirdPersonInput.");
    }

    // If you are coming from Mirror instead of using OnLocalPlayer use OnStartClient with a base.IsOwner check.
    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("PlayerScript.OnStartClient(): event fired");
        
        if (base.IsOwner)
        {
            
            // Deactivate the main camera (which we see at the start of the scene) since the 3rd person camera needs to take over now.
            
            Camera.main.gameObject.SetActive(false);
            
            Vector3 cameraPosition = new Vector3(this.transform.position.x, this.transform.position.y+this.transform.up.y*3, this.transform.position.z);
            
            _vThirdPersonCamera = Instantiate<vThirdPersonCamera>(cameraPrefab, cameraPosition, Quaternion.identity);

            if (_vThirdPersonCamera)
            {
                Debug.Log("PlayerScript.OnStartClient(): setting main camera to localPlayer");

                // Now link the camera to this player:
                _vThirdPersonCamera.SetMainTarget(this.transform);
            }
            else Debug.LogError("PlayerScript.OnStartClient(): _vThirdPersonCamera = null");

            if (_vThirdPersonInput)
            {
                // Since this is the local player we'll need to enable the input handler
                // In the original Mirror script I just enabled the input and it worked.
                // However the Invector free doesn't play nice with Fish and you'll see some odd behaviour (player falling from the sky).
                //_vThirdPersonInput.enabled = true;

                // So instead we'll wait a short time then enable the inputs after events have finished executing:
                Invoke(nameof(EnableInput), 0.5f);

            }
            else Debug.LogError("PlayerScript.OnStartClient(): _vThirdPersonInput = null");
            
        }
        
     
    }

    public void EnableInput()
    {
        if (_vThirdPersonInput)
        {
            // Since this is the local player we'll need to enable the input handler
            // Simply enabling will cause some errors since Start() has not been called
            // So we'll need to do some "initialization" ourselves here.
            
            _vThirdPersonInput.cc = GetComponent<vThirdPersonController>();
            _vThirdPersonInput.cc.Init();
            _vThirdPersonInput.enabled = true;
        }
        else Debug.LogError("PlayerScript.EnableInput(): _vThirdPersonInput = null");
    }
    
    
}
