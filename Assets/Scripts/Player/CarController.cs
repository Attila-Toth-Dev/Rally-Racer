using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Axle
{
    Front,
    Rear
}

[Serializable]
public struct Wheel
{
    public GameObject wheelGameObject;
    public WheelCollider wheelCollider;
    public Axle axleOfWheel;
}

public class CarController : MonoBehaviour
{
    [Header("Car Controller Objects")]
    public Rigidbody carRb;
    public List<Wheel> wheels;

    [Header("Car Settings")]
    [SerializeField] float topSpeed;
    [SerializeField] float accelAmount;
    [SerializeField] float brakeAmount;

    [Header("Car Control")]
    [SerializeField] float turnSensitivity;
    [SerializeField] float maxSteerAngle;
    [SerializeField] Vector3 centerOfMass;

    [Header("Turbo Settings")] 
    [SerializeField] float turboPower;
    [SerializeField] float turboDuration;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask layerMask;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference driftAction;
    [SerializeField] private InputActionReference turboAction;

    [Header("Debugging Tools")]
    [SerializeField] private float rayLength = 1f;
    [SerializeField, ReadOnly] private float moveFloat;
    [SerializeField, ReadOnly] private float steerFloat;
    [SerializeField, ReadOnly] private bool isGrounded;
    
    private float speed; 
    private float rotate;

    private void Start()
    {
        carRb.centerOfMass = centerOfMass;
    }

    private void Update()
    {
        // Update Inputs
        GetInputs();

        AnimateWheels();
    }

    private void LateUpdate()
    {
        // Movement
        Move();
        Steer();
    }

    private void GroundDetection()
    {
        // Ground Detection
        if (Physics.Raycast(transform.position + -transform.up, Vector3.down, out RaycastHit hitNear, rayLength, layerMask))
        {
            // Normal Rotation
            //carNormal.up = Vector3.Lerp(carNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
            //carNormal.Rotate(0, transform.eulerAngles.y, 0);
            isGrounded = true;
        }
        else
        {
            //carNormal.Rotate(normalPosition.x, normalPosition.y, normalPosition.z);
            isGrounded = false;
        }
    }

    private void Move()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = moveFloat * topSpeed * accelAmount * Time.deltaTime;
        }
    }

    private void Steer()
    {
        foreach (Wheel wheel in wheels)
        {
            if(wheel.axleOfWheel == Axle.Front)
            {
                float steerangle = steerFloat * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerangle, 0.6f);
            }
        }
    }

    private void AnimateWheels()
    {
        foreach (Wheel wheel in wheels)
        {
            Quaternion rot;
            Vector3 pos;

            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelGameObject.transform.position = pos;
            wheel.wheelGameObject.transform.rotation = rot;
        }
    }

    private void GetInputs()
    {
        // Read Player Input values
        moveFloat = moveAction.action.ReadValue<float>();
        steerFloat = steerAction.action.ReadValue<float>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        steerAction.action.Enable();
        driftAction.action.Enable();
        turboAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        steerAction.action.Disable();
        driftAction.action.Disable();
        turboAction.action.Disable();
    }
}
