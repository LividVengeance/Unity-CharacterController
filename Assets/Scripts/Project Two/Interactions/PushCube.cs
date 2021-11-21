using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushCube : MonoBehaviour, IInteract
{
    private ProjectTwo.CharacterController _characterController;
    
    public void OnInteract(ProjectTwo.CharacterController characterController)
    {
        _characterController = characterController;
        _characterController.SetMoveToPoint(Vector3.one, 3f, 5f, 1.5f, 
            true, true);
    }
}
