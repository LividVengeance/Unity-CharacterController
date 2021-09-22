using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_NoClip_CS : MonoBehaviour, IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    
    public SCR_NoClip_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }

    public void EnterState()
    {
        // Bypass the custom collision detection
        characterMotor.SetCapsuleCollisionsActivation(false);
        characterMotor.SetMovementCollisionsSolvingActivation(false);
        characterMotor.SetGroundSolvingActivation(false);
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
    }

    public void ExitState()
    {
        // Use the custom collision detection
        characterMotor.SetCapsuleCollisionsActivation(true);
        characterMotor.SetMovementCollisionsSolvingActivation(true);
        characterMotor.SetGroundSolvingActivation(true);
    }
    
    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {

        float verticalInput = 0f + (characterController.jumpInputIsHeld ? 1f : 0f) + (characterController.crouchInputIsHeld ? -1f : 0f);
        // Smoothly interpolate to target velocity
        Vector3 targetMovementVelocity =
            (characterController.moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * characterController.characterSettings.noClipMoveSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
            1 - Mathf.Exp(-characterController.characterSettings.noClipSharpness * deltaTime));
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
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }
}
