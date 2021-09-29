using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KinematicCharacterController.Examples;

namespace ProjectTwo
{
    public class InputHandler : MonoBehaviour
    {
        public ExampleCharacterCamera OrbitCamera;
        public Transform cameraFollowPoint;
        public CharacterController character;

        private bool inputsEnabled = true;

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
            OrbitCamera.IgnoredColliders = character.GetComponentsInChildren<Collider>().ToList();
        }

        // Update is called once per frame
        void Update()
        {
            HandleCharacterInput();
        }
            
        private void LateUpdate() => HandleCameraInput();

        private void HandleCharacterInput()
        {
            if (inputsEnabled)
            {
                PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

                // Build the CharacterInputs struct
                characterInputs.MoveAxisForward = Input.GetAxisRaw("Vertical");
                characterInputs.MoveAxisRight = Input.GetAxisRaw("Horizontal");
                characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
                // Jump
                characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
                characterInputs.JumpHeld = Input.GetKey(KeyCode.Space);
                // Crouch
                characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
                characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);
                characterInputs.CrouchHeld = Input.GetKey(KeyCode.C);
                // Charge
                characterInputs.ChargingDown = Input.GetKeyDown(KeyCode.Q);
                // NoClip
                characterInputs.NoClipDown = Input.GetKeyUp(KeyCode.N);
                // Interact
                characterInputs.InteractDown = Input.GetKeyDown(KeyCode.E);
                // Sprint
                characterInputs.SprintDown = Input.GetKey(KeyCode.LeftShift);
                // Dodge
                characterInputs.DodgeDown = Input.GetKeyDown(KeyCode.F);

                // Apply inputs to character
                character.SetInputs(ref characterInputs);
            }
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
        
        public void SetInputStatus(bool allow) => inputsEnabled = allow;
    }
}