using System.Collections.Generic;
using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

public class PushCube : MonoBehaviour, IState
{
    private bool debugInteractionPoints = true;
    
    private List<Vector3> interactionPoints = new List<Vector3>();
    private Vector3 closestPoint;
    
    public enum PushPullState
    {
        None,
        Enter,
        Tick,
        Exit,
    };
    private PushPullState pushPullState = PushPullState.None;

    private bool inPosition;
    private GameObject interactObject;

    private KinematicCharacterMotor _characterMotor;
    private ProjectTwo.CharacterController _characterController;

    public PushCube(KinematicCharacterMotor characterMotor, ProjectTwo.CharacterController characterController)
    {
        _characterMotor = characterMotor;
        _characterController = characterController;
    }

    private void OnDrawGizmos()
    {
        if (debugInteractionPoints)
        {
            Gizmos.color = Color.blue;
            foreach (var point in interactionPoints) Gizmos.DrawWireSphere(point, .1f);
        }
    }

    public void EnterState()
    {
        interactionPoints = new List<Vector3>();
        
        // Gets Push/Pull interaction positions
        Vector3 interactObjPos = new Vector3(interactObject.transform.position.x, 
            _characterController.transform.position.y, interactObject.transform.position.z);
        interactionPoints.Add((interactObjPos - interactObject.transform.forward * 1));
        interactionPoints.Add((interactObjPos + interactObject.transform.forward * 1));
        interactionPoints.Add((interactObjPos - interactObject.transform.right * 1));
        interactionPoints.Add((interactObjPos + interactObject.transform.right * 1));

        closestPoint = GetClosestPoint();
        inPosition = false;
        
        pushPullState = PushPullState.Enter;
        
        Debug.Log("Start PushCube State");
    }
    
    private Vector3 GetClosestPoint()
    {
        // Gets the closest point to the player
        interactionPoints.Sort(SortByDistance);
        return (interactionPoints[0]);
    }
    
    private int SortByDistance(Vector3 a, Vector3 b)
    {
        float distA = (_characterController.transform.position - a).sqrMagnitude;
        float distB = (_characterController.transform.position - b).sqrMagnitude;
        if (distA < distB) return -1;
        if (distA > distB) return 1;
        return 0;
    }

    public void Tick(ref PlayerCharacterInputs inputs)
    {
        // Updates only the current state
        switch (pushPullState)
        {
            case PushPullState.Enter:
                StartPushPullUpdate();
                break;
            case PushPullState.Tick:
                TickPushPullUpdate();
                break;
        }
        
        Debug.Log("Tick PushCube State");
    }

    private void StartPushPullUpdate()
    {
        if (closestPoint != Vector3.zero) _characterController.SetMoveToPoint(closestPoint, 2, 5, 0.2f);
        Debug.Log(closestPoint);
    }

    private void TickPushPullUpdate()
    {
        
    }

    public void ExitState()
    {
        _characterController.interactionState = false;
        Debug.Log("Exit PushCube State");
    }

    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime)
    {
        //throw new NotImplementedException();
    }

    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime)
    {
        //throw new NotImplementedException();
    }

    public void StateBeforeCharacterUpdate(float deltaTime)
    {
        //throw new NotImplementedException();
    }

    public void StateAfterCharacterUpdate(float deltaTime)
    {
        //throw new NotImplementedException();
    }

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        //throw new NotImplementedException();
    }

    public void SetInteractObject(GameObject interactGameObject = null) => interactObject = interactGameObject;
}
