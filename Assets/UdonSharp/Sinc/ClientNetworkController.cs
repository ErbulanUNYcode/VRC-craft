using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ClientNetworkController : UdonSharpBehaviour
{
	[SerializeField] private NetworkManager networkManager;
	[SerializeField] private DebugConsole debugConsole;
	[SerializeField] private WorldController worldController;
	/*
	 * 0 - set block
	 * 1 - give chunks
	 * 2 - request chunks
	 */
	[UdonSynced] private int type = 0;
	[UdonSynced] private string syncData = "";
	[UdonSynced] private int dataVersion = 0;
	[UdonSynced] private string playerId = "";
	private string playerName = "";

	private void Start()
	{
		playerName = Networking.GetOwner(gameObject).displayName;

		if (playerName == "TonyEric")
			debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=green>joined</color>");
		else
			debugConsole.Message($"{playerName}[<color=green>player</color>] <color=green>joined</color>");

		if (Networking.IsOwner(gameObject))
			networkManager.SetMyController(this);
	}

	public bool SetBlock(Vector3Int pos, int block)
	{
		var setState = worldController.SetBlock(pos, block);
		if (!setState) return false;
		syncData = $"{pos.x}/{pos.y}/{pos.z}/{block}";
		RequestSerialization();
		return true;
	}

	private void OnDestroy()
	{
		if (playerName == "TonyEric")
			debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=red>left</color>");
		else
			debugConsole.Message($"{playerName}[<color=green>player</color>] <color=red>left</color>");
	}

	public override void OnDeserialization()
	{
		switch (type)
		{
			case 0:
				ReceiveSetBlock();
				break;
			default:
				break;
		}
	}

	private void ReceiveSetBlock()
	{
		var data = syncData.Split('/');
		if (data.Length == 4)
		{
			var pos = new Vector3Int(int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]));
			var block = int.Parse(data[3]);
			worldController.SetBlock(pos, block);
		}
	}
}