using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class IKFootPlacements : MonoBehaviour
{
    private Animator animator;

    private Vector3 rightFootPos;
    private Vector3 leftFootPos;
    private Vector3 leftFootIKPos;
    private Vector3 rightFootIKPos;
    private Quaternion leftFootIKRot;
    private Quaternion rightFootIKRot;
    private float lastPelvisPosY;
    private float lastLeftFootPosY;
    private float lastRightFootPosY;

    [Header("Foot IK")] 
    [SerializeField] private bool enableFootIK = true;
    [SerializeField, Range(0, 2f)] private float heightFromGroundRaycast = 1.14f;
    [SerializeField, Range(0, 2f)] private float raycastDownDistance = 1.5f;
    [SerializeField] private LayerMask environmentLayerMask;
    [SerializeField] private float pelvisOffset = 0f;
    [SerializeField, Range(0, 1)] private float pelvisUpDownSpeed = 0.28f;
    [SerializeField, Range(0, 1)] private float feetToIKPosSpeed = 0.5f;

    [SerializeField] private string leftFootAnimVarName = "LeftFootCurve";
    [SerializeField] private string rightFootAnimVarName = "RightFootCurve";

    [SerializeField] private bool useProIK = false;
    [SerializeField] private bool showDebugSolver = false; 
    
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if(!enableFootIK) return;
        
        AdjustFeetTarget(ref rightFootPos, HumanBodyBones.RightFoot);
        AdjustFeetTarget(ref leftFootPos, HumanBodyBones.LeftFoot);
        
        // Find and raycast to the ground to find pos
        FeetPosSolver(rightFootPos, ref rightFootIKPos, ref rightFootIKRot);
        FeetPosSolver(leftFootPos, ref leftFootIKPos, ref leftFootIKRot);
        
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if(!enableFootIK) return;
        
        MovePelvisHeight();
        
        // Right Foot IK pos and rot -- Utilize the pro features 
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        if (useProIK) animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rightFootAnimVarName));
        
        MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPos, rightFootIKRot, ref lastRightFootPosY);
        
        // Left Foot IK pos and rot -- Utilize the pro features 
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        if (useProIK) animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(leftFootAnimVarName));
        
        MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPos, leftFootIKRot, ref lastLeftFootPosY);
    }       

    void MoveFeetToIKPoint(AvatarIKGoal foot, Vector3 posIKHolder, Quaternion rotIKHolder, ref float lastFootPosY)
    {
        Vector3 targetIKPos = animator.GetIKPosition(foot);

        if (posIKHolder != Vector3.zero)
        {
            targetIKPos = transform.InverseTransformPoint(targetIKPos);
            posIKHolder = transform.InverseTransformPoint(posIKHolder);

            float y = Mathf.Lerp(lastFootPosY, posIKHolder.y, feetToIKPosSpeed);
            targetIKPos.y += y;

            lastFootPosY = y;
            targetIKPos = transform.TransformPoint(targetIKPos);
            animator.SetIKRotation(foot, rotIKHolder);
        }
        
        animator.SetIKPosition(foot, targetIKPos);
    }

    void MovePelvisHeight()
    {
        if (rightFootIKPos == Vector3.zero || leftFootIKPos == Vector3.zero || lastPelvisPosY == 0)
        {
            lastPelvisPosY = animator.bodyPosition.y;
            return;
        }

        float leftOffsetPos = leftFootIKPos.y - transform.position.y;
        float rightOffsetPos = rightFootIKPos.y - transform.position.y;

        float totalOffset = (leftOffsetPos < rightOffsetPos) ? leftOffsetPos : rightOffsetPos;

        Vector3 newPelvisPos = animator.bodyPosition + Vector3.up * totalOffset;
        newPelvisPos.y = Mathf.Lerp(lastPelvisPosY, newPelvisPos.y, pelvisUpDownSpeed);

        animator.bodyPosition = newPelvisPos;
        lastPelvisPosY = animator.bodyPosition.y;
    }

    void FeetPosSolver(Vector3 fromSkyPos, ref Vector3 feetIKPos, ref Quaternion feetIKRot)
    {
        // Raycast handler
        RaycastHit hit;
        if(showDebugSolver) Debug.DrawLine(fromSkyPos, fromSkyPos + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.cyan);

        if (Physics.Raycast(fromSkyPos, Vector3.down, out hit, raycastDownDistance + heightFromGroundRaycast,
            environmentLayerMask))
        {
            // Find feet IK positions - using the skyPos
            feetIKPos = fromSkyPos;
            feetIKPos.y = hit.point.y + pelvisOffset;
            feetIKRot = quaternion.LookRotation(Vector3.up, hit.normal) * transform.rotation;
            return;
        }
        
        feetIKPos = Vector3.zero;
    }

    void AdjustFeetTarget(ref Vector3 feetPos, HumanBodyBones foot)
    {
        feetPos = animator.GetBoneTransform(foot).position;
        feetPos.y = transform.position.y + heightFromGroundRaycast;
    }
}
