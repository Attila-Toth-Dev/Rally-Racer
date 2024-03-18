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
    [SerializeField, ReadOnly] private float engineHorsePower;
    [SerializeField, ReadOnly] private float engineTorque;
    
    [Header("Drivetrain Settings")]
    [SerializeField] private Drivetrain vehicleDrivetrain;
    [SerializeField, ShowIf("vehicleDrivetrain", Drivetrain.Awd)] private PowerDistribution powerDistribution;

    [Header("Transmission Settings")]
    //[SerializeField] private AnimationCurve gearRatios;
    [SerializeField] private float[] gearRatios;
    [SerializeField] private float finalDriveRatio;
    [SerializeField, ReadOnly] private int gearIndex = 0;

    [Header("Handling Settings")]
    [SerializeField] private AnimationCurve steerCurve;
    [SerializeField] private float turnSensitivity;
    [SerializeField] private float brakePower;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference accelAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference handBrakeAction;
    [SerializeField] private InputActionReference shiftUp;
    [SerializeField] private InputActionReference shiftDown;

    [Header("Debugging Tools")]
    [ReadOnly] public float currentSpeed;
    
    [Header("Input Debugging")]
    [SerializeField, ReadOnly] private float accelInputFloat;
    [SerializeField, ReadOnly] private float brakeInputFloat;
    [SerializeField, ReadOnly] private float steerInputFloat;
    [SerializeField, ReadOnly] private float handBrakeInput;

    private float wheelsRpm;

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
        gearIndex = 1;
    }

    private void Update()
    {
        // Update Inputs
        GetInputs();
        
        // Animations
        AnimateWheels();
     
        // Engine Calculations
        CalculateEngineSpeed();
        
        // Transmission
        GearBox();
        
        // Get Current Vehicle Speed
        currentSpeed = carRb.velocity.magnitude * 3.6f;
    }

    private void GearBox()
    {
        
        
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

        if(gearIndex >= gearRatios.Length)
            gearIndex = gearRatios.Length;
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
        // Calculate Wheel Speeds
        WheelRpm();
        
        engineRpm = idleRpm + (Mathf.Abs(wheelsRpm) * finalDriveRatio * gearRatios[gearIndex]) / finalDriveRatio;
        engineHorsePower = torqueCurve.Evaluate(engineRpm) * gearRatios[gearIndex] * accelInputFloat;
        engineTorque = (engineHorsePower * 5252 / engineRpm) * 1.3558f;
    }

    // Calculate the wheel rpm when driving
    private void WheelRpm()
    {
        float sum = 0;
        for(int i = 0; i < wheels.Count; i++)
        {
            if(vehicleDrivetrain == Drivetrain.Awd)
                sum += wheels[i].wheelCollider.rpm;
            
            if(vehicleDrivetrain is Drivetrain.Rwd or Drivetrain.Fwd)
                sum += wheels[i].wheelCollider.rpm * 0.5f;
        }

        wheelsRpm = sum;
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
        // MAKE INTO COROUTINE

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
