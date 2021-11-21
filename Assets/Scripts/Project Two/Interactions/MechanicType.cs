using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterController = ProjectTwo.CharacterController;

public class MechanicType : MonoBehaviour, IInteract
{
    public enum EMechanicType
    {
        PushPull,
    }

    public EMechanicType mechanicType;
    public void OnInteract(CharacterController characterController, GameObject interactObject)
    {
    }
}
