using NaughtyAttributes;
using System;
using System.Collections.Generic;
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
    [SerializeField] private float topSpeed;
    [SerializeField] private float accelAmount;
    [SerializeField] private float brakeAmount;

    [Header("Car Control")]
    [SerializeField] private float turnSensitivity;
    [SerializeField] private float maxSteerAngle;
    [SerializeField] private Vector3 centerOfMass;

    [Header("Turbo Settings")] 
    [SerializeField] private float turboPower;
    [SerializeField] private float turboDuration;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference driftAction;
    [SerializeField] private InputActionReference turboAction;

    [Header("Debugging Tools")]
    [SerializeField, ReadOnly] private float moveFloat;
    [SerializeField, ReadOnly] private float steerFloat;
    
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

        Brake();
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
                float steerAngle = steerFloat * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerAngle, 0.6f);
            }
        }
    }

    private void Brake()
    {
        if(moveFloat < 0)
        {
            foreach (Wheel wheel in wheels)
            {
                if(wheel.axleOfWheel == Axle.Front)
                    wheel.wheelCollider.brakeTorque = 500 * brakeAmount * Time.deltaTime;

                if (wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.brakeTorque = 300 * brakeAmount * Time.deltaTime;
            }
        }
        else
        {
            foreach (Wheel wheel in wheels)
                wheel.wheelCollider.brakeTorque = 0;
        }
    }

    private void AnimateWheels()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
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
