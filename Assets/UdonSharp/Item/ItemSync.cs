using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ItemSync : UdonSharpBehaviour
{
	[SerializeField]
	private ItemMeshGenerator itemMeshGenerator;
	[SerializeField] private VRC_Pickup pickup;

	[SerializeField]
	private MeshFilter item;
	[SerializeField]
	private MeshFilter block;

	[SerializeField] private Inventory VRInventoy;
	[SerializeField] private Inventory DesktopInventory;

	[UdonSynced]
	private int itemID = -1;
	[UdonSynced]
	private bool isBlock = false;
	[UdonSynced]
	private float scale = 1f;

	public void ChangeItem(int newItemID, bool newIsBlock)
	{
		if (newIsBlock == isBlock && newItemID == itemID)
			return;

		itemID = newItemID;
		isBlock = newIsBlock;
		RequestSerialization();
	}

	void Start()
	{
		var localPlayer = Networking.LocalPlayer;
		if (localPlayer.IsOwner(gameObject))
		{
			Destroy(item.gameObject);
			Destroy(block.gameObject);
			if (localPlayer.IsUserInVR())
				VRInventoy.AddItemSync(this);
			else
				DesktopInventory.AddItemSync(this);
			return;
		}

		item.mesh = null;
		block.mesh = null;
		owner = Networking.GetOwner(gameObject);
	}

	VRCPlayerApi owner = null;
	private void Update()
	{
		if (owner == null) return;
		var tracking = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
	}

	public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
	{
		if (owner == null) return;
		scale = player.GetAvatarEyeHeightAsMeters() / 1.6f;
		RequestSerialization();
	}

	public override void OnDeserialization()
	{
		if (isBlock)
		{
			block.mesh = itemMeshGenerator.GetMesh(itemID);
			item.mesh = null;
		}
		else
		{
			item.mesh = itemMeshGenerator.GetMesh(itemID);
			block.mesh = null;
		}

		transform.localScale = Vector3.one * scale;
	}
}
