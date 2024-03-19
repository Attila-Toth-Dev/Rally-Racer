using NaughtyAttributes;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

#region Structs & Enums

[Serializable]
public enum Drivetrain
{
    Awd,
    Rwd,
    Fwd
}

[Serializable]
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

[Serializable]
public struct PowerDistribution
{
    [Range(0, 1)] public float frontPower;
    [Range(0, 1)] public float rearPower;
}

#endregion

public class CarController : MonoBehaviour
{
    [Header("Controller Objects")]
    public Rigidbody carRb;
    public List<Wheel> wheels;

    [Header("Engine Settings")]
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField, Range(700, 1000)] private float idleRpm;
    [SerializeField, ReadOnly] private float engineRpm;
    [SerializeField, ReadOnly] private float engineHorsePower;
    
    [Header("Drivetrain Settings")]
    [SerializeField] private Drivetrain vehicleDrivetrain;
    [SerializeField, ShowIf("vehicleDrivetrain", Drivetrain.Awd)] private PowerDistribution powerDistribution;

    [Header("Transmission Settings")]
    [SerializeField] private float[] gearRatios;
    [SerializeField, ReadOnly] private int gearIndex = 0;

    [Header("Handling Settings")]
    [SerializeField] private AnimationCurve steerCurve;
    [SerializeField] private float turnSensitivity;
    [SerializeField] private float brakePower;

    // Debugging
    [SerializeField, ReadOnly, Foldout("Debugging")] private float currentSpeed;
    [SerializeField, ReadOnly, Foldout("Debugging")] private float wheelsRpm;
    [SerializeField, ReadOnly, Foldout("Debugging")] private float accelInputFloat;
    [SerializeField, ReadOnly, Foldout("Debugging")] private float brakeInputFloat;
    [SerializeField, ReadOnly, Foldout("Debugging")] private float steerInputFloat;
    [SerializeField, ReadOnly, Foldout("Debugging")] private float handBrakeInput;
    
    // Inputs
    [SerializeField, Foldout("Inputs")] private InputActionReference accelAction;
    [SerializeField, Foldout("Inputs")] private InputActionReference steerAction;
    [SerializeField, Foldout("Inputs")] private InputActionReference handBrakeAction;
    [SerializeField, Foldout("Inputs")] private InputActionReference shiftUp;
    [SerializeField, Foldout("Inputs")] private InputActionReference shiftDown;
    
    private Vector3 centerOfMass;

    private void Awake()
    {
        shiftUp.action.performed += ShiftUp;
        shiftDown.action.performed += ShiftDown;
    }

    private void Start()
    {
        carRb.centerOfMass = centerOfMass;
        gearIndex = 1;
    }

    private void Update()
    {
        // Update Inputs
        GetInputs();

        // Engine Calculations
        CalculateEngineSpeed();

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

    // Shift transmission up a gear
    private void ShiftUp(InputAction.CallbackContext _context)
    {
        gearIndex++;

        if(gearIndex == gearRatios.Length)
            gearIndex = gearRatios.Length - 1;
    }

    // Shift transmission down a gear
    private void ShiftDown(InputAction.CallbackContext _context)
    {
        gearIndex--;

        if(gearIndex < gearRatios.Length - gearRatios.Length)
            gearIndex = gearRatios.Length - gearRatios.Length;
    }

    // Add calculated wheel rpm to get motor torque to drive vehicle
    private void CalculateEngineSpeed()
    {
        float rpm = WheelRpm();

        engineRpm = idleRpm + (rpm * gearRatios[gearIndex]);
        engineHorsePower = torqueCurve.Evaluate(engineRpm) * gearRatios[gearIndex] * accelInputFloat;
    }

    // Calculate the wheel rpm when driving
    private float WheelRpm()
    {
        float sum = 0;
        foreach (Wheel wheel in wheels)
        {
            if (vehicleDrivetrain == Drivetrain.Awd)
                sum += wheel.wheelCollider.rpm;

            if (vehicleDrivetrain == Drivetrain.Fwd)
                if (wheel.axleOfWheel == Axle.Front)
                    sum += wheel.wheelCollider.rpm * 0.5f;

            if (vehicleDrivetrain == Drivetrain.Rwd)
                if (wheel.axleOfWheel == Axle.Rear)
                    sum += wheel.wheelCollider.rpm * 0.5f;
        }

        return wheelsRpm = sum;
    }

    // Move the car at necessary speed with set drivetrain
    private void Move()
    {
        foreach (Wheel wheel in wheels)
        {
            // Movement for AWD Cars
            if(vehicleDrivetrain == Drivetrain.Awd)
            {
                if(wheel.axleOfWheel == Axle.Front)
                    wheel.wheelCollider.motorTorque = engineHorsePower * powerDistribution.frontPower;

                if (wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.motorTorque = engineHorsePower * powerDistribution.rearPower;
            }

            // Movement for RWD Cars
            if (vehicleDrivetrain == Drivetrain.Rwd)
                if(wheel.axleOfWheel == Axle.Rear)
                    wheel.wheelCollider.motorTorque = engineHorsePower * 0.5f;

            // Movement for FWD Cars
            if(vehicleDrivetrain == Drivetrain.Fwd)
                if(wheel.axleOfWheel == Axle.Front)
                    wheel.wheelCollider.motorTorque = engineHorsePower * 0.5f;
        }
    }

    // Steer the wheels on front Axle
    private void Steer()
    {
        foreach (Wheel wheel in wheels)
        {
            if(wheel.axleOfWheel == Axle.Front)
            {
                float steerAngle = Mathf.Lerp(0, steerCurve.Evaluate(currentSpeed), turnSensitivity);
                wheel.wheelCollider.steerAngle = steerAngle * steerInputFloat;
            }
        }
    }

    // Apply a heavy brake force onto wheels on rear axle
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
    
    // Apply necessary brake pressure onto front and rear axles
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
        if(accelInputFloat < 0)
        {
            brakeInputFloat = Mathf.Abs(accelInputFloat);
            accelInputFloat = 0;
        }
        else
            brakeInputFloat = 0;
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
