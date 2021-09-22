using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public class MovingPlatform : MonoBehaviour, IMoverController
{
    public PhysicsMover mover;
    
    // Start is called before the first frame update
    void Start()
    {
        //mover.MoverController = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// Used to tell what the characters position and rotations should be
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        goalPosition = Vector3.zero;
        goalRotation = Quaternion.identity;
    }
}
