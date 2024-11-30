using UnityEngine;

namespace Collisions
{
    public class SphereWheelCollision : MonoBehaviour
    {
        [Header("Wheel GameObject")]
        [SerializeField] private Transform wheelObject;
    
        [Header("Raycast Settings")]
        [SerializeField] private int raysNumber = 5;
        [SerializeField] private float raysMaxAngle = 180f;
        [SerializeField] private float sphereWidth = .2f;
    
        private WheelCollider _wheelCollider;
        private float _orgRadius;
        
        private RaycastHit _sphereHit;
    
        private void Awake()
        {
            _wheelCollider = GetComponent<WheelCollider>();
            _orgRadius = _wheelCollider.radius;
        }

        private void Update()
        {
            CreateRaycastCheck();
        }

        private void CreateRaycastCheck()
        {
            float radiusOffset = 0f;

            for (int i = 0; i <= raysNumber; i++)
            {
                Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.forward;
                Ray ray = new Ray(wheelObject.position, rayDirection);
                
                // Handles the radius of the wheel collider with sphere cast
                if(Physics.SphereCast(ray, sphereWidth, out RaycastHit sphereHit))
                {
                    Debug.DrawLine(wheelObject.position, sphereHit.point, Color.red);
                
                    radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - sphereHit.distance - sphereWidth);
                    _sphereHit = sphereHit;
                }
            
                //Debug.DrawRay(wheelObject.position, rayDirection * _orgRadius, Color.green);
            }
        
            _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, _orgRadius + radiusOffset, Time.deltaTime * 5f);
        }
    }
}