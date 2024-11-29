using Car;
using UnityEngine;

[RequireComponent(typeof(Wheel))]
public class RayWheelCollision : MonoBehaviour
{
    [Header("Wheel GameObject")]
    [SerializeField] private Transform wheelObject;
    
    [Header("Raycast Settings")]
    [SerializeField] private int raysNumber = 15;
    [SerializeField] private float raysMaxAngle = 180f;
    
    private WheelCollider _wheelCollider;
    private float _orgRadius;
    
    private void Awake()
    {
        _wheelCollider = GetComponent<WheelCollider>();
        _orgRadius = _wheelCollider.radius;
    }

    private void FixedUpdate()
    {
        float radiusOffset = 0f;
        
        for (int i = 0; i <= raysNumber; ++i)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + (180f - raysMaxAngle) / 2f, transform.right) * transform.forward;
            
            // Handles the radius of the wheel collider with spherecast
            if (Physics.SphereCast(wheelObject.position, .5f, rayDirection, out RaycastHit sphereCastHit, _wheelCollider.radius))
            {
                Debug.DrawRay(wheelObject.position, sphereCastHit.point, Color.red);
                
                radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - sphereCastHit.distance);
            }
            
            // Handles the radius of the wheel collider with raycast
            //if(Physics.Raycast(wheelObject.position, rayDirection, out RaycastHit raycastHit, _wheelCollider.radius))
            //{
            //    Debug.DrawRay(wheelObject.position, raycastHit.point, Color.red);
            //    
            //    radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - raycastHit.distance);
            //}
            
            Debug.DrawRay(wheelObject.position, rayDirection * _orgRadius, Color.green);
        }
        
        _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, _orgRadius + radiusOffset, Time.deltaTime * 10f);
    }
}