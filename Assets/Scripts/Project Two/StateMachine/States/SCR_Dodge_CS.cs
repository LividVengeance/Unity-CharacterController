using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_Dodge_CS : IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    private CharacterSettings characterSettings;
    
    // Animation Hash
    private const string dodgeTriggerHash = "dodgeTrigger";

    private bool wasSprinting;
    private bool hasNotDodged;
    private bool notHasDir;
    private Vector3 dodgeDirection;
    
    public SCR_Dodge_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;

        characterSettings = characterController.characterSettings;
    }
    
    public void EnterState()
    {
        hasNotDodged = true;
        notHasDir = true;
        dodgeDirection = Vector3.zero;

        wasSprinting = characterController.GetPreviousState.GetType() == typeof(SCR_Sprint_CS);
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        if (notHasDir)
        {
            // Forward
            if (inputs.MoveAxisForward > 0 &&  DirectionCheck("Forward")) dodgeDirection = characterMotor.CharacterForward;
            // Back
            else if (inputs.MoveAxisForward < 0 &&  DirectionCheck("Back")) dodgeDirection = -characterMotor.CharacterForward;
            // Right
            else if (inputs.MoveAxisRight > 0 &&  DirectionCheck("Right")) dodgeDirection = characterMotor.CharacterRight;
            // Left
            else if (inputs.MoveAxisRight < 0 && DirectionCheck("Left")) dodgeDirection = -characterMotor.CharacterRight;

            notHasDir = false;
        }
    }

    private bool DirectionCheck(string direction)
    {
        // Checks direction when character is sprinting
        if (wasSprinting && characterSettings.dodgeInSprint) 
            return (characterSettings.dodgeInSprint && characterSettings.SprintDodgeDirectionCheck(direction));
        // Checks direction when the character is in the air
        if (characterSettings.dodgeInAir && !characterMotor.GroundingStatus.IsStableOnGround)
            return characterSettings.AirDodgeDirectionCheck(direction);
        
        // Checks direction when on the ground
        if (characterMotor.GroundingStatus.IsStableOnGround) 
            return characterSettings.DodgeDirectionCheck(direction);
        
        return false;
    }

    public void ExitState()
    {
        wasSprinting = false;
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        if (hasNotDodged && !notHasDir && dodgeDirection != Vector3.zero)
        {
            characterController.animator.SetTrigger(dodgeTriggerHash);
            
            // Gets the force depending if the character is in the air or not
            float dodgeForce = (characterSettings.dodgeInAir && !characterMotor.GroundingStatus.FoundAnyGround) 
                ? characterSettings.dodgeAirForce : characterSettings.dodgeForce;

            if (wasSprinting) dodgeForce = characterSettings.dodgeSprintForce;
            
            currentVelocity += dodgeDirection * dodgeForce;
            hasNotDodged = false;
            characterController.FinishCurrentState(true);
        }
        else if (dodgeDirection == Vector3.zero) characterController.FinishCurrentState(true);
    }

    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime)
    {
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

    public void SetInteractObject(GameObject interactGameObject = null)
    {
    }
}
