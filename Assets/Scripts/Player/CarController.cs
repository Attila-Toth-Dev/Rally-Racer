using NaughtyAttributes;

using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Car Controller Objects")]
    public Transform carBody;
    public Transform carNormal;
    public Rigidbody carRb;

    [Header("Car Settings")]
    public float topSpeed;
    public float accelAmount;
    public float brakeAmount;
    public float steering;

    [Header("Turbo Settings")] 
    public float turboPower;
    public float turboDuration;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask layerMask;

    [Header("Model Parts")]
    [SerializeField] private Transform flWheel;
    [SerializeField] private Transform frWheel;
    [SerializeField] private Transform blWheel;
    [SerializeField] private Transform brWheel;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference steerAction;
    [SerializeField] private InputActionReference driftAction;
    [SerializeField] private InputActionReference turboAction;

    [Header("Debugging Tools")]
    [SerializeField] private float rayLength = 1f;
    [SerializeField, ReadOnly] private float move;
    [SerializeField, ReadOnly] private float steer;
    [SerializeField, ReadOnly] private bool isGrounded;
    
    private float speed, currentSpeed; 
    private float rotate, currentRotate;

    private Quaternion normalPosition;

    private void Start() => normalPosition = new Quaternion(carNormal.rotation.x, carNormal.rotation.y, carNormal.rotation.z, 0);

    private void Update()
    {
        // Update Inputs
        GetInputs();

        // Follow Collider
        transform.position = carRb.transform.position - new Vector3(0, 0.55f, 0);
        
        // Accelerate
        if(move is < 0 or > 0) 
            speed = topSpeed * move;
        
        // Steer
        if (steer != 0)
        {
            int dir = steer > 0 ? 1 : -1;
            float amount = Mathf.Abs(steer);

            Steer(dir, amount);
        }
        else Steer(0, 0);

        // Current Speed and Rotate
        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * accelAmount); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * brakeAmount); rotate = 0f;
    }

    private void FixedUpdate()
    {
        // Ground Detection
        GroundDetection();
        
        // Moving
        Move();

        // Steering
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);
    }

    private void GroundDetection()
    {
        // Ground Detection
        if (Physics.Raycast(transform.position + (transform.up * 0.1f), Vector3.down, out RaycastHit hitNear, rayLength, layerMask))
        {
            // Normal Rotation
            carNormal.up = Vector3.Lerp(carNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
            carNormal.Rotate(0, transform.eulerAngles.y, 0);
            isGrounded = true;
        }
        else
        {
            carNormal.Rotate(normalPosition.x, normalPosition.y, normalPosition.z);
            isGrounded = false;
        }
    }

    private void Steer(int _direction, float _amount)
    {
        if(isGrounded)
            rotate = (steering * _direction) * _amount;
        else
            rotate = 0;

        // Wheel Turning Animations
        /*switch (_direction)
        {
            case -1:
                FL_Wheel.localEulerAngles = new Vector3(-180, steering * -1, 0);
                FR_Wheel.localEulerAngles = new Vector3(0, steering * -1, 0);
                break;

            case 1:
                FL_Wheel.localEulerAngles = new Vector3(-180, steering * 1, 0);
                FR_Wheel.localEulerAngles = new Vector3(0, steering * 1, 0);
                break;

            case 0:
                FL_Wheel.localEulerAngles = new Vector3(-180, steering * 0, 0);
                FR_Wheel.localEulerAngles = new Vector3(0, steering * 0, 0);
                break;
        }*/
    }

    private void Move()
    {
        // Forward Acceleration
        if(Physics.Raycast(transform.position + (transform.up * 0.1f), Vector3.down, rayLength, layerMask) && isGrounded)
            carRb.AddForce(carBody.transform.forward * (currentSpeed), ForceMode.Acceleration);
        else
            carRb.AddForce(carBody.transform.forward * carRb.velocity.magnitude, ForceMode.Acceleration);
        
        /*flWheel.Rotate(carRb.velocity.magnitude, 0, 0);
        frWheel.Rotate(carRb.velocity.magnitude, 0, 0);
        
        blWheel.Rotate(carRb.velocity.magnitude, 0, 0);
        brWheel.Rotate(carRb.velocity.magnitude, 0, 0);*/
    }

    private void GetInputs()
    {
        // Read Player Input values
        move = moveAction.action.ReadValue<float>();
        steer = steerAction.action.ReadValue<float>();
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + (transform.up * 0.1f), transform.position - (transform.up * rayLength));
    }
}
