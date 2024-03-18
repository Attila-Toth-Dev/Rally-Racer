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
    [FormerlySerializedAs("motorTorque"),SerializeField, ReadOnly] private float totalMotorTorque;
    
    [Header("Drivetrain Settings")]
    [SerializeField] private Drivetrain vehicleDrivetrain;
    [SerializeField, ShowIf("vehicleDrivetrain", Drivetrain.Awd)] private PowerDistribution powerDistribution;
    
    [Header("Transmission Settings")]
    [SerializeField] private AnimationCurve gearRatios;
    [SerializeField] private float finalDriveRatio;
    [SerializeField] private int minGears;
    [SerializeField] private int maxGears;
    [SerializeField, ReadOnly] private int gearIndex = 0;
    
    [Header("Handling Settings")]
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private float brakePower;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference accelAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference handBrakeAction;
    [SerializeField] private InputActionReference shiftUp;
    [SerializeField] private InputActionReference shiftDown;

    [Header("Debugging Tools")]
    [ReadOnly] public float currentSpeed;
    [SerializeField, ReadOnly] private float wheelSpeed;
    
    [Header("Input Debugging")]
    [SerializeField, ReadOnly] private float accelInputFloat;
    [SerializeField, ReadOnly] private float brakeInputFloat;
    [SerializeField, ReadOnly] private float steerInputFloat;
    [SerializeField, ReadOnly] private float handBrakeInput;

    private float wheelRpm;

    private float slipAngle;
    private Vector3 centerOfMass;

    private void Awake()
    {
        shiftUp.action.performed += ShiftUp;
        shiftDown.action.performed += ShiftDown;
    }

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
     
        // Engine Calculations
        CalculateEngineSpeed();
        
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

    private void ShiftUp(InputAction.CallbackContext _context)
    {
        gearIndex++;

        if(gearIndex > maxGears)
            gearIndex = maxGears;
    }

    private void ShiftDown(InputAction.CallbackContext _context)
    {
        gearIndex--;

        if(gearIndex < minGears)
            gearIndex = minGears;
    }
    
    private void CalculateEngineSpeed()
    {
        // Calculate Wheel Speeds
        WheelRpm();

        engineRpm = idleRpm + (wheelRpm * finalDriveRatio * gearRatios.Evaluate(gearIndex)) * finalDriveRatio / 100;
        totalMotorTorque = torqueCurve.Evaluate(engineRpm) * gearRatios.Evaluate(gearIndex) * finalDriveRatio * accelInputFloat;
    }

    private void WheelRpm()
    {
        float sum = 0;
        
        for(int i = 0; i < wheels.Count; i++)
            sum += wheels[i].wheelCollider.rpm;

        wheelRpm = sum;
    }

    private void Move()
    {
        foreach (Wheel wheel in wheels)
        {
            // Movement for AWD Cars
            if(vehicleDrivetrain == Drivetrain.Awd)
            {
                if(wheel.axleOfWheel == Axle.Front)
                    wheel.wheelCollider.motorTorque = (totalMotorTorque / 4);
                
                if(wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.motorTorque = (totalMotorTorque / 4);
            }
            
            // Movement for RWD Cars
            if(wheel.axleOfWheel == Axle.Rear && vehicleDrivetrain == Drivetrain.Rwd)
                wheel.wheelCollider.motorTorque = (totalMotorTorque / 2);

            // Movement for FWD Cars
            if(wheel.axleOfWheel == Axle.Front && vehicleDrivetrain == Drivetrain.Fwd)
                wheel.wheelCollider.motorTorque = (totalMotorTorque / 2);
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
                wheel.wheelCollider.brakeTorque = brakeInputFloat * brakePower * carRb.drag * 1.5f;

            if(wheel.axleOfWheel == Axle.Rear)
                wheel.wheelCollider.brakeTorque = brakeInputFloat * brakePower * carRb.drag * 0.3f;
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
                                                                                                                
        // Gets the input of the braking motion from
        // from the acceleration input. Then allows the player
        // to reverse
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
    }   

    private void OnEnable()
    {
        accelAction.action.Enable();
        steerAction.action.Enable();
        handBrakeAction.action.Enable();
        
        shiftUp.action.Enable();
        shiftDown.action.Enable();
    }

    private void OnDisable()
    {
        accelAction.action.Disable();
        steerAction.action.Disable();
        handBrakeAction.action.Disable();
        
        shiftUp.action.Disable();
        shiftDown.action.Disable();
    }
}
