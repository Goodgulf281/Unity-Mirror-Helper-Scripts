using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    
    /*
     * First define the structs for the Move and Replicate data. The MoveData holds the inputs to be processed by the  
     * MoveWithData methods. Reconcile data contains the "output" of the move which is used to reset the client position.
     *
     * See: https://fish-networking.gitbook.io/docs/manual/guides/client-side-prediction/using-client-side-prediction
     * 
     */
    
    public struct MoveData : IReplicateData
    {
        public Vector2 Move;
        public bool Jump;
        public float CameraEulerY;
        public bool Sprint;
        public float PlayerEulerY;

        /* Everything below this is required for
        * the interface. You do not need to implement
        * Dispose, it is there if you want to clean up anything
        * that may allocate when this structure is discarded. */
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }


    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float VerticalVelocity;
        public float FallTimeout;
        public float JumpTimeout;
        public bool Grounded;
       
        /* Everything below this is required for
        * the interface. You do not need to implement
        * Dispose, it is there if you want to clean up anything
        * that may allocate when this structure is discarded. */
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;

    }
    
    
    public class ThirdPersonControllerCSP : NetworkBehaviour
    {
        /*
         * This is an updated ThirdPersonController from the Unity Starter Assets - Third Person Controller.
         * It now supports the Fishnet Networking Client Side Prediction. It's not perfect yet however, it should get
         * you started on making this (or your own) character controller working with Fishnet Client Side Prediction.
         *
         * Unfortunately the original code doesn't support an easy way to override key functions. Besides that
         * we'll need to inherit from Fishnet's NetworkBehaviour so I just copied some of the methods into
         * a new class.
         *
         * This code is inspired by an earlier attempt to make the Third Person Controller work with Fishnet:  
         * https://github.com/RidefortGames/FishNet-ThirdPersonPrediction/blob/main/Assets/Script/PredictionMotor.cs
         *
         * You can easily update this class when a newer version of the Starter Assets get published:
         * (1) Copy the ThirdPersonController properties into the OriginalProperties region.
         * (2) Copy the ThirdPersonController methods (with some exceptions listed below) into the OriginalMethods region.
         */


        /*
         * (1) Start with copy paste of ThirdPersonController properties
         */

        #region OriginalProperties

        [Header("Player")] [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")] [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)] [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;


        #endregion

        /*
         * (2) Copy all methods except Awake, Start, Update, LateUpdate, OnFootStep, OnLand
         */
        
        #region OriginalMethods

        
        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }
        
        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }



        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }


        

        #endregion

        
        /*
         * This is where the new code starts for the ThirdPersonController in order to implement Client Side Prediction.
         * 
         */
        
        
        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
           
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            
        }
        
        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                //float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                // Instead of multiplying by Time.deltaTime we need to use TimeManager.TickDelta
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : (float)base.TimeManager.TickDelta;
                
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }
        
        
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            // Originally this code was in the Start() method but it wasn't called. It gets called just in time
            // when put in the OnStartNetwork event. See: https://fish-networking.gitbook.io/docs/manual/guides/network-behaviour-guides
            
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            
            // Now we need to add listeners to some NetworkManager events:
            if (base.IsServer || base.IsClient)
            {
                base.TimeManager.OnTick += TimeManager_OnTick;
                base.TimeManager.OnUpdate += TimeManager_OnUpdate;
                base.TimeManager.OnLateUpdate += TimeManager_OnLateUpdate;
            }
        }

        public override void OnStopNetwork()
        {
            // Remove the listeners on Exit:
            base.OnStopNetwork();
            if (base.TimeManager != null)
            {
                base.TimeManager.OnTick -= TimeManager_OnTick;
                base.TimeManager.OnUpdate -= TimeManager_OnUpdate;
                base.TimeManager.OnLateUpdate -= TimeManager_OnLateUpdate;
            }
        }

        private void TimeManager_OnLateUpdate()
        {
            // In the original Starter Asset code the Camera Rotation takes place in the LateUpdate call.
            // In this network behaviour object we can use the LateUpdate event from the Time Manager instead. 
            
            if (base.IsOwner)
                CameraRotation();
        }
        
        private void TimeManager_OnTick()
        {
            // The OnTick event is the beating heart of the Client Side Prediction. This is where the movement and
            // reconciliation methods are called. The Tick events are synchronized across server and clients.
            
            // If this event is called on an object which has a disabled controller (it's a network instantiated
            // object not owned by the local player) then exit this method immediately:
            
            if (!_controller.enabled)
                return;
            
            
            if (base.IsOwner)
            {
                // These are the steps for the local client:
                
                Reconcile(default, false);
                BuildActions(out MoveData md);
                Move(md, false);
            }
            if (base.IsServer)
            {
                // These are the steps for the server:
                
                Move(default, true);
                ReconcileData rd = new ReconcileData
                {
                    Position = transform.position,
                    Rotation = transform.rotation,
                    VerticalVelocity = _verticalVelocity,
                    FallTimeout = _fallTimeoutDelta,
                    JumpTimeout = _jumpTimeoutDelta,
                    Grounded = Grounded
                };
                Reconcile(rd, true);
            }
        }

        private void TimeManager_OnUpdate()
        {
            // In the original code this takes place in the Update method.
            // In this network behaviour object we can use the Update event from the Time Manager instead. 
            
            _hasAnimator = TryGetComponent(out _animator);
        }
        
        
        private void BuildActions(out MoveData md)
        {
            // This is where we collect the input (from the Input System) and store it in the MoveData which
            // will be passed on to the client to actually move the character controller and it will be replicated
            // to the server.
            
            md = new MoveData()
            {
                Move = _input.move,                                 // Move directions from WASD keys
                Jump = _input.jump,                                 // Jump command from Space key 
                CameraEulerY = _mainCamera.transform.eulerAngles.y, // This is the rotation (y-axis only) where the camera points to;
                                                                    // it's used to turn the character to the left or right.
                Sprint = _input.sprint,                             // The sprint input (Shift)
                PlayerEulerY = transform.eulerAngles.y              // the Player rotation.
            };

            // Debug.Log("CameraEulerY="+md.CameraEulerY);
            
            _input.jump = false;
        }
        
        [Replicate]
        private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {
            // This is the method where the actual movement takes place.
            // Two of these functions have been modified so we can pass the MoveData, delta time and the asServer arguments:
            
            JumpAndGravity(md, (float)base.TimeManager.TickDelta);
            GroundedCheck();
            MoveWithData(md, (float)base.TimeManager.TickDelta, asServer);
        } 
        
        

        [Reconcile]
        private void Reconcile(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            // Reset the character position and other key parameters for the character controller's movement: 
            transform.position = rd.Position;

            // According to a post on the Fishnet Discord channel it doesn't make sense to reconcile player rotation
            // on the client so we skip it unless the code is run asServer.
            //transform.rotation = rd.Rotation;

            _verticalVelocity = rd.VerticalVelocity;
            _fallTimeoutDelta = rd.FallTimeout;
            _jumpTimeoutDelta = rd.JumpTimeout;
            Grounded = rd.Grounded;
            
            if(asServer)
                transform.rotation = rd.Rotation;
        }    
    
        private void MoveWithData(MoveData md, float delta,bool asServer)
        {
            // This code has been modified to use the MoveData inputs instead of live checks of the input system.
            // Also, it uses the delta argument (= TimeManager.TickDelta) instead of Time.DeltaTime
            
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = md.Sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (md.Move == Vector2.zero) targetSpeed = 0.0f;
            
            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? md.Move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    delta * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, delta * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(md.Move.x, 0.0f, md.Move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (md.Move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + md.CameraEulerY;

                // When we use the SmoothDampAngle from the original code the player seems to "fight" making a turn. I tried a few
                // changes but so far haven't found a working solution yet.
                //
                // This line seems to be the culprit: I suspect this is because it uses the transform instead of an angle stored in the MoveData 
                //
                //float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                //
                // This effort to fix it doesn't help:
                //float rotation = Mathf.SmoothDampAngle(md.PlayerEulerY, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // By taking out the dampening the left and right turn are currently not smoothed 
                float rotation = _targetRotation;
                
                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (targetSpeed * delta) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * delta);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, 1f);
            }
        }

        private void JumpAndGravity(MoveData md, float delta)
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (md.Jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= delta;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= delta;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
                
                // if we are not grounded, do not jump
                //_input.jump = false;
                md.Jump = false;

            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * delta;
            }
        }
            
        
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // Only play the sounds if we're on the owning client
            if (!base.IsOwner) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            // Only play the sounds if we're on the owning client
            if (!base.IsOwner) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        
        }
    

}