using TMPro;

using UnityEngine;

public class CarGaugeUI : MonoBehaviour
{
	[Header("Target Vehicle")]
	[SerializeField] private CarController car;

	[Header("Gauge Placeholders")]
	[SerializeField] private TextMeshProUGUI speedometer;
	[SerializeField] private TextMeshProUGUI currentGear;

	private void Update()
	{
		speedometer.text = $"{(int)car.currentSpeed}km/h";
	}
}
