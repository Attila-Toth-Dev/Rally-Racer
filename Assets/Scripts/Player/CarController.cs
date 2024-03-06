using NaughtyAttributes;

using UnityEngine;
using UnityEngine.InputSystem;

public enum DriveTrain
{
    FWD,
    RWD,
    AWD
}

public class CarController : MonoBehaviour
{
    [Header("Car Controller Objects")]
    public Transform carBody;
    public Transform carNormal;
    public Rigidbody carRb;

    [Header("Car Properties")]
    public float topSpeed;
    public float accelAmount;
    public float brakeAmount;
    public float steering;

    [SerializeField] private DriveTrain driveTrain;

    [Header("Physics Properties")]
    [SerializeField] private float gravity = 9.82f;
    [SerializeField] private float gravityMultiplier;
    
    [SerializeField] private LayerMask layerMask;

    [Header("Model Parts")]
    private Transform FL_Wheel;
    private Transform FR_Wheel;
    
    private Transform BL_Wheel;
    private Transform BR_Wheel;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference steerAction;

    [Header("Debugging Tools")]
    [SerializeField] private float rayLength = 1f;
    [SerializeField, ReadOnly] private float move;
    [SerializeField, ReadOnly] private float steer;
    [SerializeField, ReadOnly] private bool isGrounded;
    [SerializeField, ReadOnly] private float currentSpeed;
    
    private float speed; 
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

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * accelAmount); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * brakeAmount); rotate = 0f;
    }

    private void FixedUpdate()
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

        // Moving
        Move();

        // Steering
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);
    }

    private void Steer(int _direction, float _amount)
    {
        rotate = (steering * _direction) * _amount;

        // Wheel Turning Animations
        switch (_direction)
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
        }
    }

    private void Move()
    {
        // Forward Acceleration
        if(Physics.Raycast(transform.position + (transform.up * 0.1f), Vector3.down, rayLength, layerMask))
            carRb.AddForce(carBody.transform.forward * (currentSpeed), ForceMode.Acceleration);
        
        // Gravity & Drag
        carRb.AddForce(Vector3.down * (gravity * gravityMultiplier), ForceMode.Acceleration);

        switch(driveTrain)
        {
            case DriveTrain.AWD:
                
        }
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
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        steerAction.action.Disable();
    }

    private void OnDrawGizmos()
    {
        // Draw Ray-cast of Grounding Ray for car
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + (transform.up * 0.1f), transform.position - (transform.up * rayLength));
    }
}
