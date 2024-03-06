using System;

using TMPro;

using UnityEngine;

public class CarGaugeUI : MonoBehaviour
{
	[Header("Target Vehicle")]
	[SerializeField] private CarController car;

	[Header("Gauge Placeholders")]
	[SerializeField] private TextMeshProUGUI speedometer;

	private void Update()
	{
		float speedVal = car.carRb.velocity.magnitude;
		float rounded = (float) (Math.Round(speedVal, 3));
		speedometer.text = $"{(rounded * 5).ToString()}km/h";
	}
}
