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
    [SerializeField] private float wheelWidth = .4f;
    
    private WheelCollider _wheelCollider;
    private float _orgRadius;
    
    private void Awake()
    {
        _wheelCollider = GetComponent<WheelCollider>();
        _orgRadius = _wheelCollider.radius;
    }

    private void Update()
    {
        float radiusOffset = 0f;
        
        CreateRaycastCheck(radiusOffset);
        CreateRaycastRightCheck(radiusOffset);
        CreateRaycastLeftCheck(radiusOffset);
    }

    private void CreateRaycastCheck(float _radiusOffset)
    {
        for (int i = 0; i <= raysNumber; i++)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.forward;
            
            // Handles the radius of the wheel collider with raycast
            if(Physics.Raycast(wheelObject.position, rayDirection, out RaycastHit raycastHit, _wheelCollider.radius)  && !raycastHit.collider.isTrigger)
            {
                Debug.DrawLine(wheelObject.position, raycastHit.point, Color.red);
                
                _radiusOffset = Mathf.Max(_radiusOffset, _wheelCollider.radius - raycastHit.distance);
            }
            
            Debug.DrawRay(wheelObject.position, rayDirection * _orgRadius, Color.green);
        }
        
        _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, _orgRadius + _radiusOffset, Time.deltaTime * 5f);
    }

    private void CreateRaycastRightCheck(float _radiusOffset)
    {
        for (int i = 0; i <= raysNumber; i++)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.forward;
            
            // Handles the radius of the wheel collider with raycast
            if(Physics.Raycast(wheelObject.position + wheelObject.right * wheelWidth * .5f, rayDirection, out RaycastHit raycastHit, _wheelCollider.radius)  && !raycastHit.collider.isTrigger)
            {
                Debug.DrawLine(wheelObject.position + wheelObject.right * wheelWidth * .5f, raycastHit.point, Color.red);
                
                _radiusOffset = Mathf.Max(_radiusOffset, _wheelCollider.radius - raycastHit.distance);
            }
            
            Debug.DrawRay(wheelObject.position + wheelObject.right * wheelWidth * .5f, rayDirection * _orgRadius, Color.green);
        }
        
        _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, _orgRadius + _radiusOffset, Time.deltaTime * 5f);
    }
    
    private void CreateRaycastLeftCheck(float _radiusOffset)
    {
        for (int i = 0; i <= raysNumber; i++)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.forward;
            
            // Handles the radius of the wheel collider with raycast
            if(Physics.Raycast(wheelObject.position - wheelObject.right * wheelWidth * .5f, rayDirection, out RaycastHit raycastHit, _wheelCollider.radius)  && !raycastHit.collider.isTrigger)
            {
                Debug.DrawLine(wheelObject.position - wheelObject.right * wheelWidth * .5f, raycastHit.point, Color.red);
                
                _radiusOffset = Mathf.Max(_radiusOffset, _wheelCollider.radius - raycastHit.distance);
            }
            
            Debug.DrawRay(wheelObject.position - wheelObject.right * wheelWidth * .5f, rayDirection * _orgRadius, Color.green);
        }
        
        _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, _orgRadius + _radiusOffset, Time.deltaTime * 10f);
    }
}