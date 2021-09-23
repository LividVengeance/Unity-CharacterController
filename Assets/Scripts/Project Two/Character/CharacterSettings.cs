using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterSettings : MonoBehaviour
{
    // State Strings (Used form bitwise comparison)
    private const string jumpStr = "Jump";
    private const string sprintStr = "Sprint";
    private const string chargeStr = "Charge";
    private const string swimmingStr = "Swimming";
    private const string climbingStr = "Climbing";
    private const string noClipStr = "NoClip";

    [Title("Enabled Abilities")]
    public AbilityBitmaskEnum enabledAbilities;

    [System.Flags]
    public enum AbilityBitmaskEnum
    {
        Jump = 1 << 1,
        Sprint = 1 << 2,
        Charge = 1 << 3,
        Swimming = 1 << 4,
        Climbing = 1 << 5,
        NoClip = 1 << 6,
        All = Jump | Sprint | Charge | Swimming | Climbing | NoClip,
    }
    
    [Header("Stable Movement")] 
    [Tooltip("Maximum speed the character can run on stable ground")]
    public float maxStableMoveSpeed = 10f;
    [Tooltip("How quickly the character will come to a stop after no movement input")]
    public float stableMovementSharpness = 15;
    [Tooltip("How quickly the character will rotate")]
    public float orientationSharpness = 10;

    [Header("Air Movement")] 
    [Tooltip("Maximum speed the character can move when in the air")]
    public float maxAirMoveSpeed = 10f;
    [Tooltip("How quickly the character will accelerate when in the air")]
    public float airAccelerationSpeed = 5f;
    [Tooltip("The amount of drag force the character will experience when in the air")]
    public float drag = 0.1f;
    
    [Header("Jump")]
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Will allow the character to jump when sliding down a sharp incline")]
    public bool allowJumpingWhenSliding = false;
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Will allow the character to preform a second jump when in the air")] public bool allowDoubleJump = false;
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Will allow the character to jump off a wall")] public bool allowWallJump = false;
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How quickly the character will move upward. This will dictate jump height")] public float jumpSpeed = 10f;
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Time before landing where jump input will still allow jump once you land")]
    public float jumpPreGroundingGraceTime = 0f;
    [ShowIf("@enabledAbilities.ToString().Contains(jumpStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Time after leaving stable ground where jump will still be allowed")]
    public float jumpPostGroundingGraceTime = 0f;

    [Header("Sprint")]
    [ShowIf("@enabledAbilities.ToString().Contains(sprintStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Maximum run speed when in the sprint state")] public float maxSprintSpeed = 20f;
    [ShowIf("@enabledAbilities.ToString().Contains(sprintStr) || enabledAbilities == AbilityBitmaskEnum.All"),
     Tooltip("How quickly the character will come to a stop after no movement input when in the sprint state")]
    public float sprintMovementSharpness = 5;
    [ShowIf("@enabledAbilities.ToString().Contains(sprintStr) || enabledAbilities == AbilityBitmaskEnum.All"),
     Tooltip("How quickly the character will rotate when in the sprint state")]
    public float sprintOrientationSharpness = 2;
    [ShowIf("@enabledAbilities.ToString().Contains(sprintStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Allow jumping while in the sprinting state")] public bool allowJumpingWhileSprinting = true;
    
    [Header("Charging")] 
    [ShowIf("@enabledAbilities.ToString().Contains(chargeStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How fast the character will move in the charge state")] public float chargeSpeed = 15f;
    [ShowIf("@enabledAbilities.ToString().Contains(chargeStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Maximum time character can be in the charge state")] public float maxChargeTime = 1.5f;
    [ShowIf("@enabledAbilities.ToString().Contains(chargeStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Time the character will remain unable to move at end of charge state")] public float stoppedTime = 1f;

    [Header("Swimming")] 
    [ShowIf("@enabledAbilities.ToString().Contains(swimmingStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Point used to determine if character is in water")] public Transform swimmingReferencePoint;
    [ShowIf("@enabledAbilities.ToString().Contains(swimmingStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("Layer character will use to determine if in water")] public LayerMask waterLayer;
    [ShowIf("@enabledAbilities.ToString().Contains(swimmingStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How quickly the character can move when in water")] public float swimmingSpeed = 4f;
    [ShowIf("@enabledAbilities.ToString().Contains(swimmingStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How quickly the character will come to a stop after no movement input when in the swimming state")]
    public float swimmingMovementSharpness = 3;

    [Header("Ladder Climbing")] [SerializeField]
    public float climbingSpeed = 4f;
    public float anchoringDuration = 0.25f;
    public LayerMask interactionLayer;

    [Header("No Clip")] 
    [ShowIf("@enabledAbilities.ToString().Contains(noClipStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How fast the character will move in the no clip state")] public float noClipMoveSpeed = 10f;
    [ShowIf("@enabledAbilities.ToString().Contains(noClipStr) || enabledAbilities == AbilityBitmaskEnum.All"), 
     Tooltip("How quickly the character will come to a stop when in the NoClip state")] 
    public float noClipSharpness = 15;

    [Header("Misc")]
    [Tooltip("Always orient its up direction in the opposite direction of the gravity")]
    public bool orientTowardsGravity;
    public Vector3 gravity = new Vector3(0, -30f, 0);
    public Transform meshRoot;
    [Tooltip("Ignore physics collision on these layers")]
    public List<string> ignoredLayers = new List<string>();

    [Header("Animations")] 
    [Tooltip("Play Spawn in animation. Note this will disable character inputs still animation has finished")]
    public bool playSpawnAnimation = true;

    public bool AbilityEnabled(string abilityName)
    {
  return enabledAbilities.ToString().Contains(abilityName) || enabledAbilities == AbilityBitmaskEnum.All;
    }
}