using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using KinematicCharacterController;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectTwo
{
    public struct PlayerCharacterInputs
    {
        // All inputs the player can give to the character
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool JumpHeld;
        public bool CrouchDown;
        public bool CrouchUp;
        public bool CrouchHeld;
        public bool ChargingDown;
        public bool NoClipDown;
        public bool InteractDown;
        public bool SprintDown;
    }

    public class CharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor characterMotor;
        public CharacterSettings characterSettings;

        private CharacterStateMachine characterStateMachine;

        [Header("Camera")] 
        [Tooltip("Will have the character face camera look direction")]
        public bool rotateToCameraFacing = true;

        [Header("Inputs")] 
        [SerializeField] private InputHandler inputHandler;
        
        [Header("Animations")] 
        [Tooltip("Animation controller the characters model is using")]
        public Animator animator;

        private Vector3 groundNormalRelativeVelocity = Vector3.zero;

        // Input vectors
        public Vector3 moveInputVector;
        public Vector3 lookInputVector;

        // Jump vars
        public bool jumpRequested = false;
        public bool jumpConsumed = false;
        public bool jumpedThisFrame = false;
        public bool jumpInputIsHeld = false;
        public float timeSinceJumpRequested = Mathf.Infinity;
        public float timeSinceLastAbleToJump = 0f;
        // Double jump vars
        public bool doubleJumpConsumed = false;
        // Wall jump vars
        public bool canWallJump = false;
        public Vector3 wallJumpNormal;
        // Velocity to be added on next update 
        private Vector3 internalVelocityAdd = Vector3.zero;
        // Crouching vars
        public bool shouldBeCrouching = false;
        public bool isCrouching = false;
        public bool crouchInputIsHeld = false;
        public Collider[] probedColliders = new Collider[8];
        // Swimming vars
        public Collider waterZone;

        private bool isNoClipInput;
        private bool isSprintDown;
        private bool isChargeDown;
        private bool hasFinishedCharge;
        private bool hasFinishedClimbing;
        public void FinishChargeState(bool hasFinished) => hasFinishedCharge = hasFinished;
        public void FinishedClimbingState(bool hasFinished) => hasFinishedClimbing = hasFinished;

        private void Awake()
        {
            // Character State Machine Setup
            InitializeStateMachine();
        }

        /// Start is called before the first frame update
        void Start()
        {
            // Assigns as the motors controller
            characterMotor.CharacterController = this;
            
            if (characterSettings.playSpawnAnimation) StartCoroutine(SpawnAnimationStopInputs());
        }

        private void InitializeStateMachine()
        {
            characterStateMachine = new CharacterStateMachine();
            
            // State Setup
            SCR_Default_CS defaultCS = new SCR_Default_CS(characterMotor, this);
            SCR_Sprint_CS sprintCS = new SCR_Sprint_CS(characterMotor, this);
            SCR_Charge_CS chargeCS = new SCR_Charge_CS(characterMotor, this);
            SCR_Swimming_CS swimmingCS = new SCR_Swimming_CS(characterMotor, this);
            SCR_NoClip_CS noClipCS = new SCR_NoClip_CS(characterMotor, this);

            // Sprint State Transitions
            At(defaultCS, sprintCS, IsSprinting());
            At(sprintCS, defaultCS, NotSprinting());
            
            // Charge State Transitions
            At(defaultCS, chargeCS, ChargePressed());
            At(chargeCS, defaultCS, HasFinishedCharging());
            
            // No Clip State Transitions
            At(noClipCS, defaultCS, HasNoClipInput());
            At(defaultCS, noClipCS, HasNoClipInput());
            
            // Swimming State Transitions
            characterStateMachine.AddAnyTransition(swimmingCS, WaterOverlapCheck);
            At(swimmingCS, defaultCS, NoWaterOverlap());

            void At(IState to, IState from, Func<bool> condition) => characterStateMachine.AddTransition(to, from, condition);
            // State Transition Checks
            Func<bool> IsSprinting() => () => isSprintDown && characterSettings.AbilityEnabled("Sprint");
            Func<bool> NotSprinting() => () => !isSprintDown;
            Func<bool> ChargePressed() => () => isChargeDown && characterSettings.AbilityEnabled("Charge");
            Func<bool> HasFinishedCharging() => () => hasFinishedCharge;
            Func<bool> HasNoClipInput() => () => isNoClipInput && characterSettings.AbilityEnabled("NoClip");
            Func<bool> NoWaterOverlap() => () => !WaterOverlapCheck() && characterSettings.AbilityEnabled("Swimming");

            // Setting the starting state
            characterStateMachine.SetState(defaultCS);
        }
        
        private IEnumerator SpawnAnimationStopInputs()
        {
            inputHandler.SetInputStatus(false);
            yield return (new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length));
            inputHandler.SetInputStatus(true);
        }

        public void CrouchHandler(ref PlayerCharacterInputs inputs)
        {
            // Crouching input
            if (inputs.CrouchDown)
            {
                shouldBeCrouching = true;

                if (!isCrouching)
                {
                    isCrouching = true;
                    characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                    characterSettings.meshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                }
            }
            else if (inputs.CrouchUp) shouldBeCrouching = false;
        }

        public void JumpRequestCheck(ref PlayerCharacterInputs inputs)
        {
            // Jumping input
            if (inputs.JumpDown && characterSettings.AbilityEnabled("Jump"))
            {
                timeSinceJumpRequested = 0f;
                jumpRequested = true;
            }
        }

        public void MoveAndLookInput(Quaternion cameraPlanarRotation, Vector3 moveInputVec, Vector3 cameraPlanarDirection)
        {
            // Move and look inputs
            moveInputVector = cameraPlanarRotation * moveInputVec;
            lookInputVector = cameraPlanarDirection;
        }
        
        /// This is called every frame by InputHandler to tell character what inputs its receiving
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // Input Vars (For state machine)
            isChargeDown = inputs.ChargingDown;
            isSprintDown = inputs.SprintDown;
            isNoClipInput = inputs.NoClipDown;
            
            Debug.Log(inputs.NoClipDown);
            
            characterStateMachine.Tick(ref inputs);

            // Held down keys
            jumpInputIsHeld = inputs.JumpHeld;
            crouchInputIsHeld = inputs.CrouchHeld;
        }

        /// This is where you tell your character what its rotation should be right now. 
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            characterStateMachine.GetCurrentState.StateRotationUpdate(ref currentRotation, deltaTime);
        }

        public void JumpHandler(ref Vector3 currentVelocity, float deltaTime)
        {
            // Handle jumping
            jumpedThisFrame = false;
            timeSinceJumpRequested += deltaTime;
            if (jumpRequested)
            {
                // Handle double jump
                if (characterSettings.allowDoubleJump)
                {
                    if (jumpConsumed && !doubleJumpConsumed && (characterSettings.allowJumpingWhenSliding
                        ? !characterMotor.GroundingStatus.FoundAnyGround
                        : !characterMotor.GroundingStatus.IsStableOnGround))
                    {
                        characterMotor.ForceUnground(0.1f);

                        // Add to the return velocity and reset jump state
                        currentVelocity += (characterMotor.CharacterUp * characterSettings.jumpSpeed) -
                                           Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                        jumpRequested = false;
                        doubleJumpConsumed = true;
                        jumpedThisFrame = true;
                        
                        Debug.Log("Jump");
                        animator.SetTrigger("jumpTrigger");
                    }
                }

                // See if we actually are allowed to jump
                if (canWallJump || !jumpConsumed && ((characterSettings.allowJumpingWhenSliding ? characterMotor.GroundingStatus.FoundAnyGround
                         : characterMotor.GroundingStatus.IsStableOnGround) || timeSinceLastAbleToJump <= characterSettings.jumpPostGroundingGraceTime))
                {
                    // Calculate jump direction before un-grounding
                    Vector3 jumpDirection = characterMotor.CharacterUp;

                    // Wall jumping direction
                    if (canWallJump) jumpDirection = wallJumpNormal;
                    // Normal/double jumping direction
                    else if (characterMotor.GroundingStatus.FoundAnyGround &&
                             !characterMotor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = characterMotor.GroundingStatus.GroundNormal;
                    }

                    // Makes the character skip ground probing/snapping on its next update. 
                    characterMotor.ForceUnground(0.1f);

                    // Add to the return velocity and reset jump state
                    currentVelocity += (jumpDirection * characterSettings.jumpSpeed) -
                                       Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                    jumpRequested = false;
                    jumpConsumed = true;
                    jumpedThisFrame = true;
                    
                    animator.SetTrigger("jumpTrigger");
                }

                // Reset wall jump
                canWallJump = false;
            }
        }

        public void ExternalForceHandler(ref Vector3 currentVelocity)
        {
            // Take into account additive velocity
            if (internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += internalVelocityAdd;
                internalVelocityAdd = Vector3.zero;
            }
        }

        public void GroundMovementHandler(ref Vector3 currentVelocity, float deltaTime, float movementSharpness, float maxMoveSpeed)
        {
            groundNormalRelativeVelocity = currentVelocity;
                    
            Vector3 targetMovementVelocity = Vector3.zero;

            // Is on stable ground
            if (characterMotor.GroundingStatus.IsStableOnGround)
            {
                animator.SetBool("isGrounded", true);
                
                // Reorient source velocity on current ground slope
                // (this is because we don't want our smoothing to cause any velocity losses in slope changes)
                currentVelocity = characterMotor.GetDirectionTangentToSurface(currentVelocity,
                    characterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // Calculate target velocity
                Vector3 inputRight = Vector3.Cross(moveInputVector, characterMotor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(characterMotor.GroundingStatus.GroundNormal,
                    inputRight).normalized * moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * maxMoveSpeed;

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                    1 - Mathf.Exp(-movementSharpness * deltaTime));
            }
            else
            {
                animator.SetBool("isGrounded", false);
                
                // Add move input
                if (moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = moveInputVector * characterSettings.maxAirMoveSpeed;

                    // Prevent climbing on un-stable slopes with air movement
                    if (characterMotor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(
                                characterMotor.CharacterUp, characterMotor.GroundingStatus.GroundNormal),
                            characterMotor.CharacterUp).normalized;
                        targetMovementVelocity = Vector3.ProjectOnPlane(
                            targetMovementVelocity, perpenticularObstructionNormal);
                    }

                    Vector3 velocityDiff =
                        Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, characterSettings.gravity);
                    currentVelocity += velocityDiff * characterSettings.airAccelerationSpeed * deltaTime;
                }

                // Gravity
                currentVelocity += characterSettings.gravity * deltaTime;

                // Drag
                currentVelocity *= (1f / (1f + (characterSettings.drag * deltaTime)));
            }
        }

        /// This is where you tell your character what its velocity should be right now. 
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            characterStateMachine.GetCurrentState.StateVelocityUpdate(ref currentVelocity, deltaTime);
        }

        /// This is called before the character has its movement update
        public void BeforeCharacterUpdate(float deltaTime)
        {
            characterStateMachine.GetCurrentState.StateBeforeCharacterUpdate(deltaTime);
            
            //WaterOverlap();
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (characterMotor.GroundingStatus.IsStableOnGround && !characterMotor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!characterMotor.GroundingStatus.IsStableOnGround &&
                     characterMotor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        /// This is called after the character has finished its movement update
        public void AfterCharacterUpdate(float deltaTime)
        {
            characterStateMachine.GetCurrentState.StateAfterCharacterUpdate(deltaTime);
        }

        /// Return true if character controller can collide with this collider else is false
        public bool IsColliderValidForCollisions(Collider coll)
        {
            string collLayer = LayerMask.LayerToName(coll.gameObject.layer);
            // Checks if layer is in ignore list
            if (characterSettings.ignoredLayers.Contains(collLayer)) return false;

            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            characterStateMachine.GetCurrentState.StateOnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        private void OnLanded()
        {
            Debug.Log("Landed");
            animator.SetTrigger("hasLanded");
        }

        private void OnLeaveStableGround()
        {
            Debug.Log("Left ground");
        }

        private bool WaterOverlapCheck()
        {
            // Do a character overlap test to detect water surfaces
            if (characterMotor.CharacterOverlap(characterMotor.TransientPosition, characterMotor.TransientRotation,
                probedColliders, characterSettings.waterLayer, QueryTriggerInteraction.Collide) > 0)
            {
                // If a water surface was detected
                if (probedColliders[0] != null)
                {
                    // If the swimming reference point is inside the box, make sure we are in swimming state
                    if (Physics.ClosestPoint(characterSettings.swimmingReferencePoint.position, probedColliders[0],
                            probedColliders[0].transform.position, probedColliders[0].transform.rotation) ==
                        characterSettings.swimmingReferencePoint.position)
                    {
                        waterZone = probedColliders[0];
                        return true;
                    }
                }
            }

            return false;
        }
        
    }
}