using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DesktopMode : UdonSharpBehaviour
{
	[SerializeField] private GameObject[] vrUis;
	[SerializeField] private GameObject BaseUi;
	[SerializeField] private GameObject InteractionUi;

	private VRCPlayerApi player;

	private Vector2 mousePos;

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
			Destroy(BaseUi);
			Destroy(InteractionUi);
			Destroy(gameObject);
		}
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.Tab))
		{
			BaseUi.SetActive(false);
			InteractionUi.SetActive(true);
		}
		else
		{
			BaseUi.SetActive(true);
			InteractionUi.SetActive(false);
		}
	}
}
