using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DebugConsole : UdonSharpBehaviour
{
	[SerializeField] private GameObject consolePrefab;
	[SerializeField] private Transform vrUIconsole;
	[SerializeField] private Transform desktopUIconsole;
	private Transform console;

	private void Start()
	{
		if (Networking.LocalPlayer.IsUserInVR())
			console = vrUIconsole;
		else
			console = desktopUIconsole;
	}


	public void Message(string message)
	{
		Debug.Log(message);
		Instantiate(consolePrefab, console).GetComponent<ConsoleMessage>().SetText(message).SetActive(true);
	}
}
