using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public struct PlayerCharacterInputs
{
    // All inputs the player can give to the character
    public float MoveAxisForward;
    public float MoveAxisRight;
    public Quaternion CameraRotation;
    public bool JumpDown;
}

public class CharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor characterMotor;
    
    [Header("Stable Movement")]
    public float maxStableMoveSpeed = 10f;
    public float stableMovementSharpness = 15;
    public float orientationSharpness = 10;

    [Header("Air Movement")]
    public float maxAirMoveSpeed = 10f;
    public float airAccelerationSpeed = 5f;
    public float drag = 0.1f;
    
    [Header("Jumping")]
    public bool allowJumpingWhenSliding = false;
    public bool allowDoubleJump = false;
    public float jumpSpeed = 10f;
    [Tooltip("Time before landing where jump input will still allow jump once you land.")] 
    public float jumpPreGroundingGraceTime = 0f;
    [Tooltip("Time after leaving stable ground where jump will still be allowed")]
    public float jumpPostGroundingGraceTime = 0f;

    [Header("Misc")]
    public bool rotationObstruction;
    public Vector3 gravity = new Vector3(0, -30f, 0);
    public Transform meshRoot;
    
    // Input vectors
    private Vector3 moveInputVector;
    private Vector3 lookInputVector;
    
    // Jump vars
    private bool jumpRequested = false;
    private bool jumpConsumed = false;
    private bool jumpedThisFrame = false;
    private float timeSinceJumpRequested = Mathf.Infinity;
    private float timeSinceLastAbleToJump = 0f;
    private bool doubleJumpConsumed = false;
    
    // Will have the character face camera look direction
    private bool rotateToCameraFacing = true;

    /// Start is called before the first frame update
    void Start()
    {
        // Assigns as the motors controller
        characterMotor.CharacterController = this;
    }

    /// Update is called once per frame
    void Update()
    {
        
    }

    /// This is called every frame by InputHandler to tell character what inputs its receiving
    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 _moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);

        // Move and look inputs
        moveInputVector = cameraPlanarRotation * _moveInputVector;
        lookInputVector = cameraPlanarDirection;
        
        // Jumping input
        if (inputs.JumpDown)
        {
            timeSinceJumpRequested = 0f;
            jumpRequested = true;
        }
    }

    /// This is where you tell your character what its rotation should be right now. 
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (rotateToCameraFacing && lookInputVector != Vector3.zero && orientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward, lookInputVector, 
                1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, 
                characterMotor.CharacterUp);
        }
    }

    /// This is where you tell your character what its velocity should be right now. 
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
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

                Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, gravity);
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
                if (jumpConsumed && !doubleJumpConsumed && (allowJumpingWhenSliding ? 
                    !characterMotor.GroundingStatus.FoundAnyGround : !characterMotor.GroundingStatus.IsStableOnGround))
                {
                    characterMotor.ForceUnground(0.1f);

                    // Add to the return velocity and reset jump state
                    currentVelocity += (characterMotor.CharacterUp * jumpSpeed) - Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                    jumpRequested = false;
                    doubleJumpConsumed = true;
                    jumpedThisFrame = true;
                }
            }
            
            // See if we actually are allowed to jump
            if (!jumpConsumed && ((allowJumpingWhenSliding ? characterMotor.GroundingStatus.FoundAnyGround : 
                characterMotor.GroundingStatus.IsStableOnGround) || timeSinceLastAbleToJump <= jumpPostGroundingGraceTime))
            {
                // Calculate jump direction before un-grounding
                Vector3 jumpDirection = characterMotor.CharacterUp;
                if (characterMotor.GroundingStatus.FoundAnyGround && !characterMotor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = characterMotor.GroundingStatus.GroundNormal;
                }

                // Makes the character skip ground probing/snapping on its next update. 
                characterMotor.ForceUnground(0.1f);

                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                jumpRequested = false;
                jumpConsumed = true;
                jumpedThisFrame = true;
            }
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    /// This is called after the character has finished its movement update
    public void AfterCharacterUpdate(float deltaTime)
    {
        // Handle jumping pre-ground grace period
        if (jumpRequested && timeSinceJumpRequested > jumpPreGroundingGraceTime) jumpRequested = false;

        // Handle jumping while sliding
        if (allowJumpingWhenSliding ? characterMotor.GroundingStatus.FoundAnyGround : characterMotor.GroundingStatus.IsStableOnGround)
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
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}
