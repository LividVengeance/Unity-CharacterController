using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_PowerSlide_CS : IState
{
    //TODO: Have character increase velocity till max velocity based on the initial velocity
    //(upon enter of the power slide state) 
    
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    
    // Animation Hash
    private const string slideTriggerHash = "slideTrigger";

    // Sliding vars
    private Vector3 desiredVelocity;
    
    
    private Vector3 currentSlideVelocity;
    //private float currentSlideAcceleration;
    private float currentSlideTime;
    private bool isStopped;
    
    public SCR_PowerSlide_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }

    public void EnterState()
    {
        characterController.animator.SetTrigger(slideTriggerHash);

        desiredVelocity = characterMotor.Velocity + (characterMotor.CharacterForward * characterController.characterSettings.maxPowerSlideSpeed);
        
        currentSlideVelocity = characterMotor.Velocity + (
            characterMotor.CharacterForward * characterController.characterSettings.initialSlideForce);
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
    }

    public void ExitState()
    {
        characterController.animator.SetTrigger(slideTriggerHash);
        currentSlideTime = 0f;

        // Resets
        characterController.FinishCurrentState(false);
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        currentSlideTime += deltaTime;
        
        // Get normal to the ground
        RaycastHit hit;
        if (Physics.Raycast(characterController.transform.position, -characterMotor.CharacterUp, out hit))
        {
            Vector3 slopeAngle = hit.normal;
            float angle = Vector3.Angle(characterMotor.CharacterForward, slopeAngle);

            if ((angle < 100) && (currentSlideTime < characterController.characterSettings.maxSlideTime))
            {
                // Checks if already at max slide velocity (based on the initial velocity)
                if (currentVelocity.magnitude < desiredVelocity.magnitude)
                {
                    currentVelocity += currentSlideVelocity * (characterController.characterSettings.maxSlideAcceleration * deltaTime);
                }
                else currentVelocity = Vector3.zero;
                currentVelocity += characterController.characterSettings.gravity * deltaTime;
            }
            // Angle is too sleep
            else
            {
                Debug.Log("The angle too the ground is too steep OR current slide time to large");
                currentVelocity *= -(characterController.characterSettings.maxSlideDecelerate * deltaTime);
            }
        }
        
        if (currentVelocity == Vector3.zero) characterController.FinishCurrentState(true);
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