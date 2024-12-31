using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Car
{
    public class CarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform accelerationPoint;
        [SerializeField] private Rigidbody carRb;
        
        [Header("Car Settings")]
        [SerializeField] private float acceleration = 25f;
        [SerializeField] private float maxSpeed = 100f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float steerStrength = 15f;
        [SerializeField] private AnimationCurve steeringCurve;
        [SerializeField] private float dragCoefficient = 1f;
        
        //[Header("Engine Settings")]
        
        //[Header("Transmission Settings")]
        
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
        [SerializeField] private float moveInput;
        [SerializeField] private float steerInput;
        [SerializeField] private bool isHandBraking;
        [FormerlySerializedAs("isCarGrounded"),SerializeField] private bool isGrounded = false;

        private Vector3 currentCarLocalVelocity;
        private float carVelocityRatio;
        
        private readonly int[] wheelsIsGrounded = new int[4];
        
        private void Update()
        {
            GetInputValues();
        }

        private void FixedUpdate()
        {
            Movement();
            Suspension();
            CarGroundedCheck();
            
            CalculateCarVelocity();
        }

        #region Movement

        private void Movement()
        {
            if(isGrounded)
            {
                Acceleration();
                Deceleration();
                
                Steer();
                
                SidewaysDrag();
            }
        }
            
        private void Acceleration()
        {
            carRb.AddForceAtPosition(acceleration * moveInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
        }

        private void Deceleration()
        {
            carRb.AddForceAtPosition(deceleration * moveInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
        }

        private void Steer()
        {
            carRb.AddTorque(steerStrength * steerInput * steeringCurve.Evaluate(carVelocityRatio) * Mathf.Sign(carVelocityRatio) * transform.up, ForceMode.Acceleration);
        }

        private void SidewaysDrag()
        {
            float currentSidewaysSpeed = currentCarLocalVelocity.x;
            
            float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;
            
            Vector3 dragForce = transform.right * dragMagnitude;
            
            carRb.AddForceAtPosition(dragForce, carRb.worldCenterOfMass, ForceMode.Acceleration);
        }

        #endregion

        #region Check Functions

        private void CarGroundedCheck()
        {
            int tempGroundedWheels = 0;

            for(int i = 0; i < wheelsIsGrounded.Length; i++)
            {
                tempGroundedWheels += wheelsIsGrounded[i];
            }

            if(tempGroundedWheels > 1)
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }

        private void CalculateCarVelocity()
        {
            currentCarLocalVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
        }

        #endregion

        #region Suspension Functions

        private void Suspension()
        {
            for(int i = 0; i < suspensionPoints.Length; i++)
            {
                RaycastHit hit;
                float maxLength = restLength + springTravel;

                if(Physics.Raycast(suspensionPoints[i].position, -suspensionPoints[i].up, out hit, maxLength + wheelRadius))
                {
                    wheelsIsGrounded[i] = 1;
                    
                    float currentSpringLength = hit.distance - wheelRadius;
                    float springCompression = (restLength - currentSpringLength) / springTravel;

                    float springVelocity = Vector3.Dot(carRb.GetPointVelocity(suspensionPoints[i].position), suspensionPoints[i].up);
                    float dampForce = damperStiffness * springVelocity;

                    float springForce = springStiffness * springCompression;

                    float netForce = springForce - dampForce;

                    carRb.AddForceAtPosition(netForce * suspensionPoints[i].up, suspensionPoints[i].position);
                    Debug.DrawLine(suspensionPoints[i].position, hit.point, Color.red);
                }
                else
                {
                    wheelsIsGrounded[i] = 0;
                    Debug.DrawLine(suspensionPoints[i].position, suspensionPoints[i].position + (wheelRadius + maxLength) * -suspensionPoints[i].up, Color.green);
                }
            }
        }

        #endregion

        #region Input Functions

        private void GetInputValues()
        {
            moveInput = movement.action.ReadValue<float>();
            steerInput = steer.action.ReadValue<float>();
            
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

        #endregion
    }
}
