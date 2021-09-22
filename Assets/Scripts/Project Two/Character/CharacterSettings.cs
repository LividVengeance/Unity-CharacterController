using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSettings : MonoBehaviour
{
    [Header("Stable Movement")] 
    [Tooltip("Maximum speed the character can run on stable ground")]
    public float maxStableMoveSpeed = 10f;
    [Tooltip("How quickly the character will come to a stop after no movement input")]
    public float stableMovementSharpness = 15;
    [Tooltip("How quickly the character will rotate")]
    public float orientationSharpness = 10;

    [Header("Sprint")]
    [Tooltip("Maximum run speed when in the sprint state")]
    public float maxSprintSpeed = 20f;
    [Tooltip("How quickly the character will come to a stop after no movement input " +
                             "when in the sprint state")]
    public float sprintMovementSharpness = 5;
    [Tooltip("How quickly the character will rotate when in the sprint state")]
    public float sprintOrientationSharpness = 2;
    [Tooltip("Allow jumping while in the sprinting state")]
    public bool allowJumpingWhileSprinting = true;

    [Header("Air Movement")] [Tooltip("Maximum speed the character can move when in the air")]
    public float maxAirMoveSpeed = 10f;
    [Tooltip("How quickly the character will accelerate when in the air")]
    public float airAccelerationSpeed = 5f;
    [Tooltip("The amount of drag force the character will experience when in the air")]
    public float drag = 0.1f;

    [Header("Jumping")]
    [Tooltip("Will allow the character to jump when sliding down a sharp incline")]
    public bool allowJumpingWhenSliding = false;
    [Tooltip("Will allow the character to preform a second jump when in the air")]
    public bool allowDoubleJump = false;
    [Tooltip("Will allow the character to jump off a wall")]
    public bool allowWallJump = false;
    [Tooltip("How quickly the character will move upward. This will dictate jump height")]
    public float jumpSpeed = 10f;
    [Tooltip("Time before landing where jump input will still allow jump once you land")]
    public float jumpPreGroundingGraceTime = 0f;
    [Tooltip("Time after leaving stable ground where jump will still be allowed")]
    public float jumpPostGroundingGraceTime = 0f;

    [Header("Charging")] 
    [Tooltip("How fast the character will move in the charge state")]
    public float chargeSpeed = 15f;
    [Tooltip("Maximum time character can be in the charge state")]
    public float maxChargeTime = 1.5f;
    [Tooltip("Time the character will remain unable to move at end of charge state")]
    public float stoppedTime = 1f;

    [Header("Swimming")] 
    [Tooltip("Point used to determine if character is in water")]
    public Transform swimmingReferencePoint;
    [Tooltip("Layer character will use to determine if in water")]
    public LayerMask waterLayer;
    [Tooltip("How quickly the character can move when in water")]
    public float swimmingSpeed = 4f;
    [Tooltip("How quickly the character will come to a stop after no movement input when " +
                             "in the swimming state")]
    public float swimmingMovementSharpness = 3;

    [Header("Ladder Climbing")] [SerializeField]
    public float climbingSpeed = 4f;
    public float anchoringDuration = 0.25f;
    public LayerMask interactionLayer;

    [Header("No Clip")] 
    [Tooltip("How fast the character will move in the no clip state")]
    public float noClipMoveSpeed = 10f;
    [Tooltip("How quickly the character will come to a stop when in the NoClip state")]
    public float noClipSharpness = 15;

    [Header("Misc")]
    [Tooltip("Always orient its up direction in the opposite direction of the gravity")]
    public bool orientTowardsGravity;
    public Vector3 gravity = new Vector3(0, -30f, 0);
    public Transform meshRoot;
    [Tooltip("Ignore physics collision on these layers")]
    public List<string> ignoredLayers = new List<string>();

    [Header("Animations")] 
    [Tooltip("Play Spawn in animation. Note this will disable character inputs " +
                             "still animation has finished")]
    public bool playSpawnAnimation = true;
}
