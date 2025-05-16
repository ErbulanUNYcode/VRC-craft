using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DesktopMode : UdonSharpBehaviour
{
	[SerializeField] private GameObject[] vrUis;
	[SerializeField] private Collider box;
	[SerializeField] private GameObject desktopUi;

	private VRCPlayerApi player;

	void Start()
	{
		player = Networking.LocalPlayer;

		if (!player.IsUserInVR())
		{
			foreach (GameObject vrUi in vrUis)
			{
				Destroy(vrUi);
			}
			vrUis = null;
		}
		else
		{
			Destroy(desktopUi);
			Destroy(gameObject);
		}
	}

	private void Update()
	{
		desktopUi.SetActive(Input.GetKey(KeyCode.Tab));
		box.enabled = Input.GetKey(KeyCode.Tab);
	}
}
