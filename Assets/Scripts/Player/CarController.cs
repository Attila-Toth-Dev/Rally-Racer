using NaughtyAttributes;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[Serializable]
public enum Drivetrain
{
    Awd,
    Rwd,
    Fwd
}

[Serializable]
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

[Serializable]
internal struct PowerDistribution
{
    [Range(0, 1)] public float frontPower;
    [Range(0, 1)] public float rearPower;
}

public class CarController : MonoBehaviour
{
    [Header("Controller Objects")]
    public Rigidbody carRb;
    [SerializeField] private List<Wheel> wheels;

    [Header("Engine Settings")]
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField, Range(700, 1000)] private float idleRpm;
    [SerializeField, ReadOnly] private float engineRpm;
    [SerializeField, ReadOnly] private float motorTorque;
    
    [Header("Drivetrain Settings")]
    [SerializeField] private Drivetrain vehicleDrivetrain;
    [SerializeField, ShowIf("vehicleDrivetrain", Drivetrain.Awd)] private PowerDistribution powerDistribution;
    
    [Header("Steering Settings")]
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private float brakePower;

    [Header("Transmission Settings")]
    [SerializeField, ReadOnly] private int gearIndex;
    [SerializeField] private float[] gearRatios;
    [SerializeField] private float finalDriveRatio;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference accelAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference handBrakeAction;
    [SerializeField] private InputActionReference shiftAction;
    [SerializeField] private InputActionReference clutchAction;

    [Header("Debugging Tools")]
    [ReadOnly] public float currentSpeed;
    [SerializeField, ReadOnly] private float wheelSpeed;
    
    [Header("Input Debugging")]
    [SerializeField, ReadOnly] private float accelInputFloat;
    [SerializeField, ReadOnly] private float brakeInputFloat;
    [SerializeField, ReadOnly] private float steerInputFloat;
    [SerializeField, ReadOnly] private float handBrakeInput;
    [SerializeField, ReadOnly] private float shiftInputFloat;
    [SerializeField, ReadOnly] private float clutchInputFloat;

    private float wheelRpm;

    private float slipAngle;
    private Vector3 centerOfMass;
    
    private void Start()
    {
        carRb.centerOfMass = centerOfMass;
    }

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
        // Engine Calculations
        CalculateEngineSpeed();
        WheelRpm();
        
        // Movement
        Move();
        
        // Handling
        Steer();
        
        // Braking
        Brake();
        HandBrake();
    }

    private void CalculateEngineSpeed()
    {
        engineRpm = idleRpm + (wheelRpm * finalDriveRatio * gearRatios[gearIndex]) * finalDriveRatio * accelInputFloat;
        motorTorque = torqueCurve.Evaluate(engineRpm) * gearRatios[gearIndex] * finalDriveRatio * accelInputFloat;
    }

    private void WheelRpm()
    {
        float sum = 0;
        int r = 0;
        
        for(int i = 0; i < wheels.Count; i++)
        {
            sum += wheels[i].wheelCollider.rpm;
            r++;
        }

        wheelRpm = (r != 0) ? sum / r : 0;
    }

    private void Move()
    {
        foreach (Wheel wheel in wheels)
        {
            // Movement for AWD Cars
            if(vehicleDrivetrain == Drivetrain.Awd)
            {
                if(wheel.axleOfWheel == Axle.Front)
                    wheel.wheelCollider.motorTorque = (motorTorque / 4) * powerDistribution.frontPower;
                
                if(wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.motorTorque = (motorTorque / 4) * powerDistribution.rearPower;
            }
            
            // Movement for RWD Cars
            if(wheel.axleOfWheel == Axle.Rear && vehicleDrivetrain == Drivetrain.Rwd)
                wheel.wheelCollider.motorTorque = (motorTorque / 2);

            // Movement for FWD Cars
            if(wheel.axleOfWheel == Axle.Front && vehicleDrivetrain == Drivetrain.Fwd)
                wheel.wheelCollider.motorTorque = (motorTorque / 2);
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
                //steerAngle += Vector3.SignedAngle(transform.forward, carRb.velocity + transform.forward, Vector3.up);
                //steerAngle = Mathf.Clamp(steerAngle, -90f, 90f);
                
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
