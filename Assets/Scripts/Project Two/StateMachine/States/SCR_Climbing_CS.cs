using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public enum ClimbingState
{
    Anchoring,
    Climbing,
    DeAnchoring
}

public class SCR_Climbing_CS : IState
{
    private KinematicCharacterMotor characterMotor;
    private ProjectTwo.CharacterController characterController;
    
    // Ladder vars
    private Ladder activeLadder { get; set; }
    private ClimbingState internalClimbingState;
    private ClimbingState climbingState
    {
        get
        {
            return internalClimbingState;
        }
        set
        {
            internalClimbingState = value;
            anchoringTimer = 0f;
            anchoringStartPosition = characterMotor.TransientPosition;
            anchoringStartRotation = characterMotor.TransientRotation;
        }
    }
    private Vector3 ladderTargetPosition;
    private Quaternion ladderTargetRotation;
    private float onLadderSegmentState = 0;
    private float anchoringTimer = 0f;
    private Vector3 anchoringStartPosition = Vector3.zero;
    private Quaternion anchoringStartRotation = Quaternion.identity;
    private Quaternion rotationBeforeClimbing = Quaternion.identity;

    private float ladderUpDownInput;
    
    public SCR_Climbing_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
    {
        characterMotor = _characterMotor;
        characterController = _characterController;
    }
    
    public void EnterState()
    {
        rotationBeforeClimbing = characterMotor.TransientRotation;

        characterMotor.SetMovementCollisionsSolvingActivation(false);
        characterMotor.SetGroundSolvingActivation(false);
        climbingState = ClimbingState.Anchoring;

        // Store the target position and rotation to snap to
        ladderTargetPosition = activeLadder.ClosestPointOnLadderSegment(characterMotor.TransientPosition, out onLadderSegmentState);
        ladderTargetRotation = activeLadder.transform.rotation;
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        //TODO: have character switch to this state on interact key (INTERACTION SYSTEM)
        
        // Handle ladder transitions
        ladderUpDownInput = inputs.MoveAxisForward;

        if (characterMotor.CharacterOverlap(characterMotor.TransientPosition, characterMotor.TransientRotation, characterController.probedColliders, 
            characterController.characterSettings.interactionLayer, QueryTriggerInteraction.Collide) > 0)
        {
            if (characterController.probedColliders[0] != null)
            {
                // Handle ladders
                Ladder ladder = characterController.probedColliders[0].gameObject.GetComponent<Ladder>();
                if (ladder)
                {
                    // Transition to ladder climbing state
                    //if (CurrentCharacterState == CharacterState.Default)
                    {
                        //TODO: This will need to happen in the interaction system:
                        //activeLadder = ladder;
                        //TransitionToState(CharacterState.Climbing);
                    }
                    // Transition back to default movement state
                    climbingState = ClimbingState.DeAnchoring;
                    ladderTargetPosition = characterMotor.TransientPosition;
                    ladderTargetRotation = rotationBeforeClimbing;
                    
                }
            }
        }
    }

    public void ExitState()
    {
       characterMotor.SetMovementCollisionsSolvingActivation(true);
       characterMotor.SetGroundSolvingActivation(true);

       // Reset
       characterController.FinishCurrentState(false);
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = Vector3.zero;

        switch (climbingState)
        {
            case ClimbingState.Climbing:
            {
                currentVelocity = (ladderUpDownInput * activeLadder.transform.up).normalized *
                                  characterController.characterSettings.climbingSpeed;
                break;
            }
            case ClimbingState.Anchoring:
            case ClimbingState.DeAnchoring:
            {
                Vector3 tmpPosition = Vector3.Lerp(anchoringStartPosition, ladderTargetPosition,
                    (anchoringTimer / characterController.characterSettings.anchoringDuration));
                currentVelocity = characterMotor.GetVelocityForMovePosition(characterMotor.TransientPosition, tmpPosition, deltaTime);
                break;
            }
        }
    }

    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime)
    {
        switch (climbingState)
        {
            case ClimbingState.Climbing:
            {
                currentRotation = activeLadder.transform.rotation;
                break;
            }
            case ClimbingState.Anchoring:
            case ClimbingState.DeAnchoring:
            {

                currentRotation = Quaternion.Slerp(anchoringStartRotation, ladderTargetRotation,
                    (anchoringTimer / characterController.characterSettings.anchoringDuration));
                break;
            }
        }
    }

    public void StateBeforeCharacterUpdate(float deltaTime)
    {
        throw new System.NotImplementedException();
    }

    public void StateAfterCharacterUpdate(float deltaTime)
    {
        switch (climbingState)
        {
            case ClimbingState.Climbing:
            {
                // Detect getting off ladder during climbing
                activeLadder.ClosestPointOnLadderSegment(characterMotor.TransientPosition, out onLadderSegmentState);
                if (Mathf.Abs(onLadderSegmentState) > 0.05f)
                {
                    climbingState = ClimbingState.DeAnchoring;

                    // If we're higher than the ladder top point
                    if (onLadderSegmentState > 0)
                    {
                        ladderTargetPosition = activeLadder.GetTopReleasePoint.position;
                        ladderTargetRotation = activeLadder.GetTopReleasePoint.rotation;
                    }
                    // If we're lower than the ladder bottom point
                    else if (onLadderSegmentState < 0)
                    {
                        ladderTargetPosition = activeLadder.GetBottomReleasePoint.position;
                        ladderTargetRotation = activeLadder.GetBottomReleasePoint.rotation;
                    }
                }
                break;
            }
            case ClimbingState.Anchoring:
            case ClimbingState.DeAnchoring:
                // Detect transitioning out from anchoring states
                if (anchoringTimer >= characterController.characterSettings.anchoringDuration)
                {
                    if (climbingState == ClimbingState.Anchoring)
                    {
                        climbingState = ClimbingState.Climbing;
                    }
                    else if (climbingState == ClimbingState.DeAnchoring)
                    {
                        characterController.FinishCurrentState(true);
                        //TransitionToState(CharacterState.Default);
                    }
                }

                // Keep track of time since we started anchoring
                anchoringTimer += deltaTime;
                break;
        }
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        
    }
}
