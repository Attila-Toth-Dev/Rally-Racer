using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarController : MonoBehaviour
    {
        [Header("Engine")]
        private float EngineSpeed = 1000f;
        
        [Header("Transmission")]
        
        [Header("Drivetrain")]
        public Drivetrain drivetrain;
        
        [Header("Wheels")] 
        public List<Wheel> wheels;

        [Header("Controls")] 
        [SerializeField] private InputActionReference movement;
        [SerializeField] private InputActionReference steer;
        [SerializeField] private InputActionReference handbrake;

        private float _movementValue;
        private float _steerValue;
        public bool isHandBraking;

        private void Update()
        {
            GetInputValues();
            AnimateWheels();
        }

        private void FixedUpdate()
        {
            Move();
            Steer();
            HandBrake();
        }

        private void Move()
        {
            foreach (Wheel wheel in wheels)
            {
                switch (drivetrain)
                {
                    case Drivetrain.Fwd:
                    {
                        if (wheel.axle == Axle.Front)
                            wheel.WheelCollider.motorTorque = EngineSpeed * _movementValue;
                    
                        break;
                    }
                    case Drivetrain.Rwd:
                    {
                        if(wheel.axle == Axle.Rear)
                            wheel.WheelCollider.motorTorque = EngineSpeed * _movementValue;

                        break;
                    }
                    case Drivetrain.Awd:
                    {
                        wheel.WheelCollider.motorTorque = EngineSpeed * _movementValue;
                        break;
                    }
                }
            }
        }
        
        private void Steer()
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.axle == Axle.Front)
                {
                    wheel.WheelCollider.steerAngle = 45 * _steerValue;
                }
            }
        }
        
        private void HandBrake()
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.axle == Axle.Rear)
                {
                    wheel.WheelCollider.brakeTorque = isHandBraking ? 1000 : 0;
                }
            }
        }
        
        private void AnimateWheels()
        {
            foreach (Wheel wheel in wheels)
            {
                wheel.WheelCollider.GetWorldPose(out Vector3 worldPose, out Quaternion worldPoseRotation);
                wheel.wheelObject.transform.position = worldPose;
                wheel.wheelObject.transform.rotation = worldPoseRotation;
            }
        }
    
        private void GetInputValues()
        {
            _movementValue = movement.action.ReadValue<float>();
            _steerValue = steer.action.ReadValue<float>();
            
            isHandBraking = handbrake.action.IsPressed();
        }
        
        private void OnEnable()
        {
            movement.action.Enable();
            steer.action.Enable();
            
            handbrake.action.Enable();
        }

        private void OnDisable()
        {
            movement.action.Disable();
            steer.action.Disable();
            
            handbrake.action.Disable();
        }
    }
}
