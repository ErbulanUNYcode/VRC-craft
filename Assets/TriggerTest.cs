using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TriggerTest : MonoBehaviour
{
	[SerializeField] private GraphicRaycaster raycaster;
	[SerializeField] private Camera eventCamera;

	private PointerEventData pointerData;
	private readonly List<RaycastResult> results = new List<RaycastResult>();

	private void Start()
	{
		pointerData = new PointerEventData(EventSystem.current);

		Debug.Log($"[UI TEST] Cursor world position: {transform.position}");
		Debug.Log($"[UI TEST] Raycaster: {raycaster}");
		Debug.Log($"[UI TEST] Event Camera: {eventCamera}");
	}

	private void Update()
	{
		Vector3 screenPosition = eventCamera.WorldToScreenPoint(transform.position);

		pointerData.position = screenPosition;

		results.Clear();
		raycaster.Raycast(pointerData, results);

		Debug.Log(
			$"[UI TEST] World={transform.position} " +
			$"Screen={screenPosition} " +
			$"Hits={results.Count}"
		);

		for (int i = 0; i < results.Count; i++)
		{
			Debug.Log(
				$"[UI TEST] Hit {i}: " +
				$"{results[i].gameObject.name} " +
				$"distance={results[i].distance}"
			);
		}
	}
}