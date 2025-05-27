using UdonSharp;
using UnityEngine;
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
		Instantiate(consolePrefab, console).GetComponent<ConsoleMessage>().SetText(message).SetActive(true);
	}
}
