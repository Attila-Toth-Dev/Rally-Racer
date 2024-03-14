using NaughtyAttributes;

using System;
using System.Collections.Generic;
using System.Numerics;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

internal enum Drivetrain
{
    Awd,
    Rwd,
    Fwd
}

internal enum Axle
{
    Front,
    Rear
}

[Serializable]
internal struct Wheel
{
    public GameObject wheelGameObject;
    public WheelCollider wheelCollider;
    public Axle axleOfWheel;
}

public class CarController : MonoBehaviour
{
    [Header("Controller Objects")]
    public Rigidbody carRb;
    [SerializeField] private List<Wheel> wheels;

    [Header("Engine Settings")]
    [SerializeField] private float enginePower = 350;

    [Header("Handling Settings")]
    [SerializeField] private Drivetrain vehicleDrivetrain;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private float brakePower;
    
    [Header("Transmission Settings")]

    [Header("Input Actions")]
    [SerializeField] private InputActionReference accelAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference handBrakeAction;
    [SerializeField] private InputActionReference shiftAction;
    [SerializeField] private InputActionReference clutchAction;

    [Header("Debugging Tools")]
    [ReadOnly] public float currentSpeed;
    [SerializeField, ReadOnly] private float accelInputFloat;
    [SerializeField, ReadOnly] private float brakeInputFloat;
    [SerializeField, ReadOnly] private float steerInputFloat;
    [SerializeField, ReadOnly] private float handBrakeInput;
    [SerializeField, ReadOnly] private float shiftInputFloat;
    [SerializeField, ReadOnly] private float clutchInputFloat;

    private float slipAngle;
    private Vector3 centerOfMass;
    
    private void Start() => carRb.centerOfMass = centerOfMass;

    private void Update()
    {
        // Update Inputs
        GetInputs();
        
        // Animations
        AnimateWheels();
        
        // Get Current Vehicle Speed
        currentSpeed = carRb.velocity.magnitude * 3.6f;
    }

    private void FixedUpdate()
    {
        // Movement
        Move();
        
        // Handling
        Steer();
        
        // Braking
        Brake();
        HandBrake();
    }

    private void Move()
    {
        foreach (Wheel wheel in wheels)
        {
            if(vehicleDrivetrain == Drivetrain.Awd)
                wheel.wheelCollider.motorTorque = accelInputFloat * (enginePower / 2);

            if(wheel.axleOfWheel == Axle.Rear && vehicleDrivetrain == Drivetrain.Rwd)
                wheel.wheelCollider.motorTorque = accelInputFloat * (enginePower);

            if(wheel.axleOfWheel == Axle.Front && vehicleDrivetrain == Drivetrain.Fwd)
                wheel.wheelCollider.motorTorque = accelInputFloat * (enginePower);
        }
    }

    private void Steer()
    {
        foreach (Wheel wheel in wheels)
        {
            if(wheel.axleOfWheel == Axle.Front)
            {
                float steerAngle = steerInputFloat * steeringCurve.Evaluate(currentSpeed);
                
                // Counter Steering
                steerAngle += Vector3.SignedAngle(transform.forward, carRb.velocity + transform.forward, Vector3.up);
                steerAngle = Mathf.Clamp(steerAngle, -90f, 90f);
                
                wheel.wheelCollider.steerAngle = steerAngle;
            }
        }
    }

    private void HandBrake()
    {
        if(handBrakeInput > 0)
        {
            foreach(Wheel wheel in wheels)
            {
                if(wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.brakeTorque = brakePower;
            }
        }
    }

    private void Brake()
    {
        foreach(Wheel wheel in wheels)
        {
            if(wheel.axleOfWheel == Axle.Front)
                wheel.wheelCollider.brakeTorque = brakeInputFloat * brakePower * 0.7f;

            if(wheel.axleOfWheel == Axle.Rear)
                wheel.wheelCollider.brakeTorque = brakeInputFloat * brakePower * 0.3f;
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
        // Gets the active input from the player keypress
        accelInputFloat = accelAction.action.ReadValue<float>();
        steerInputFloat = steerAction.action.ReadValue<float>();
        handBrakeInput = handBrakeAction.action.ReadValue<float>();
        shiftInputFloat = shiftAction.action.ReadValue<float>();
        clutchInputFloat = clutchAction.action.ReadValue<float>();
        
        // Gets the input of the braking motion from
        // from the acceleration input. Then allows the player
        // to reverse
        slipAngle = Vector3.Angle(transform.forward, carRb.velocity - transform.forward);
        if(slipAngle < 120f)
        {
            if(accelInputFloat < 0)
            {
                brakeInputFloat = Mathf.Abs(accelInputFloat);
                accelInputFloat = 0;
            }
            else
                brakeInputFloat = 0;
        }
        else
            brakeInputFloat = 0;
    }   

    private void OnEnable()
    {
        accelAction.action.Enable();
        steerAction.action.Enable();
        handBrakeAction.action.Enable();
        shiftAction.action.Enable();
        clutchAction.action.Enable();
    }

    private void OnDisable()
    {
        accelAction.action.Disable();
        steerAction.action.Disable();
        handBrakeAction.action.Disable();
        shiftAction.action.Disable();
        clutchAction.action.Disable();
    }
}
