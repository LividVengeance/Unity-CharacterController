using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using KinematicCharacterController;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum CharacterState
{
    Default,
    Charging,
    Swimming,
    NoClip,
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
}

public class CharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor characterMotor;
    
    [Header("Stable Movement")]
    [SerializeField] private float maxStableMoveSpeed = 10f;
    [SerializeField] private float stableMovementSharpness = 15;
    [SerializeField] private float orientationSharpness = 10;

    [Header("Air Movement")]
    [SerializeField] private float maxAirMoveSpeed = 10f;
    [SerializeField] private float airAccelerationSpeed = 5f;
    [SerializeField] private float drag = 0.1f;
    
    [Header("Jumping")]
    [SerializeField] private bool allowJumpingWhenSliding = false;
    [SerializeField] private bool allowDoubleJump = false;
    [SerializeField] private bool allowWallJump = false;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField, Tooltip("Time before landing where jump input will still allow jump once you land")] 
    private float jumpPreGroundingGraceTime = 0f;
    [SerializeField, Tooltip("Time after leaving stable ground where jump will still be allowed")]
    private float jumpPostGroundingGraceTime = 0f;
    
    [Header("Charging")]
    [SerializeField, Tooltip("How fast the character will move in the charge state")]
    private float chargeSpeed = 15f;
    [SerializeField, Tooltip("Maximum time character can be in the charge state")]
    private float maxChargeTime = 1.5f;
    [SerializeField, Tooltip("Time the character will remain unable to move at end of charge state")] 
    private float stoppedTime = 1f;
    
    [Header("Swimming")]
    [SerializeField] private Transform swimmingReferencePoint;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float swimmingSpeed = 4f;
    [SerializeField] private float swimmingMovementSharpness = 3;
    [SerializeField] private float swimmingOrientationSharpness = 2f;
    
    [Header("NoClip")]
    [SerializeField, Tooltip("How fast the character will move in the no clip state")] 
    private float noClipMoveSpeed = 10f;
    [SerializeField] private float noClipSharpness = 15;

    [Header("Misc")] 
    [SerializeField, Tooltip("Always orient its up direction in the opposite direction of the gravity")] 
    private bool orientTowardsGravity;
    [SerializeField] private bool rotationObstruction;
    [SerializeField] private Vector3 gravity = new Vector3(0, -30f, 0);
    [SerializeField] private Transform meshRoot;
    [SerializeField, Tooltip("Ignore physics collision on these layers")] 
    List<string> ignoredLayers = new List<string>();
    
    [Header("Camera")]
    [SerializeField, Tooltip("Will have the character face camera look direction")] 
    private bool rotateToCameraFacing = true;
    
    // Character state
    public CharacterState currentCharacterState { get; private set; }
    
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
    // Swimming
    private Collider waterZone;
    

    /// Start is called before the first frame update
    void Start()
    {
        // Assigns as the motors controller
        characterMotor.CharacterController = this;
        
        // Sets the initial state
        TransitionToState(CharacterState.Default);
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
            case CharacterState.Charging:
            {
                // Cache a charging velocity based on the character’s forward direction
                currentChargeVelocity = characterMotor.CharacterForward * chargeSpeed;
                
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
            case CharacterState.Charging:
            {
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
    
    /// This is called every frame by InputHandler to tell character what inputs its receiving
    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        // Handle NoClip state transitions
        if (inputs.NoClipDown)
        {
            if (currentCharacterState == CharacterState.Default) TransitionToState(CharacterState.NoClip);
            else if (currentCharacterState == CharacterState.NoClip) TransitionToState(CharacterState.Default);
        }
        
        // Held down keys
        jumpInputIsHeld = inputs.JumpHeld;
        crouchInputIsHeld = inputs.CrouchHeld;
        
        // Handle state transition for charging state
        if (inputs.ChargingDown) TransitionToState(CharacterState.Charging);

        // Clamp input
        Vector3 moveInputVec = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);

        switch (currentCharacterState)
        {
            case CharacterState.Default:
            {
                // Move and look inputs
                moveInputVector = cameraPlanarRotation * moveInputVec;
                lookInputVector = cameraPlanarDirection;

                // Jumping input
                if (inputs.JumpDown)
                {
                    timeSinceJumpRequested = 0f;
                    jumpRequested = true;
                }

                // Crouching input
                if (inputs.CrouchDown)
                {
                    shouldBeCrouching = true;

                    if (!isCrouching)
                    {
                        isCrouching = true;
                        characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                        meshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                    }
                }
                else if (inputs.CrouchUp) shouldBeCrouching = false;

                break;
            }
            case CharacterState.Charging:
            {
                break;
            }
            case CharacterState.Swimming:
            {
                jumpRequested = inputs.JumpHeld;

                moveInputVector = inputs.CameraRotation * moveInputVector;
                lookInputVector = cameraPlanarDirection;
                break;
            }
            case CharacterState.NoClip:
            {
                moveInputVector = inputs.CameraRotation * moveInputVec;
                lookInputVector = cameraPlanarDirection;
                break;
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
                if (rotateToCameraFacing && lookInputVector != Vector3.zero && orientationSharpness > 0f)
                {
                    // Smoothly interpolate from current to target look direction
                    Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward, lookInputVector,
                        1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

                    // Set the current rotation (which will be used by the KinematicCharacterMotor)
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection,
                        characterMotor.CharacterUp);

                    if (orientTowardsGravity)
                    {
                        // Rotate from current up to invert gravity
                        currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -gravity) *
                                          currentRotation;
                    }
                }

                break;
            }
            case CharacterState.Charging:
            {
                break;
            }
            case CharacterState.Swimming:
            {
                if (lookInputVector != Vector3.zero && orientationSharpness > 0f)
                {
                    // Smoothly interpolate from current to target look direction
                    Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward, lookInputVector, 
                        1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

                    // Set the current rotation (which will be used by the KinematicCharacterMotor)
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, characterMotor.CharacterUp);
                }
                if (orientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -gravity) * currentRotation;
                }
                break;
            }
            case CharacterState.NoClip:
            {
                if (lookInputVector != Vector3.zero && orientationSharpness > 0f)
                {
                    // Smoothly interpolate from current to target look direction
                    Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward, 
                        lookInputVector, 1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

                    // Set the current rotation (which will be used by the KinematicCharacterMotor)
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, characterMotor.CharacterUp);
                }
                if (orientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -gravity) * currentRotation;
                }
                break;
            }
        }
    }

    /// This is where you tell your character what its velocity should be right now. 
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (currentCharacterState)
        {
            case CharacterState.Default:
            {
                Vector3 targetMovementVelocity = Vector3.zero;

                // Is on stable ground
                if (characterMotor.GroundingStatus.IsStableOnGround)
                {
                    // Reorient source velocity on current ground slope
                    // (this is because we don't want our smoothing to cause any velocity losses in slope changes)
                    currentVelocity = characterMotor.GetDirectionTangentToSurface(currentVelocity,
                        characterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                    // Calculate target velocity
                    Vector3 inputRight = Vector3.Cross(moveInputVector, characterMotor.CharacterUp);
                    Vector3 reorientedInput = Vector3.Cross(characterMotor.GroundingStatus.GroundNormal,
                        inputRight).normalized * moveInputVector.magnitude;
                    targetMovementVelocity = reorientedInput * maxStableMoveSpeed;

                    // Smooth movement Velocity
                    currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                        1 - Mathf.Exp(-stableMovementSharpness * deltaTime));
                }
                else
                {
                    // Add move input
                    if (moveInputVector.sqrMagnitude > 0f)
                    {
                        targetMovementVelocity = moveInputVector * maxAirMoveSpeed;

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
                            Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, gravity);
                        currentVelocity += velocityDiff * airAccelerationSpeed * deltaTime;
                    }

                    // Gravity
                    currentVelocity += gravity * deltaTime;

                    // Drag
                    currentVelocity *= (1f / (1f + (drag * deltaTime)));
                }

                // Handle jumping
                jumpedThisFrame = false;
                timeSinceJumpRequested += deltaTime;
                if (jumpRequested)
                {
                    // Handle double jump
                    if (allowDoubleJump)
                    {
                        if (jumpConsumed && !doubleJumpConsumed && (allowJumpingWhenSliding
                            ? !characterMotor.GroundingStatus.FoundAnyGround
                            : !characterMotor.GroundingStatus.IsStableOnGround))
                        {
                            characterMotor.ForceUnground(0.1f);

                            // Add to the return velocity and reset jump state
                            currentVelocity += (characterMotor.CharacterUp * jumpSpeed) -
                                               Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                            jumpRequested = false;
                            doubleJumpConsumed = true;
                            jumpedThisFrame = true;
                        }
                    }

                    // See if we actually are allowed to jump
                    if (canWallJump || !jumpConsumed &&
                        ((allowJumpingWhenSliding
                             ? characterMotor.GroundingStatus.FoundAnyGround
                             : characterMotor.GroundingStatus.IsStableOnGround) ||
                         timeSinceLastAbleToJump <= jumpPostGroundingGraceTime))
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
                        currentVelocity += (jumpDirection * jumpSpeed) -
                                           Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                        jumpRequested = false;
                        jumpConsumed = true;
                        jumpedThisFrame = true;
                    }

                    // Reset wall jump
                    canWallJump = false;
                }

                // Take into account additive velocity
                if (internalVelocityAdd.sqrMagnitude > 0f)
                {
                    currentVelocity += internalVelocityAdd;
                    internalVelocityAdd = Vector3.zero;
                }

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
                if (isStopped) currentVelocity += gravity * deltaTime;
                else
                {
                    // When charging, velocity is always constant
                    float previousY = currentVelocity.y;
                    currentVelocity = currentChargeVelocity;
                    currentVelocity.y = previousY;
                    currentVelocity += gravity * deltaTime;
                }

                break;
            }
            case CharacterState.Swimming:
            {
                float verticalInput = 0f + (jumpInputIsHeld ? 1f : 0f) + (crouchInputIsHeld ? -1f : 0f);

                // Smoothly interpolate to target swimming velocity
                Vector3 targetMovementVelocity = (moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * swimmingSpeed;
                Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 
                    1 - Mathf.Exp(-swimmingMovementSharpness * deltaTime));

                /// See if our swimming reference point would be out of water after the movement from our velocity has been applied

                Vector3 resultingSwimmingReferancePosition = 
                    characterMotor.TransientPosition + (smoothedVelocity * deltaTime) + 
                    (swimmingReferencePoint.position - characterMotor.TransientPosition);
                Vector3 closestPointWaterSurface = Physics.ClosestPoint(resultingSwimmingReferancePosition, 
                    waterZone, waterZone.transform.position, waterZone.transform.rotation);

                // if our position would be outside the water surface on next update, project the velocity
                // on the surface normal so that it would not take us out of the water
                if (closestPointWaterSurface != resultingSwimmingReferancePosition)
                {
                    Vector3 waterSurfaceNormal = (resultingSwimmingReferancePosition - closestPointWaterSurface).normalized;
                    smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);

                    // Jump out of water
                    if (jumpRequested)
                    {
                        smoothedVelocity += (characterMotor.CharacterUp * jumpSpeed) - Vector3.Project(
                            currentVelocity, characterMotor.CharacterUp);
                    }
                }

                currentVelocity = smoothedVelocity;
                break;
            }
            case CharacterState.NoClip:
            {
                float verticalInput = 0f + (jumpInputIsHeld ? 1f : 0f) + (crouchInputIsHeld ? -1f : 0f);

                // Smoothly interpolate to target velocity
                Vector3 targetMovementVelocity = (moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * noClipMoveSpeed;
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-noClipSharpness * deltaTime));
                break;
            }
        }
    }

    /// This is called before the character has its movement update
    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Do a character overlap test to detect water surfaces
        if (characterMotor.CharacterOverlap(characterMotor.TransientPosition, characterMotor.TransientRotation, 
            probedColliders, waterLayer, QueryTriggerInteraction.Collide) > 0)
        {
            // If a water surface was detected
            if (probedColliders[0] != null)
            {
                // If the swimming reference point is inside the box, make sure we are in swimming state
                if (Physics.ClosestPoint(swimmingReferencePoint.position, probedColliders[0], 
                    probedColliders[0].transform.position, probedColliders[0].transform.rotation) == swimmingReferencePoint.position)
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
        else if (!characterMotor.GroundingStatus.IsStableOnGround && characterMotor.LastGroundingStatus.IsStableOnGround)
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
                // Handle jumping pre-ground grace period
                if (jumpRequested && timeSinceJumpRequested > jumpPreGroundingGraceTime) jumpRequested = false;

                // Handle jumping while sliding
                if (allowJumpingWhenSliding
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
                        meshRoot.localScale = new Vector3(1f, 1f, 1f);
                        isCrouching = false;
                    }
                }

                break;
            }
            case CharacterState.Charging:
            {
                // Detect being stopped by elapsed time
                if (!isStopped && timeSinceStartedCharge > maxChargeTime)
                {
                    mustStopVelocity = true;
                    isStopped = true;
                }

                // Detect end of stopping phase and transition back to default movement state
                if (timeSinceStopped > stoppedTime) TransitionToState(CharacterState.Default);

                break;
            }
        }
    }

    /// Return true if character controller can collide with this collider else is false
    public bool IsColliderValidForCollisions(Collider coll)
    {
        string collLayer = LayerMask.LayerToName(coll.gameObject.layer);
        // Checks if layer is in ignore list
        if (ignoredLayers.Contains(collLayer)) return false;

        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
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
                if (allowWallJump && !characterMotor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                {
                    canWallJump = true;
                    wallJumpNormal = hitNormal;
                }

                break;
            }
            case CharacterState.Charging:
            {
                // Detect being stopped by obstructions
                if (!isStopped && !hitStabilityReport.IsStable && Vector3.Dot(-hitNormal, currentChargeVelocity.normalized) > 0.5f)
                {
                    mustStopVelocity = true;
                    isStopped = true;
                }
                break;
            }
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    private void OnLanded()
    {
        Debug.Log("Landed");
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
}
