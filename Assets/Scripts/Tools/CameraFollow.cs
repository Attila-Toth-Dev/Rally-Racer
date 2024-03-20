using NaughtyAttributes;

using System;

using UnityEngine;

namespace Tools
{
	public class CameraFollow : MonoBehaviour
	{
		[Header("Target")] 
		[SerializeField] private Transform targetPos;

		[Header("Camera Settings")]
		[SerializeField] private float followDistance = 8f;
		[SerializeField] private float elevationAngle = 6f;
		[SerializeField] private float orbitalAngle = 2f;

		[Header("Smoothing Settings")]
		[SerializeField] private bool isMovementSmoothing = true;
		[SerializeField] private bool isRotationSmoothing = true;
		[SerializeField, EnableIf("isRotationSmoothing")] private float rotationSmoothing = 5.0f;

		private Vector3 desiredPosition;
		private Transform cameraPos;

		private void Start()
		{
			// Get Camera Pos
			cameraPos = GetComponent<Transform>();
			
			// Get Target Pos
			if(targetPos == null)
			{
				try
				{
					targetPos = FindObjectOfType<CarController>().transform;
				}

				catch(Exception e)
				{
					Console.WriteLine(e);

					throw;
				}	
			}
		}

		private void FixedUpdate()
		{
			// Check for valid target
			if(targetPos != null)
				desiredPosition = targetPos.position + targetPos.TransformDirection(Quaternion.Euler(elevationAngle, orbitalAngle, 0f) * (new Vector3(0, 0, -followDistance)));

			// Movement Smoothing
			cameraPos.position = isMovementSmoothing ? Vector3.Lerp(cameraPos.position, desiredPosition, Time.deltaTime * 5.0f) : desiredPosition;
			
			// Rotation Smoothing
			if (isRotationSmoothing)
				cameraPos.rotation = Quaternion.Lerp(cameraPos.rotation, Quaternion.LookRotation(targetPos.position - cameraPos.position), rotationSmoothing * Time.deltaTime);
			else
				cameraPos.LookAt(targetPos);
		}
	}
}