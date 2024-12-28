using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarController : MonoBehaviour
    {
        [Header("Engine")]
        private float engineSpeed = 1000f;
        
        [Header("Transmission")]
        
        [Header("Drivetrain")]
        public Drivetrain drivetrain;
        
        [Header("Wheels")] 
        public List<Wheel> wheels;

        [Header("Controls")] 
        [SerializeField] private InputActionReference movement;
        [SerializeField] private InputActionReference steer;
        [SerializeField] private InputActionReference handbrake;

        private float movementValue;
        private float steerValue;
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
            
        }
        
        private void Steer()
        {
            
        }
        
        private void HandBrake()
        {
            
        }
        
        private void AnimateWheels()
        {
            
        }
    
        private void GetInputValues()
        {
            movementValue = movement.action.ReadValue<float>();
            steerValue = steer.action.ReadValue<float>();
            
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
