using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_Default_CS : MonoBehaviour, IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;

    public SCR_Default_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }
    
    public void EnterState()
    {
        characterMotor.SetGroundSolvingActivation(true); 
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 moveInputVec =
            Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3
                .ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
        }

        Quaternion cameraPlanarRotation =
            Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);
        
        // Move and look inputs
        characterController.MoveAndLookInput(cameraPlanarRotation, moveInputVec, cameraPlanarDirection);

        // Requests a jump if the key is down
        characterController.JumpRequestCheck(ref inputs);

        // Crouches if the key is down
        characterController.CrouchHandler(ref inputs);
    }

    public void ExitState()
    {
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        // Handles ground movement if has move input
        characterController.GroundMovementHandler(ref currentVelocity, deltaTime, characterController.characterSettings.stableMovementSharpness, characterController.characterSettings.maxStableMoveSpeed);
        
        // Handles jump if jump requested
        characterController.JumpHandler(ref currentVelocity, deltaTime);

        // Handles external forces to the character controller
        characterController.ExternalForceHandler(ref currentVelocity);
    }
    
    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime)
    {
        if (characterController.rotateToCameraFacing && characterController.lookInputVector != Vector3.zero && characterController.characterSettings.orientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward,
                characterController.lookInputVector,
                1 - Mathf.Exp(-characterController.characterSettings.orientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection,
                characterMotor.CharacterUp);

            if (characterController.characterSettings.orientTowardsGravity)
            {
                // Rotate from current up to invert gravity
                currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -characterController.characterSettings.gravity) *
                                  currentRotation;
            }
        }
    }

    public void StateBeforeCharacterUpdate(float deltaTime)
    {
    }

    public void StateAfterCharacterUpdate(float deltaTime)
    {
        Vector3 characterRelativeVelocity = characterController.transform.InverseTransformVector(characterMotor.Velocity);
        // Animation
        characterController.animator.SetFloat("velocityZ", (characterRelativeVelocity.z / 
                                                            characterController.characterSettings.maxStableMoveSpeed));
        characterController.animator.SetFloat("velocityX", characterRelativeVelocity.x / 
                                                           characterController.characterSettings.maxStableMoveSpeed);
        
        // Handle jumping pre-ground grace period
        if (characterController.jumpRequested && characterController.timeSinceJumpRequested > 
            characterController.characterSettings.jumpPreGroundingGraceTime) characterController.jumpRequested = false;

        // Handle jumping while sliding
        if (characterController.characterSettings.allowJumpingWhenSliding
            ? characterMotor.GroundingStatus.FoundAnyGround
            : characterMotor.GroundingStatus.IsStableOnGround)
        {
            // If we're on a ground surface, reset jumping values
            if (!characterController.jumpedThisFrame)
            {
                characterController.doubleJumpConsumed = false;
                characterController.jumpConsumed = false;
            }

            characterController.timeSinceLastAbleToJump = 0f;
        }
        // Keep track of time since we were last able to jump (for grace period)
        else characterController.timeSinceLastAbleToJump += deltaTime;

        // Handle un-crouching
        if (characterController.isCrouching && !characterController.shouldBeCrouching)
        {
            // Do an overlap test with the character's standing height to see if there are any obstructions
            characterMotor.SetCapsuleDimensions(0.5f, 2f, 1f);
            if (characterMotor.CharacterCollisionsOverlap(
                characterMotor.TransientPosition,
                characterMotor.TransientRotation,
                characterController.probedColliders) > 0)
            {
                // If obstructions, just stick to crouching dimensions
                characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            }
            else
            {
                // If no obstructions, un-crouch
                characterController.characterSettings.meshRoot.localScale = new Vector3(1f, 1f, 1f);
                characterController.isCrouching = false;
            }
        }
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        // We can wall jump only if we are not stable on ground and are moving against an obstruction
        if (characterController.characterSettings.allowWallJump && !characterMotor.GroundingStatus.IsStableOnGround &&
            !hitStabilityReport.IsStable)
        {
            characterController.canWallJump = true;
            characterController.wallJumpNormal = hitNormal;
        }
    }
}