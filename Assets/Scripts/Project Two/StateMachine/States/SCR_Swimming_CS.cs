using KinematicCharacterController;
using ProjectTwo;
using UnityEngine;

//public class SCR_Swimming_CS : MonoBehaviour, IState, ICharacterController
//{
//    private KinematicCharacterMotor characterMotor;
//    private ProjectTwo.CharacterController characterController;
//    
//    public SCR_Swimming_CS(KinematicCharacterMotor _characterMotor, ProjectTwo.CharacterController _characterController)
//    {
//        characterMotor = _characterMotor;
//        characterController = _characterController;
//    }
//    
//    public void EnterState()
//    {
//        characterMotor.CharacterController = this;
//        
//        characterMotor.SetGroundSolvingActivation(false);
//    }
//
//    public void Tick(ref PlayerCharacterInputs inputs)
//    {
//        characterController.jumpRequested = inputs.JumpHeld;
//
//        // Clamp input
//        Vector3 moveInputVec =
//            Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
//
//        // Calculate camera direction and rotation on the character plane
//        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
//        if (cameraPlanarDirection.sqrMagnitude == 0f)
//        {
//            cameraPlanarDirection = Vector3
//                .ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
//        }
//
//        Quaternion cameraPlanarRotation =
//            Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);
//        
//        // Move and look inputs
//        characterController.MoveAndLookInput(cameraPlanarRotation, moveInputVec, cameraPlanarDirection);
//    }
//
//    public void ExitState()
//    {
//        
//    }
//
//    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
//    {
//        if (characterController.rotateToCameraFacing && characterController.lookInputVector != Vector3.zero && characterController.characterSettings.orientationSharpness > 0f)
//        {
//            // Smoothly interpolate from current to target look direction
//            Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward,
//                characterController.lookInputVector,
//                1 - Mathf.Exp(-characterController.characterSettings.orientationSharpness * deltaTime)).normalized;
//
//            // Set the current rotation (which will be used by the KinematicCharacterMotor)
//            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection,
//                characterMotor.CharacterUp);
//
//            if (characterController.characterSettings.orientTowardsGravity)
//            {
//                // Rotate from current up to invert gravity
//                currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -characterController.characterSettings.gravity) *
//                                  currentRotation;
//            }
//        }
//    }
//
//    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
//    {
//        float verticalInput = 0f + (characterController.jumpInputIsHeld ? 1f : 0f) + (characterController.crouchInputIsHeld ? -1f : 0f);
//
//        // Smoothly interpolate to target swimming velocity
//        Vector3 targetMovementVelocity =
//            (characterController.moveInputVector + (characterMotor.CharacterUp * verticalInput)).normalized * characterController.characterSettings.swimmingSpeed;
//        Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
//            1 - Mathf.Exp(-characterController.characterSettings.swimmingMovementSharpness * deltaTime));
//
//        // See if our swimming reference point would be out of water after the movement from our velocity has been applied
//
//        Vector3 resultingSwimmingReferancePosition =
//            characterMotor.TransientPosition + (smoothedVelocity * deltaTime) +
//            (characterController.characterSettings.swimmingReferencePoint.position - characterMotor.TransientPosition);
//        Vector3 closestPointWaterSurface = Physics.ClosestPoint(resultingSwimmingReferancePosition,
//            characterController.waterZone, characterController.waterZone.transform.position, characterController.waterZone.transform.rotation);
//
//        // if our position would be outside the water surface on next update, project the velocity
//        // on the surface normal so that it would not take us out of the water
//        if (closestPointWaterSurface != resultingSwimmingReferancePosition)
//        {
//            Vector3 waterSurfaceNormal =
//                (resultingSwimmingReferancePosition - closestPointWaterSurface).normalized;
//            smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);
//
//            // Jump out of water
//            if (characterController.jumpRequested)
//            {
//                smoothedVelocity += (characterMotor.CharacterUp * characterController.characterSettings.jumpSpeed) - Vector3.Project(
//                    currentVelocity, characterMotor.CharacterUp);
//            }
//        }
//
//        currentVelocity = smoothedVelocity;
//    }
//
//    public void BeforeCharacterUpdate(float deltaTime)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public void PostGroundingUpdate(float deltaTime)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public void AfterCharacterUpdate(float deltaTime)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public bool IsColliderValidForCollisions(Collider coll)
//    {
//        string collLayer = LayerMask.LayerToName(coll.gameObject.layer);
//        // Checks if layer is in ignore list
//        if (characterController.characterSettings.ignoredLayers.Contains(collLayer)) return false;
//
//        return true;
//    }
//
//    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
//        ref HitStabilityReport hitStabilityReport)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
//        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
//    {
//        throw new System.NotImplementedException();
//    }
//
//    public void OnDiscreteCollisionDetected(Collider hitCollider)
//    {
//        throw new System.NotImplementedException();
//    }
//}
