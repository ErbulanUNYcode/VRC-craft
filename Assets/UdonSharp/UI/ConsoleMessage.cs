using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleMessage : UdonSharpBehaviour
{
	[SerializeField] private TextMeshProUGUI text;
	[SerializeField] private Image background;

	public GameObject SetText(string message)
	{
		text.text = message;
		LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
		LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
		return gameObject;
	}

	float _timeAlive = 0f;

	private void Update()
	{
		_timeAlive += Time.deltaTime / 6;
		if (_timeAlive > 1)
		{
			Destroy(gameObject);
		}

		if (_timeAlive < 0.5f) return;
		background.color = new Color(0, 0, 0, Mathf.Lerp(0.78f, 0, _timeAlive * 2 - 1));
		text.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, _timeAlive * 2 - 1));
	}
}
