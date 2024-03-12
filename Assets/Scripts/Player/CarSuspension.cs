
using NaughtyAttributes;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Player
{
	public class CarSuspension : MonoBehaviour
	{
		[Header("Required Components")]
		[SerializeField] private Rigidbody rb;
		[SerializeField] private List<GameObject> wheels;
		[SerializeField] private Dictionary<GameObject, GameObject> points;

		[Header("Suspension Settings")]
		[SerializeField] private float suspensionDistance;
		[SerializeField] private float dampingFactor;
		[SerializeField] private float maxForce;

		[SerializeField] private float wheelRadius = 0.28f;

		[Header("Physics Checking")]
		[SerializeField] private LayerMask suspensionLayers;

		private void FixedUpdate()
		{
			GroundChecking();
		}

		private void GroundChecking()
		{
			foreach(GameObject wheel in wheels)
			{
				if(Physics.Raycast(wheel.transform.position, -transform.up, out RaycastHit hit, suspensionDistance))
				{
					float damping = dampingFactor * Vector3.Dot(rb.GetPointVelocity(wheel.transform.position), wheel.transform.up);
					rb.AddForceAtPosition(maxForce * Time.fixedDeltaTime * transform.up * Mathf.Max(((suspensionDistance - hit.distance + wheelRadius) / suspensionDistance + damping), 0), wheel.transform.position);
				}
			}
		}

		private void OnDrawGizmos()
		{
			
		}
	}
}