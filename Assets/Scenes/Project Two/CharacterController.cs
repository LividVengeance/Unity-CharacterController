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

    [Header("Misc")]
    public bool rotationObstruction;
    public Vector3 gravity = new Vector3(0, -30f, 0);
    public Transform meshRoot;
    
    // Input vectors
    private Vector3 moveInputVector;
    private Vector3 lookInputVector;
    
    // Will have the character face camera look direction
    private bool rotateToCameraFacing = true;

    // Start is called before the first frame update
    void Start()
    {
        // Assigns as the motors controller
        characterMotor.CharacterController = this;
    }

    // Update is called once per frame
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
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
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
