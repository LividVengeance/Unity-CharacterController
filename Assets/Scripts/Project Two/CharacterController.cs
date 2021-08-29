using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using KinematicCharacterController;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectTwo
{
    public enum CharacterState
    {
        Default,
        Sprinting,
        Charging,
        Swimming,
        Climbing,
        NoClip,
    }

    public enum ClimbingState
    {
        Anchoring,
        Climbing,
        DeAnchoring
    }

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

        [Header("Camera")] 
        [SerializeField, Tooltip("Will have the character face camera look direction")]
        private bool rotateToCameraFacing = true;

        [Header("Inputs")] 
        [SerializeField] private InputHandler inputHandler;
        
        [Header("Animations")] 
        [SerializeField, Tooltip("Animation controller the characters model is using")]
        private Animator animator;

        // Character state
        public CharacterState currentCharacterState { get; private set; }
        
        private Vector3 groundNormalRelativeVelocity = Vector3.zero;

        // Input vectors
        private Vector3 moveInputVector;
        private Vector3 lookInputVector;

        // Jump vars
        private bool jumpRequested = false;
        private bool jumpConsumed = false;
        private bool jumpedThisFrame = false;
        private bool jumpInputIsHeld = false;
        private float timeSinceJumpRequested = Mathf.Infinity;
        private float timeSinceLastAbleToJump = 0f;
        // Double jump vars
        private bool doubleJumpConsumed = false;
        // Wall jump vars
        private bool canWallJump = false;
        private Vector3 wallJumpNormal;
        // Velocity to be added on next update 
        private Vector3 internalVelocityAdd = Vector3.zero;
        // Crouching vars
        private bool shouldBeCrouching = false;
        private bool isCrouching = false;
        private bool crouchInputIsHeld = false;
        private Collider[] probedColliders = new Collider[8];
        // Charging vars
        private Vector3 currentChargeVelocity;
        private bool isStopped;
        private bool mustStopVelocity = false;
        private float timeSinceStartedCharge = 0;
        private float timeSinceStopped = 0;
        // Swimming vars
        private Collider waterZone;
        // Ladder vars
        private float ladderUpDownInput;
        private Ladder activeLadder { get; set; }
        private ClimbingState internalClimbingState;
        // Animation vars
        //private float 

        private ClimbingState climbingState
        {
            get => internalClimbingState;
            set
            {
                internalClimbingState = value;
                anchoringTimer = 0f;
                anchoringStartPosition = characterMotor.TransientPosition;
                anchoringStartRotation = characterMotor.TransientRotation;
            }
        }

        private Vector3 ladderTargetPosition;
        private Quaternion ladderTargetRotation;
        private float onLadderSegmentState = 0;
        private float anchoringTimer = 0f;
        private Vector3 anchoringStartPosition = Vector3.zero;
        private Quaternion anchoringStartRotation = Quaternion.identity;
        private Quaternion rotationBeforeClimbing = Quaternion.identity;


        /// Start is called before the first frame update
        void Start()
        {
            // Assigns as the motors controller
            characterMotor.CharacterController = this;

            // Sets the initial state
            TransitionToState(CharacterState.Default);

            if (characterSettings.playSpawnAnimation) StartCoroutine(SpawnAnimationStopInputs());
        }

        private IEnumerator SpawnAnimationStopInputs()
        {
            inputHandler.SetInputStatus(false);
            yield return (new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length));
            inputHandler.SetInputStatus(true);
        }

        /// Update is called once per frame
        void Update()
        {
            Debug.Log("Current Character State: " + currentCharacterState);
        }

        /// Handles the transitions between states
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = currentCharacterState;
            OnStateExit(tmpInitialState, newState);
            currentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// Handles the entering of a state
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                {
                    characterMotor.SetGroundSolvingActivation(true);
                    break;
                }
                case CharacterState.Sprinting:
                {
                    characterMotor.SetGroundSolvingActivation(true);
                    break;
                }
                case CharacterState.Charging:
                {
                    // Cache a charging velocity based on the character’s forward direction
                    currentChargeVelocity = characterMotor.CharacterForward * characterSettings.chargeSpeed;

                    // Setup values
                    isStopped = false;
                    timeSinceStartedCharge = 0f;
                    timeSinceStopped = 0f;
                    break;
                }
                case CharacterState.Swimming:
                {
                    characterMotor.SetGroundSolvingActivation(false);
                    break;
                }
                case CharacterState.Climbing:
                {
                    rotationBeforeClimbing = characterMotor.TransientRotation;

                    characterMotor.SetMovementCollisionsSolvingActivation(false);
                    characterMotor.SetGroundSolvingActivation(false);
                    climbingState = ClimbingState.Anchoring;

                    // Store the target position and rotation to snap to
                    ladderTargetPosition = activeLadder.ClosestPointOnLadderSegment(characterMotor.TransientPosition,
                        out onLadderSegmentState);
                    ladderTargetRotation = activeLadder.transform.rotation;
                    break;
                }
                case CharacterState.NoClip:
                {
                    // Bypass the custom collision detection
                    characterMotor.SetCapsuleCollisionsActivation(false);
                    characterMotor.SetMovementCollisionsSolvingActivation(false);
                    characterMotor.SetGroundSolvingActivation(false);
                    break;
                }
            }
        }

        /// Handles the exiting of a state
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                {
                    break;
                }
                case CharacterState.Sprinting:
                {
                    break;
                }
                case CharacterState.Charging:
                {
                    break;
                }
                case CharacterState.Climbing:
                {
                    characterMotor.SetMovementCollisionsSolvingActivation(true);
                    characterMotor.SetGroundSolvingActivation(true);
                    break;
                }
                case CharacterState.NoClip:
                {
                    // Use the custom collision detection
                    characterMotor.SetCapsuleCollisionsActivation(true);
                    characterMotor.SetMovementCollisionsSolvingActivation(true);
                    characterMotor.SetGroundSolvingActivation(true);
                    break;
                }
            }
        }

        /// Handles the transition to the sprint character state
        private void SprintTransitionHandler(ref PlayerCharacterInputs inputs)
        {
            // Handle sprint transitions
            if (inputs.SprintDown)
            {
                if (currentCharacterState == CharacterState.Default) TransitionToState(CharacterState.Sprinting);
            }
            else if (!inputs.SprintDown && currentCharacterState == CharacterState.Sprinting) TransitionToState(CharacterState.Default);
        }

        /// Handles the transition to the climbing character state
        private void ClimbingTransitionHandler(ref PlayerCharacterInputs inputs)
        {
            // Handle ladder transitions
            ladderUpDownInput = inputs.MoveAxisForward;
            if (inputs.InteractDown)
            {
                if (characterMotor.CharacterOverlap(characterMotor.TransientPosition, characterMotor.TransientRotation,
                    probedColliders, characterSettings.interactionLayer, QueryTriggerInteraction.Collide) > 0)
                {
                    if (probedColliders[0] != null)
                    {
                        // Handle ladders
                        Ladder ladder = probedColliders[0].gameObject.GetComponent<Ladder>();
                        if (ladder)
                        {
                            // Transition to ladder climbing state
                            if (currentCharacterState == CharacterState.Default)
                            {
                                activeLadder = ladder;
                                TransitionToState(CharacterState.Climbing);
                            }
                            // Transition back to default movement state
                            else if (currentCharacterState == CharacterState.Climbing)
                            {
                                climbingState = ClimbingState.DeAnchoring;
                                ladderTargetPosition = characterMotor.TransientPosition;
                                ladderTargetRotation = rotationBeforeClimbing;
                            }
                        }
                    }
                }
            }
        }

        /// Handles the transition to the noClip character state
        private void NoClipTransitionHandler(ref PlayerCharacterInputs inputs)
        {
            // Handle NoClip state transitions
            if (inputs.NoClipDown)
            {
                if (currentCharacterState == CharacterState.Default) TransitionToState(CharacterState.NoClip);
                else if (currentCharacterState == CharacterState.NoClip) TransitionToState(CharacterState.Default);
            }
        }

        private void CrouchHandler(ref PlayerCharacterInputs inputs)
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

        private void JumpRequestCheck(ref PlayerCharacterInputs inputs)
        {
            // Jumping input
            if (inputs.JumpDown)
            {
                timeSinceJumpRequested = 0f;
                jumpRequested = true;
            }
        }

        private void MoveAndLookInput(Quaternion cameraPlanarRotation, Vector3 moveInputVec, Vector3 cameraPlanarDirection)
        {
            // Move and look inputs
            moveInputVector = cameraPlanarRotation * moveInputVec;
            lookInputVector = cameraPlanarDirection;
        }
        
        /// This is called every frame by InputHandler to tell character what inputs its receiving
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // Handle sprint state transitions
            SprintTransitionHandler(ref inputs);

            // Handle climbing state transitions
            ClimbingTransitionHandler(ref inputs);

            // Handle NoClip state transitions
            NoClipTransitionHandler(ref inputs);

            // Held down keys
            jumpInputIsHeld = inputs.JumpHeld;
            crouchInputIsHeld = inputs.CrouchHeld;

            // Handle state transition for charging state
            if (inputs.ChargingDown) TransitionToState(CharacterState.Charging);

            // Clamp input
            Vector3 moveInputVec =
                Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3
                .ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3
                    .ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
            }

            Quaternion cameraPlanarRotation =
                Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);

            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Move and look inputs
                    MoveAndLookInput(cameraPlanarRotation, moveInputVec, cameraPlanarDirection);

                    // Requests a jump if the key is down
                    JumpRequestCheck(ref inputs);

                    // Crouches if the key is down
                    CrouchHandler(ref inputs);
                    break;
                }
                case CharacterState.Sprinting:
                {
                    // Move and look inputs
                    MoveAndLookInput(cameraPlanarRotation, moveInputVec, cameraPlanarDirection);
                    
                    // Requests a jump if the key is down
                    JumpRequestCheck(ref inputs);
                    break;
                }
                case CharacterState.Charging:
                {
                    break;
                }
                case CharacterState.Swimming:
                {
                    jumpRequested = inputs.JumpHeld;

                    // Move and look inputs
                    MoveAndLookInput(inputs.CameraRotation, moveInputVec, cameraPlanarDirection);
                    break;
                }
                case CharacterState.NoClip:
                {
                    // Move and look inputs
                    MoveAndLookInput(inputs.CameraRotation, moveInputVec, cameraPlanarDirection);
                    break;
                }
            }
        }

        private void CharacterRotation(ref Quaternion currentRotation, float deltaTime, float _orientationSharpness)
        {
            if (rotateToCameraFacing && lookInputVector != Vector3.zero && _orientationSharpness > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward,
                    lookInputVector,
                    1 - Mathf.Exp(-_orientationSharpness * deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection,
                    characterMotor.CharacterUp);

                if (characterSettings.orientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -characterSettings.gravity) *
                                      currentRotation;
                }
            }
        }

        /// This is where you tell your character what its rotation should be right now. 
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    CharacterRotation(ref currentRotation, deltaTime, characterSettings.orientationSharpness);
                    break;
                }
                case CharacterState.Sprinting:
                {
                    CharacterRotation(ref currentRotation, deltaTime, characterSettings.sprintOrientationSharpness);
                    break;
                }
                case CharacterState.Charging:
                {
                    break;
                }
                case CharacterState.Swimming:
                {
                    CharacterRotation(ref currentRotation, deltaTime, characterSettings.orientationSharpness);
                    break;
                }
                case CharacterState.Climbing:
                {
                    switch (climbingState)
                    {
                        case ClimbingState.Climbing:
                            currentRotation = activeLadder.transform.rotation;
                            break;
                        case ClimbingState.Anchoring:
                        case ClimbingState.DeAnchoring:
                            currentRotation = Quaternion.Slerp(anchoringStartRotation,
                                ladderTargetRotation, (anchoringTimer / characterSettings.anchoringDuration));
                            break;
                    }

                    break;
                }
                case CharacterState.NoClip:
                {
                    CharacterRotation(ref currentRotation, deltaTime, characterSettings.orientationSharpness);
                    break;
                }
            }
        }

        private void JumpHandler(ref Vector3 currentVelocity, float deltaTime)
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

        private void ExternalForceHandler(ref Vector3 currentVelocity)
        {
            // Take into account additive velocity
            if (internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += internalVelocityAdd;
                internalVelocityAdd = Vector3.zero;
            }
        }

        private void GroundMovementHandler(ref Vector3 currentVelocity, float deltaTime, float movementSharpness, float maxMoveSpeed)
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

        private void SwimmingMovementHandler(ref Vector3 currentVelocity, float deltaTime)
        {
            float verticalInput = 0f + (jumpInputIsHeld ? 1f : 0f) + (crouchInputIsHeld ? -1f : 0f);

            // Smoothly interpolate to target swimming velocity
            Vector3 targetMovementVelocity =
                (moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * characterSettings.swimmingSpeed;
            Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                1 - Mathf.Exp(-characterSettings.swimmingMovementSharpness * deltaTime));

            /// See if our swimming reference point would be out of water after the movement from our velocity has been applied

            Vector3 resultingSwimmingReferancePosition =
                characterMotor.TransientPosition + (smoothedVelocity * deltaTime) +
                (characterSettings.swimmingReferencePoint.position - characterMotor.TransientPosition);
            Vector3 closestPointWaterSurface = Physics.ClosestPoint(resultingSwimmingReferancePosition,
                waterZone, waterZone.transform.position, waterZone.transform.rotation);

            // if our position would be outside the water surface on next update, project the velocity
            // on the surface normal so that it would not take us out of the water
            if (closestPointWaterSurface != resultingSwimmingReferancePosition)
            {
                Vector3 waterSurfaceNormal =
                    (resultingSwimmingReferancePosition - closestPointWaterSurface).normalized;
                smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);

                // Jump out of water
                if (jumpRequested)
                {
                    smoothedVelocity += (characterMotor.CharacterUp * characterSettings.jumpSpeed) - Vector3.Project(
                        currentVelocity, characterMotor.CharacterUp);
                }
            }

            currentVelocity = smoothedVelocity;
        }
        
        /// This is where you tell your character what its velocity should be right now. 
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Handles ground movement if has move input
                    GroundMovementHandler(ref currentVelocity, deltaTime, characterSettings.stableMovementSharpness, characterSettings.maxStableMoveSpeed);

                    // Handles jump if jump requested
                    JumpHandler(ref currentVelocity, deltaTime);

                    // Handles external forces to the character controller
                    ExternalForceHandler(ref currentVelocity);
                    break;
                }
                case CharacterState.Sprinting:
                {
                    // Handles ground movement if has move input
                    GroundMovementHandler(ref currentVelocity, deltaTime, characterSettings.sprintMovementSharpness, characterSettings.maxSprintSpeed);

                    // Handles jump request if has jump input
                    if (characterSettings.allowJumpingWhileSprinting) JumpHandler(ref currentVelocity, deltaTime);
                    
                    // Handles external forces to the character controller
                    ExternalForceHandler(ref currentVelocity);
                    break;
                }
                case CharacterState.Charging:
                {
                    // If we have stopped and need to cancel velocity, do it here
                    if (mustStopVelocity)
                    {
                        currentVelocity = Vector3.zero;
                        mustStopVelocity = false;
                    }

                    // When stopped, do no velocity handling except gravity
                    if (isStopped) currentVelocity += characterSettings.gravity * deltaTime;
                    else
                    {
                        // When charging, velocity is always constant
                        float previousY = currentVelocity.y;
                        currentVelocity = currentChargeVelocity;
                        currentVelocity.y = previousY;
                        currentVelocity += characterSettings.gravity * deltaTime;
                    }

                    break;
                }
                case CharacterState.Swimming:
                {
                    // Handles the movement for the swimming
                    SwimmingMovementHandler(ref currentVelocity, deltaTime);
                    break;
                }
                case CharacterState.Climbing:
                {
                    currentVelocity = Vector3.zero;

                    switch (climbingState)
                    {
                        case ClimbingState.Climbing:
                            currentVelocity = (ladderUpDownInput * activeLadder.transform.up).normalized *
                                              characterSettings.climbingSpeed;
                            break;
                        case ClimbingState.Anchoring:
                        case ClimbingState.DeAnchoring:
                            Vector3 tmpPosition = Vector3.Lerp(anchoringStartPosition, ladderTargetPosition,
                                (anchoringTimer / characterSettings.anchoringDuration));
                            currentVelocity = characterMotor.GetVelocityForMovePosition(
                                characterMotor.TransientPosition, tmpPosition, deltaTime);
                            break;
                    }

                    break;
                }
                case CharacterState.NoClip:
                {
                    float verticalInput = 0f + (jumpInputIsHeld ? 1f : 0f) + (crouchInputIsHeld ? -1f : 0f);

                    // Smoothly interpolate to target velocity
                    Vector3 targetMovementVelocity =
                        (moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * characterSettings.noClipMoveSpeed;
                    currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                        1 - Mathf.Exp(-characterSettings.noClipSharpness * deltaTime));
                    break;
                }
            }
        }

        /// This is called before the character has its movement update
        public void BeforeCharacterUpdate(float deltaTime)
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
                        if (currentCharacterState == CharacterState.Default)
                        {
                            TransitionToState(CharacterState.Swimming);
                            waterZone = probedColliders[0];
                        }
                    }
                    // otherwise; default state
                    else
                    {
                        if (currentCharacterState == CharacterState.Swimming)
                        {
                            TransitionToState(CharacterState.Default);
                        }
                    }
                }
            }

            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    break;
                }
                case CharacterState.Sprinting:
                {
                    break;
                }
                case CharacterState.Charging:
                {
                    // Update times
                    timeSinceStartedCharge += deltaTime;
                    if (isStopped) timeSinceStopped += deltaTime;
                    break;
                }
                case CharacterState.Swimming:
                {
                    break;
                }
                case CharacterState.NoClip:
                {
                    break;
                }
            }
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
            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    Vector3 characterRelativeVelocity = transform.InverseTransformVector(characterMotor.Velocity);
                    // Animation
                    animator.SetFloat("velocityZ", (characterRelativeVelocity.z / characterSettings.maxStableMoveSpeed));
                    animator.SetFloat("velocityX", characterRelativeVelocity.x / characterSettings.maxStableMoveSpeed);
                    
                    // Handle jumping pre-ground grace period
                    if (jumpRequested && timeSinceJumpRequested > characterSettings.jumpPreGroundingGraceTime) jumpRequested = false;

                    // Handle jumping while sliding
                    if (characterSettings.allowJumpingWhenSliding
                        ? characterMotor.GroundingStatus.FoundAnyGround
                        : characterMotor.GroundingStatus.IsStableOnGround)
                    {
                        // If we're on a ground surface, reset jumping values
                        if (!jumpedThisFrame)
                        {
                            doubleJumpConsumed = false;
                            jumpConsumed = false;
                        }

                        timeSinceLastAbleToJump = 0f;
                    }
                    // Keep track of time since we were last able to jump (for grace period)
                    else timeSinceLastAbleToJump += deltaTime;

                    // Handle un-crouching
                    if (isCrouching && !shouldBeCrouching)
                    {
                        // Do an overlap test with the character's standing height to see if there are any obstructions
                        characterMotor.SetCapsuleDimensions(0.5f, 2f, 1f);
                        if (characterMotor.CharacterCollisionsOverlap(
                            characterMotor.TransientPosition,
                            characterMotor.TransientRotation,
                            probedColliders) > 0)
                        {
                            // If obstructions, just stick to crouching dimensions
                            characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                        }
                        else
                        {
                            // If no obstructions, un-crouch
                            characterSettings.meshRoot.localScale = new Vector3(1f, 1f, 1f);
                            isCrouching = false;
                        }
                    }

                    break;
                }
                case CharacterState.Sprinting:
                {
                    Vector3 characterRelativeVelocity = transform.InverseTransformVector(characterMotor.Velocity);
                    // Animation
                    animator.SetFloat("velocityZ", Mathf.Clamp(characterRelativeVelocity.z, -2, 2));
                    animator.SetFloat("velocityX", Mathf.Clamp(characterRelativeVelocity.x, -2, 2));
                    break;
                }
                case CharacterState.Charging:
                {
                    // Detect being stopped by elapsed time
                    if (!isStopped && timeSinceStartedCharge > characterSettings.maxChargeTime)
                    {
                        mustStopVelocity = true;
                        isStopped = true;
                    }

                    // Detect end of stopping phase and transition back to default movement state
                    if (timeSinceStopped > characterSettings.stoppedTime) TransitionToState(CharacterState.Default);

                    break;
                }
                case CharacterState.Climbing:
                {
                    switch (climbingState)
                    {
                        case ClimbingState.Climbing:
                            // Detect getting off ladder during climbing
                            activeLadder.ClosestPointOnLadderSegment(characterMotor.TransientPosition,
                                out onLadderSegmentState);
                            if (Mathf.Abs(onLadderSegmentState) > 0.05f)
                            {
                                climbingState = ClimbingState.DeAnchoring;

                                // If we're higher than the ladder top point
                                if (onLadderSegmentState > 0)
                                {
                                    ladderTargetPosition = activeLadder.GetTopReleasePoint.position;
                                    ladderTargetRotation = activeLadder.GetTopReleasePoint.rotation;
                                }
                                // If we're lower than the ladder bottom point
                                else if (onLadderSegmentState < 0)
                                {
                                    ladderTargetPosition = activeLadder.GetBottomReleasePoint.position;
                                    ladderTargetRotation = activeLadder.GetBottomReleasePoint.rotation;
                                }
                            }

                            break;
                        case ClimbingState.Anchoring:
                        case ClimbingState.DeAnchoring:
                            // Detect transitioning out from anchoring states
                            if (anchoringTimer >= characterSettings.anchoringDuration)
                            {
                                if (climbingState == ClimbingState.Anchoring)
                                {
                                    climbingState = ClimbingState.Climbing;
                                }
                                else if (climbingState == ClimbingState.DeAnchoring)
                                {
                                    TransitionToState(CharacterState.Default);
                                }
                            }

                            // Keep track of time since we started anchoring
                            anchoringTimer += deltaTime;
                            break;
                    }

                    break;
                }
            }
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
            switch (currentCharacterState)
            {
                case CharacterState.Default:
                {
                    // We can wall jump only if we are not stable on ground and are moving against an obstruction
                    if (characterSettings.allowWallJump && !characterMotor.GroundingStatus.IsStableOnGround &&
                        !hitStabilityReport.IsStable)
                    {
                        canWallJump = true;
                        wallJumpNormal = hitNormal;
                    }

                    break;
                }
                case CharacterState.Charging:
                {
                    // Detect being stopped by obstructions
                    if (!isStopped && !hitStabilityReport.IsStable &&
                        Vector3.Dot(-hitNormal, currentChargeVelocity.normalized) > 0.5f)
                    {
                        mustStopVelocity = true;
                        isStopped = true;
                    }

                    break;
                }
            }
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

        /// Adds a velocity to the character controller
        public void AddVelocity(Vector3 velocity)
        {
            switch (currentCharacterState)
            {
                case CharacterState.Default:
                    internalVelocityAdd += velocity;
                    break;
            }
        }
        
        public Vector3 GetCurrentVelocity() => groundNormalRelativeVelocity;
    }
}