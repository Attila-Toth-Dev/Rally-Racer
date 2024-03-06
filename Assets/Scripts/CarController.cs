using NaughtyAttributes;

using UnityEngine;

public class CarController : MonoBehaviour
{
    public float Speed { get; private set; }

        public Transform carBody;
        public Transform carNormal;
        public Rigidbody carRb;

        [Header("Car Properties")]
        [ReadOnly] public float topSpeed = 30f;
        [ReadOnly] public float accelerationAmount = 4f;
        [ReadOnly] public float brakeAmount = 4f;
        [ReadOnly] public float steering = 80f;

        [Header("Physics Properties")]
        public float gravity = 10f;
        public float inAirGravity = 5f;
        public float drag = 10f;
        public LayerMask layerMask;

        /*[Header("Car Models")]
        [SerializeField] private List<GameObject> carModels;
        
        [Header("Model Parts")]
        public Transform FL_Wheel;
        public Transform FR_Wheel;
        
        public Light FL_Light;
        public Light FR_Light;*/

        [Header("Input Actions")]
        //public InputActionReference moveAction;
        //public InputActionReference steerAction;

        [Header("Debugging Tools")]
        [SerializeField] private float rayLength = 1f;
        [SerializeField, ReadOnly] private float move;
        [SerializeField, ReadOnly] private float steer;
        [SerializeField, ReadOnly]private bool isGrounded;

        public float currentSpeed;
        private float rotate, currentRotate;

        private Quaternion normalPosition;

        private void Start()
        {
            int carIndex = PlayerPrefs.GetInt("Car");
            for(int i = 0; i < carNormal.childCount; i++)
            {
                if(i == carIndex)
                {
                    carNormal.GetChild(i).gameObject.SetActive(true);
                    carBody = carNormal.GetChild(i);
                }
            }

            topSpeed = PlayerPrefs.GetFloat("Speed");
            accelerationAmount = PlayerPrefs.GetFloat("Acceleration");
            steering = PlayerPrefs.GetFloat("Handling");

            normalPosition = new Quaternion(carNormal.rotation.x, carNormal.rotation.y, carNormal.rotation.z, 0);
        }

        private void Update()
        {
            // Update Inputs
            GetInputs();

            // Follow Collider
            transform.position = carRb.transform.position - new Vector3(0, 0.4f, 0);

            // Accelerate
            if(move < 0 || move > 0)
                Speed = topSpeed * move;

            // Steer
            if (steer != 0)
            {
                int dir = steer > 0 ? 1 : -1;
                float amount = Mathf.Abs(steer);

                Steer(dir, amount);
            }
            else Steer(0, 0);

            currentSpeed = Mathf.SmoothStep(currentSpeed, Speed, Time.deltaTime * accelerationAmount); Speed = 0f;
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

            /*// Wheel Turning Animations
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
            }*/

        }

        private void Move()
        {
            LayerMask onTrack = LayerMask.GetMask("On Track");
            //LayerMask offTrack = LayerMask.GetMask("Off Track");

            // Forward Acceleration
            if(Physics.Raycast(transform.position + (transform.up * 0.1f), Vector3.down, out RaycastHit hitGround, rayLength, onTrack))
                carRb.AddForce(isGrounded ? carBody.transform.forward * (currentSpeed) : carBody.transform.forward * (currentSpeed * 0.2f), ForceMode.Acceleration);
            else
                carRb.AddForce(carBody.transform.forward * (currentSpeed * 0.5f), ForceMode.Acceleration);
            
            // Gravity & Drag
            carRb.AddForce(isGrounded ? Vector3.down * gravity : Vector3.down * (gravity * inAirGravity), ForceMode.Acceleration);
            carRb.drag = drag;
        }

        private void GetInputs()
        {
            // Read Player Input values
            //move = moveAction.action.ReadValue<float>();
            //steer = steerAction.action.ReadValue<float>();
        }

        private void OnEnable()
        {
            //moveAction.action.Enable();
            //steerAction.action.Enable();
        }

        private void OnDisable()
        {
            //moveAction.action.Disable();
            //steerAction.action.Disable();
        }

        private void OnDrawGizmos()
        {
            // Draw Ray-cast of Grounding Ray for car
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + transform.up, transform.position - (transform.up * rayLength));
        }
}
