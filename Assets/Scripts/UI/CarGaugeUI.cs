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
		speedometer.text = SetTextData();
	}

	private string SetTextData()
	{
		float speedVal = car.carRb.velocity.magnitude;
		
		return $"{((int)speedVal * 5).ToString()}km/h";
	}
}
