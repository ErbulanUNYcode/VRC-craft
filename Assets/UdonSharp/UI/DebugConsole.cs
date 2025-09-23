using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class DebugConsole : UdonSharpBehaviour
{
	//[SerializeField] private GameObject consolePrefab;
	[SerializeField] private GameObject vrConsolePrefab;
	[SerializeField] private GameObject desktopConsolePrefab;
	[SerializeField] private Transform vrUIconsole;
	[SerializeField] private Transform desktopUIconsole;
	private GameObject consolePrefab;
	private Transform console;

	private void Start()
	{
		vrConsolePrefab.gameObject.SetActive(false);
		desktopConsolePrefab.gameObject.SetActive(false);
	}

	public void Message(string message)
	{
		if (console == null)
		{
			if (Networking.LocalPlayer.IsUserInVR())
			{
				consolePrefab = vrConsolePrefab;
				console = vrUIconsole;
			}
			else
			{
				consolePrefab = desktopConsolePrefab;
				console = desktopUIconsole;
			}
		}
		Debug.Log(message);
		var m = Instantiate(consolePrefab, console);
		m.SetActive(true);
		m.GetComponent<ConsoleMessage>().SetText(message);
		LayoutRebuilder.ForceRebuildLayoutImmediate(m.GetComponent<RectTransform>());
		LayoutRebuilder.ForceRebuildLayoutImmediate(console.GetComponent<RectTransform>());
	}
}
