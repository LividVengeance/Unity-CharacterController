using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_Dodge_CS : MonoBehaviour, IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    
    // Animation Hash
    private const string dodgeTriggerHash = "dodgeTrigger";

    private bool hasNotDodged;
    private bool notHasDir;
    private Vector3 dodgeDirection;
    
    public SCR_Dodge_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }
    
    public void EnterState()
    {
        hasNotDodged = true;
        notHasDir = true;
        dodgeDirection = Vector3.zero;
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        if (notHasDir)
        {
            // Forward
            if (inputs.MoveAxisForward > 0 && characterController.characterSettings.DodgeDirectionCheck("Forward") && 
                ((!characterController.characterSettings.dodgeInAir || characterController.characterSettings.dodgeInAir && characterMotor.GroundingStatus.IsStableOnGround) || (characterController.characterSettings.dodgeInAir 
                && characterController.characterSettings.AirDodgeDirectionCheck("Forward"))))
            {
                dodgeDirection = characterMotor.CharacterForward;
            }
            // Back
            else if (inputs.MoveAxisForward < 0 && characterController.characterSettings.DodgeDirectionCheck("Back") && 
                ((!characterController.characterSettings.dodgeInAir|| characterController.characterSettings.dodgeInAir && characterMotor.GroundingStatus.IsStableOnGround) || (characterController.characterSettings.dodgeInAir 
                && characterController.characterSettings.AirDodgeDirectionCheck("Back"))))
            {
                dodgeDirection = -characterMotor.CharacterForward;
            }
            // Right
            else if (inputs.MoveAxisRight > 0 && characterController.characterSettings.DodgeDirectionCheck("Right") && 
                ((!characterController.characterSettings.dodgeInAir|| characterController.characterSettings.dodgeInAir && characterMotor.GroundingStatus.IsStableOnGround) || (characterController.characterSettings.dodgeInAir 
                && characterController.characterSettings.AirDodgeDirectionCheck("Right"))))
            {
                dodgeDirection = characterMotor.CharacterRight;
            }
            // Left
            else if (inputs.MoveAxisRight < 0 && characterController.characterSettings.DodgeDirectionCheck("Left") && 
                ((!characterController.characterSettings.dodgeInAir|| characterController.characterSettings.dodgeInAir && characterMotor.GroundingStatus.IsStableOnGround) || (characterController.characterSettings.dodgeInAir 
                && characterController.characterSettings.AirDodgeDirectionCheck("Left"))))
            {
                dodgeDirection = -characterMotor.CharacterRight;
            }

            Debug.Log(dodgeDirection);
            notHasDir = false;
        }
    }

    public void ExitState()
    {
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        if (hasNotDodged && !notHasDir && dodgeDirection != Vector3.zero)
        {
            characterController.animator.SetTrigger(dodgeTriggerHash);
            
            // Gets the force depending if the character is in the air or not
            float dodgeForce = (characterController.characterSettings.dodgeInAir && !characterMotor.GroundingStatus.FoundAnyGround) 
                ? characterController.characterSettings.dodgeAirForce : characterController.characterSettings.dodgeForce;
            
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
}
