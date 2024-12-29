using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarController : MonoBehaviour
    {
        [Header("Engine Settings")]
        
        [Header("Transmission Settings")]
        
        [Header("Drivetrain Settings")]
        [SerializeField] private Drivetrain drivetrain;
        
        [Header("Suspension Settings")]
        [SerializeField] private Transform[] suspensionPoints;
        [SerializeField] private float springStiffness;
        [SerializeField] private float damperStiffness;
        [SerializeField] private float restLength;
        [SerializeField] private float springTravel;
        [SerializeField] private float wheelRadius;
        
        [Header("Wheel Settings")] 
        [SerializeField] private Wheel[] wheels;
        
        [Header("Controls")] 
        [SerializeField] private InputActionReference movement;
        [SerializeField] private InputActionReference steer;
        [SerializeField] private InputActionReference handbrake;

        [Header("Debugging")]
        [SerializeField] private float movementValue;
        [SerializeField] private float steerValue;
        [SerializeField] private bool isHandBraking;
        
        private Rigidbody carRigidbody;

        private void Awake()
        {
            carRigidbody = GetComponent<Rigidbody>();
        }

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
            
            Suspension();
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

        private void Suspension()
        {
            foreach(Transform rayPoint in suspensionPoints)
            {
                RaycastHit hit;
                float maxLength = restLength + springTravel;

                if(Physics.Raycast(rayPoint.position, -rayPoint.up, out hit, maxLength + wheelRadius))
                {
                    float currentSpringLength = hit.distance - wheelRadius;
                    float springCompression = (restLength - currentSpringLength) / springTravel;
                    
                    float springVelocity = Vector3.Dot(carRigidbody.GetPointVelocity(rayPoint.position), rayPoint.up);
                    float dampForce = damperStiffness * springVelocity;
                    
                    float springForce = springStiffness * springCompression;
                    
                    float netForce = springForce - dampForce;
                    
                    carRigidbody.AddForceAtPosition(netForce * rayPoint.up, rayPoint.position);
                    Debug.DrawLine(rayPoint.position, hit.point, Color.red);
                }
                else
                    Debug.DrawLine(rayPoint.position, rayPoint.position + (wheelRadius + maxLength) * -rayPoint.up, Color.green);
            }
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
