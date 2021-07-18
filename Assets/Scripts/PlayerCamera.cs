using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float lookSensitivity;
    [SerializeField] private float minXLook;
    [SerializeField] private float maxXLook;
    [SerializeField] private Transform camAnchor;

    [SerializeField] private bool invertXRotation;
    private float currentXRotation;

    private void LateUpdate()
    {
        float xInput = Input.GetAxis("Mouse X");
        float yInput = Input.GetAxis("Mouse Y");

        // Horizontal look 
        transform.eulerAngles += Vector3.up * xInput * lookSensitivity;
        
        // Vertical Look
        if (invertXRotation) currentXRotation += yInput * lookSensitivity;
        else currentXRotation -= yInput * lookSensitivity;
        currentXRotation = Mathf.Clamp(currentXRotation, minXLook, maxXLook);

        Vector3 clampedAngle = camAnchor.eulerAngles;
        clampedAngle.x = currentXRotation;
        camAnchor.eulerAngles = clampedAngle;
    }
}
