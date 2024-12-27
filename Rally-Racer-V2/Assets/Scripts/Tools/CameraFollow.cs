using UnityEngine;

namespace Tools
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")] 
        [SerializeField] private Transform target;

        [Header("Camera Settings")]
        [SerializeField] private float followDistance = 8f;
        [SerializeField] private float elevationAngle = 6f;
        [SerializeField] private float orbitalAngle = 2f;

        [Header("Smoothing Settings")]
        [SerializeField] private bool isMovementSmoothing = true;
        [SerializeField] private bool isRotationSmoothing = true;
        [SerializeField] private float rotationSmoothing = 5.0f;

        private Vector3 desiredPosition;
        private Transform cameraPosition;

        private void Awake()
        {
            cameraPosition = GetComponent<Transform>();
        }

        private void FixedUpdate()
        {
            // Check for valid target
            if(target != null)
                desiredPosition = target.position + target.TransformDirection(Quaternion.Euler(elevationAngle, orbitalAngle, 0f) * (new Vector3(0, 0, -followDistance)));

            // Movement Smoothing
            cameraPosition.position = isMovementSmoothing ? Vector3.Lerp(cameraPosition.position, desiredPosition, Time.deltaTime * 5.0f) : desiredPosition;
		
            // Rotation Smoothing
            if (isRotationSmoothing)
                cameraPosition.rotation = Quaternion.Lerp(cameraPosition.rotation, Quaternion.LookRotation(target.position - cameraPosition.position), rotationSmoothing * Time.deltaTime);
            else
                cameraPosition.LookAt(target);
        }
    }
}
