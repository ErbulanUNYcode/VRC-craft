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
	 * 2 - test
	 */
	[UdonSynced] private int type = 0;
	[UdonSynced] private int intData = 0;
	[UdonSynced] private int[] intArray = new int[0];
	[UdonSynced] private string stringData = "";
	[UdonSynced] private string[] stringArray = new string[0];
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

		if (!Networking.IsOwner(gameObject) && Networking.LocalPlayer.isMaster) networkManager.GiveKitStart(playerName);
	}

	public bool SetBlock(Vector3Int pos, int block)
	{
		var state = worldController.SetBlock(pos, block);
		if (!state) return false;

		Reset();
		type = 0;
		stringData = $"{pos.x}/{pos.y}/{pos.z}/{block}";
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
			case 1:
				SetChunksData();
				break;
			default:
				break;
		}
	}

	private void ReceiveSetBlock()
	{
		var data = stringData.Split('/');
		if (data.Length == 4)
		{
			var pos = new Vector3Int(int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]));
			var block = int.Parse(data[3]);
			worldController.SetBlock(pos, block);
		}
	}


	private void Reset()
	{
		type = 0;
		intData = 0;
		intArray = new int[0];
		stringData = "";
		stringArray = new string[0];
	}

	public void GiveStartData(string playerName, string[] data, int[] positions)
	{
		debugConsole.Message($"Giving start data to {playerName}");
		debugConsole.Message($"from master {Networking.LocalPlayer.displayName}");
		debugConsole.Message($"with seed {worldController.GetSeed()}");
		Reset();
		type = 1;
		intData = worldController.GetSeed();
		stringArray = data;
		intArray = positions;
		stringData = playerName;
		RequestSerialization();
	}

	private void SetChunksData()
	{
		if (stringData != Networking.LocalPlayer.displayName) return;
		worldController.SetChunksData(stringArray, intArray);
		worldController.SetSeed(intData);
	}
}