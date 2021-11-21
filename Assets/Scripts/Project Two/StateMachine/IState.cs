using KinematicCharacterController;
using UnityEngine;

public interface IState
{
    public void EnterState();
    public void Tick(ref ProjectTwo.PlayerCharacterInputs inputs);
    public void ExitState();
    public void StateVelocityUpdate(ref Vector3 currentVelocity, float deltaTime);
    public void StateRotationUpdate(ref Quaternion currentRotation, float deltaTime);

    public void StateBeforeCharacterUpdate(float deltaTime);
    public void StateAfterCharacterUpdate(float deltaTime);

    public void StateOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport);

    public void SetInteractObject(GameObject interactGameObject = null);
}
