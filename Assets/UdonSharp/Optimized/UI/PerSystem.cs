using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC_MINE.UI;
using VRC_MINE.World;

namespace VRC_MINE.Data
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class PerSystem : UdonSharpBehaviour
	{
		[UdonSynced] private Vector3 spawnPosition;
		[UdonSynced] private Quaternion spawnRotation;
		[UdonSynced] private byte[] invIcons;
		[UdonSynced] private byte[] invCounts;
		[SerializeField] private WorldGenerator worldGenerator;
		[SerializeField] private InvCell[] PCCells;
		[SerializeField] private InvCell[] VRCells;
		public override void OnPlayerRestored(VRCPlayerApi player)
		{
			if (!player.isLocal)
			{
				Destroy(gameObject);
				return;
			}
			if (spawnPosition == Vector3.zero)
			{
				spawnPosition = player.GetPosition();
				spawnRotation = player.GetRotation();
			}

			player.TeleportTo(spawnPosition + Vector3.up, spawnRotation);
			worldGenerator.OnPlayerTeleport();



			//if (invIcons == null)
			{
				invIcons = new byte[41];
				invCounts = new byte[41];
				for (byte i = 0; i < 15; i++)
				{
					invIcons[i] = i;
					invCounts[i] = 64;
				}
			}
			//else
			{
				Debug.Log("Loading inventory with " + invIcons.Length + " cells");
			}
			var cells = player.IsUserInVR() ? VRCells : PCCells;
			for (int i = 0; i < cells.Length; i++)
			{
				if (invCounts[i] == 0) continue;
				cells[i].TryGiveItems(invIcons[i], invCounts[i]);
			}
		}

		private void Update()
		{
			if (Time.frameCount % 300 == 0) SaveSpawnPoint();
		}

		public void SaveSpawnPoint()
		{
			VRCPlayerApi player = Networking.LocalPlayer;

			spawnPosition = player.GetPosition();
			spawnRotation = player.GetRotation();

			RequestSerialization();
		}
	}
}