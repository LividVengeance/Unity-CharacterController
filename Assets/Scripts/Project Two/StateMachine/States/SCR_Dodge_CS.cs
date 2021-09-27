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

    private bool hasDodged;

    private float test;
    
    public SCR_Dodge_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }
    
    public void EnterState()
    {
        characterController.animator.SetTrigger(dodgeTriggerHash);
        hasDodged = true;
        Debug.Log("Entered Dodge State");
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        if (hasDodged)
        {
            Debug.Log("FWD: " + inputs.MoveAxisForward + "        |RIGHT: " + inputs.MoveAxisRight);
            if (inputs.MoveAxisForward > 1 || inputs.MoveAxisForward < 1) test = inputs.MoveAxisForward;
            else if (inputs.MoveAxisRight > 1 || inputs.MoveAxisRight < 1) test = inputs.MoveAxisRight;
            characterController.FinishCurrentState(true);
        }
    }

    public void ExitState()
    {
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        if (hasDodged)
        {
            currentVelocity += currentVelocity * (test * 20f);
            hasDodged = false;
        }
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
