using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Rigidbody rb;

    [Header("Gameplay")] 
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Range(0, 100)] private float currentHealth;
    
    [Header("Animation")] 
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 2.0f;

    private int isWalkingHash;
    private int isRunningHash;
    private int velocityHash;
    private int isJumpingHash;
    private int isPunchingHash;

    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 0.1f;
    private float velocityX = 0f;
    private float velocityZ = 0f;

    // Aim Down Sights
    [SerializeField] private float adsSpeed = 1f;
    private float currentAdsTime = 0f;
    
    // Crouching
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private float crouchSpeed = 1f;
    private float currentCrouchTime = 0f;
    
    // Animation Hashing
    private int velocityXHash;
    private int velocityZHash;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Animation hashing
        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");
        isJumpingHash = Animator.StringToHash("isJumping");
        isPunchingHash = Animator.StringToHash("isPunching");

        currentHealth = maxHealth;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Gets player inputs
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool backwardPressed = Input.GetKey(KeyCode.S);
        bool leftPressed = Input.GetKey(KeyCode.A);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool runPressed = Input.GetKey(KeyCode.LeftShift);

        // Get current max velocity, if the sprint is being pressed
        float currentMaxVelocity = runPressed ? maxRunVelocity : maxWalkVelocity;

        ChangeVelocity(forwardPressed, backwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);
        LockOrResetVelocity(forwardPressed, backwardPressed , leftPressed, rightPressed, runPressed, currentMaxVelocity);
        
        AimDownSights();
        Crouch();
        Jump();
        if (Input.GetMouseButton(0)) StartCoroutine(TempPunchTime());
        
        // Adjusts animation for health
        animator.SetLayerWeight(animator.GetLayerIndex("Injured"), 1 - (currentHealth/maxHealth));

        // Pass velocities to animation controller
        animator.SetFloat(velocityZHash, velocityZ);
        animator.SetFloat(velocityXHash, velocityX);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space)) StartCoroutine(TempJumpTIme());
    }

    IEnumerator TempJumpTIme()
    {
        animator.SetBool(isJumpingHash, true);
        yield return (new WaitForSeconds(2f));
        animator.SetBool(isJumpingHash, false);
    }

    IEnumerator TempPunchTime()
    {
        animator.SetBool(isPunchingHash, true);
        yield return (new WaitForSeconds(1.5f));
        animator.SetBool(isPunchingHash, false);
    }
    
    private void Crouch()
    {
        if (Input.GetKeyDown(KeyCode.C)) isCrouching = !isCrouching;
        
        // Is crouching
        if (isCrouching)
        {
            if (currentCrouchTime < 1f) currentCrouchTime += (Time.deltaTime * crouchSpeed);
            else if (currentCrouchTime > 1f) currentCrouchTime = 1f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), currentCrouchTime);
        }
        // Is NOT crouching
        else
        {
            if (currentCrouchTime > 0f) currentCrouchTime -= (Time.deltaTime * crouchSpeed);
            else if (currentCrouchTime > 1f) currentCrouchTime = 0f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), currentCrouchTime);
        }
    }

    private void AimDownSights()
    {
        // Is aiming down sights
        if (Input.GetMouseButton(1))
        {
            if (currentAdsTime < 1f) currentAdsTime += (Time.deltaTime * adsSpeed);
            else if (currentAdsTime > 1f) currentAdsTime = 1f; 
            
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), currentAdsTime);
        }
        // Is NOT aiming down sights
        else
        {
            if (currentAdsTime > 0f) currentAdsTime -= (Time.deltaTime * adsSpeed);
            else if (currentAdsTime < 0f) currentAdsTime = 0f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), currentAdsTime);
        }
    }

    private void ChangeVelocity(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity)
    {
        // If press forward, increase in velocity Z
        if (forwardPressed && velocityZ < currentMaxVelocity) velocityZ += Time.deltaTime * acceleration;
        // If NOT press forward, decrease in velocity Z
        if (!forwardPressed && velocityZ > 0f) velocityZ -= Time.deltaTime * deceleration;
        
        // If press forward, decrease in velocity Z
        if (backwardPressed && velocityZ > -currentMaxVelocity) velocityZ -= Time.deltaTime * acceleration;
        // If NOT press forward, increase velocity Z
        if (!backwardPressed && velocityZ < 0f) velocityZ += Time.deltaTime * deceleration;

        // If press left, decrease in velocity X
        if (leftPressed && velocityX > -currentMaxVelocity) velocityX -= Time.deltaTime * acceleration; 
        // If NOT press left, increase velocity X
        if (!leftPressed && velocityX < 0f) velocityX += Time.deltaTime * deceleration; 
        
        // If press right, increase in velocity X
        if (rightPressed && velocityX < currentMaxVelocity) velocityX += Time.deltaTime * acceleration;
        // If NOT press right, decrease velocity X 
        if (!rightPressed && velocityX > 0f) velocityX -= Time.deltaTime * deceleration; 
    }

    private void LockOrResetVelocity(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity)
    {
        // Rest velocity Z
        if (!forwardPressed && velocityZ < 0f) velocityZ = 0f;
        // Reset velocity X
        if (!leftPressed && !rightPressed && velocityX != 0f && (velocityX > -0.05f && velocityX < 0.05f)) velocityX = 0f;
                
        // Clamping forward velocity
        if (forwardPressed && runPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ = currentMaxVelocity;
        }
        // Decelerate to the mac walk velocity
        else if (forwardPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within the offset
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + 0.05f))
            {
                velocityZ = currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within the offset
        else if (forwardPressed && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - 0.05f))
        {
            velocityZ = currentMaxVelocity;
        }

        if (backwardPressed && runPressed && velocityZ < -currentMaxVelocity)
        {
            velocityZ = -currentMaxVelocity;
        }
        else if (backwardPressed && velocityZ < -currentMaxVelocity)
        {
            velocityZ += Time.deltaTime * deceleration;
            if (velocityZ < -currentMaxVelocity && velocityZ > (-currentMaxVelocity + 0.05))
            {
                velocityZ = -currentMaxVelocity;
            }
        }
        else if (backwardPressed && velocityZ > -currentMaxVelocity && velocityZ < (currentMaxVelocity - 0.05))
        {
            velocityZ = -currentMaxVelocity;
        }

        
        // Clamp Left
        if (leftPressed && runPressed && velocityX < -currentMaxVelocity)
        {
            velocityX = -currentMaxVelocity;
        }
        // Decelerate to max walk velocity
        else if (leftPressed && velocityX < -currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within offset
            if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity + 0.05f))
            {
                velocityX = -currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within offset
        else if (leftPressed && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity - 0.05f))
        {
            velocityX -= -currentMaxVelocity;
        }
        
        // Clamp right
        if (rightPressed && runPressed && velocityX > currentMaxVelocity)
        {
            velocityX = currentMaxVelocity;
        }
        // Decelerate to max walk velocity
        else if (leftPressed && velocityX > currentMaxVelocity)
        {
            velocityX += Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within offset
            if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + 0.05f))
            {
                velocityX = currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within offset
        else if (rightPressed && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - 0.05f))
        {
            velocityX = currentMaxVelocity;
        }
    }
}