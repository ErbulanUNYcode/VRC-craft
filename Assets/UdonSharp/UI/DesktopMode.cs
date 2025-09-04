using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DesktopMode : UdonSharpBehaviour
{
	[SerializeField] private GameObject[] vrUis;
	[SerializeField] private Collider box;
	[SerializeField] private GameObject desktopUi;
	[SerializeField] private GameObject desktopLight;
	//[SerializeField] private bool testModeIsVR = false;

	private VRCPlayerApi player;

	void Start()
	{
		player = Networking.LocalPlayer;

		if (!player.IsUserInVR())
		//if (!testModeIsVR)
		{
			foreach (GameObject vrUi in vrUis)
			{
				Destroy(vrUi);
			}
			vrUis = null;
		}
		else
		{
			Destroy(desktopLight);
			Destroy(gameObject);
		}
	}

	private void Update()
	{
		desktopUi.SetActive(Input.GetKey(KeyCode.Tab));
		box.enabled = Input.GetKey(KeyCode.Tab);
	}
}
