using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using KinematicCharacterController.Examples;

public class InputHandler : MonoBehaviour
{
    public ExampleCharacterCamera OrbitCamera;
    public Transform cameraFollowPoint;
    public CharacterController character;

    private Vector3 lookInputVector;
    
    // Start is called before the first frame update
    void Start()
    {
        // Cursor Settings
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Tell camera to follow transform
        OrbitCamera.SetFollowTransform(cameraFollowPoint);

        // Ignore the character's collider(s) for camera obstruction checks
        //OrbitCamera.IgnoredColliders = character.GetComponentsInChildren<Collider>().ToList();
    }

    // Update is called once per frame
    void Update()
    {
        HandleCharacterInput();
        
    }

    private void LateUpdate()
    {
        HandleCameraInput();
    }

    private void HandleCharacterInput()
    {
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

        // Build the CharacterInputs struct
        characterInputs.MoveAxisForward = Input.GetAxisRaw("Vertical");
        characterInputs.MoveAxisRight = Input.GetAxisRaw("Horizontal");
        characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
        characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
        characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
        characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);
        characterInputs.ChargingDown = Input.GetKeyDown(KeyCode.Q);

        // Apply inputs to character
        character.SetInputs(ref characterInputs);
    }

    private void HandleCameraInput()
    {
        // Create the look input vector for the camera
        float mouseLookAxisUp = Input.GetAxisRaw("Mouse Y");
        float mouseLookAxisRight = Input.GetAxisRaw("Mouse X");
        lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

        float scrollInput = -Input.GetAxis("Mouse ScrollWheel");
        //scrollInput = 0f;

        // Apply inputs to the camera
        OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

        // Handle toggling zoom level
        if (Input.GetMouseButtonDown(1))
        {
            OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
        }
    }
}
