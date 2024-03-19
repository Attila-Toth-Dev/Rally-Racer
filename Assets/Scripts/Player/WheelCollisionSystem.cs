using UnityEngine;

public class WheelCollisionSystem : MonoBehaviour
{
    public Transform wheelModel;
    public int raysNumber = 36;
    public float raysMaxAngle = 180f;

    private CarController carController;
    private WheelCollider wheelCollider;
    private float orgRadius;
    
    private void Awake()
    {
        wheelCollider = GetComponent<WheelCollider>();
        carController = GetComponentInParent<CarController>();

        orgRadius = wheelCollider.radius;
    }

    private void Update()
    {
        float radiusOffset = 0f;

        for(int i = 0; i <= raysNumber; i++)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.forward;

            if(Physics.Raycast(wheelModel.position, rayDirection, out RaycastHit hit, wheelCollider.radius))
            {
                if(!hit.transform.IsChildOf(carController.transform))
                {
                    Debug.DrawLine(wheelModel.position, hit.point, Color.red);
                    radiusOffset = Mathf.Max(radiusOffset, wheelCollider.radius - hit.distance);
                }
            }

            Debug.DrawRay(wheelModel.position, rayDirection * orgRadius, Color.green);
        }

        wheelCollider.radius = Mathf.LerpUnclamped(wheelCollider.radius, orgRadius + radiusOffset, Time.deltaTime * 10.0f);
    }
}
