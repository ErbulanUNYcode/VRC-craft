using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DebugConsole : UdonSharpBehaviour
{
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

	public void Message(string message, bool ignore = true)
	{
		if (ignore && Networking.LocalPlayer.displayName != "TonyEric") return;
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
	}
}
