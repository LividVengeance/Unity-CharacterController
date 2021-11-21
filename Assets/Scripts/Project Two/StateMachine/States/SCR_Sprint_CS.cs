using KinematicCharacterController;
using UnityEngine;

public class SCR_Sprint_CS : IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;

    public SCR_Sprint_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }

    public void EnterState()
    {
        characterMotor.SetGroundSolvingActivation(true);
    }

    public void Tick(ref ProjectTwo.PlayerCharacterInputs inputs)
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
        
        characterController.MoveAndLookInput(cameraPlanarRotation, moveInputVec, cameraPlanarDirection);
                    
        // Requests a jump if the key is down
        characterController.JumpRequestCheck(ref inputs); 
    }

    public void ExitState()
    {
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

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        // Handles ground movement if has move input
        characterController.GroundMovementHandler(ref currentVelocity, deltaTime, 
            characterController.characterSettings.sprintMovementSharpness, 
            characterController.characterSettings.maxSprintSpeed);

        // Handles jump request if has jump input
        if (characterController.characterSettings.allowJumpingWhileSprinting) 
            characterController.JumpHandler(ref currentVelocity, deltaTime);
                    
        // Handles external forces to the character controller
        characterController.ExternalForceHandler(ref currentVelocity);
    }
    

    public void StateAfterCharacterUpdate(float deltaTime)
    {
        Vector3 characterRelativeVelocity = characterController.transform.InverseTransformVector(characterMotor.Velocity);
        // Animation
        characterController.animator.SetFloat("velocityZ", Mathf.Clamp(characterRelativeVelocity.z, -2, 2));
        characterController.animator.SetFloat("velocityX", Mathf.Clamp(characterRelativeVelocity.x, -2, 2));
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void SetInteractObject(GameObject interactGameObject = null)
    {
    }
}
