using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class SCR_Charge_CS : IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    
    // Charging vars
    private Vector3 currentChargeVelocity;
    private bool isStopped;
    private bool mustStopVelocity = false;
    private float timeSinceStartedCharge = 0;
    private float timeSinceStopped = 0;

    public SCR_Charge_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }
    
    public void EnterState()
    {
        //finishedCharge = false;
        // Cache a charging velocity based on the character’s forward direction
        currentChargeVelocity = characterMotor.CharacterForward * characterController.characterSettings.chargeSpeed;

        // Setup values
        isStopped = false;
        timeSinceStartedCharge = 0f;
        timeSinceStopped = 0f;
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
    }

    public void ExitState()
    {
        // Resets
        characterController.FinishCurrentState(false);
    }

    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        // If we have stopped and need to cancel velocity, do it here
        if (mustStopVelocity)
        {
            currentVelocity = Vector3.zero;
            mustStopVelocity = false;
        }

        // When stopped, do no velocity handling except gravity
        if (isStopped) currentVelocity += characterController.characterSettings.gravity * deltaTime;
        else
        {
            // When charging, velocity is always constant
            float previousY = currentVelocity.y;
            currentVelocity = currentChargeVelocity;
            currentVelocity.y = previousY;
            currentVelocity += characterController.characterSettings.gravity * deltaTime;
        }
    }

    public void StateBeforeCharacterUpdate(float deltaTime)
    {
        // Update times
        timeSinceStartedCharge += deltaTime;
        if (isStopped) timeSinceStopped += deltaTime;
    }

    public void StateAfterCharacterUpdate(float deltaTime)
    {
        // Detect being stopped by elapsed time
        if (!isStopped && timeSinceStartedCharge > characterController.characterSettings.maxChargeTime)
        {
            mustStopVelocity = true;
            isStopped = true;
        }

        // Detect end of stopping phase and transition back to default movement state
        if (timeSinceStopped > characterController.characterSettings.stoppedTime)
        {
            characterController.FinishCurrentState(true);
        }
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        // Detect being stopped by obstructions
        if (!isStopped && !hitStabilityReport.IsStable &&
            Vector3.Dot(-hitNormal, currentChargeVelocity.normalized) > 0.5f)
        {
            mustStopVelocity = true;
            isStopped = true;
        }
    }

    public void SetInteractObject(GameObject interactGameObject = null)
    {
    }
}
