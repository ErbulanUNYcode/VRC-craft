using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ClientNetworkController : UdonSharpBehaviour
{
	[SerializeField] private NetworkManager networkManager;
	[SerializeField] private WorldController worldController;
	[SerializeField] private DebugConsole debugConsole;
	private string _playerName = "";
	#region set block
	[UdonSynced] private int[] block = new int[0];
	[UdonSynced] private bool[] blockFix = new bool[0];
	#endregion
	#region owners
	[UdonSynced] private string[] owners = new string[0];
	[UdonSynced] private int[] ownersX = new int[0];
	[UdonSynced] private int[] ownersZ = new int[0];
	#endregion
	#region data
	[UdonSynced] private string[] data = new string[0];
	[UdonSynced] private int[] dataX = new int[0];
	[UdonSynced] private int[] dataZ = new int[0];
	#endregion
	#region request
	[UdonSynced] private int[] requestX = new int[0];
	[UdonSynced] private int[] requestZ = new int[0];
	[UdonSynced] private string[] requestOwner = new string[0];
	#endregion
	private void Start()
	{
		_playerName = Networking.GetOwner(gameObject).displayName;

		if (!Networking.LocalPlayer.IsOwner(gameObject))
		{
			if (_playerName == "TonyEric")
				debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=green>joined</color>", false);
			else
				debugConsole.Message($"{_playerName}[<color=green>player</color>] <color=green>joined</color>", false);

			if (Networking.LocalPlayer.isMaster)
				networkManager.SendStartData(_playerName);
		}
		else
		{
			networkManager.SetMyController(this);
		}
	}
	private void OnDestroy()
	{
		if (_playerName == "TonyEric")
			debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=red>left</color>", false);
		else
			debugConsole.Message($"{_playerName}[<color=green>player</color>] <color=red>left</color>", false);

		worldController.RemovePlayerOwning(_playerName);
	}
	public override void OnDeserialization()
	{
		if (worldController.GetSeed() == 0) return;//мир еще не прогружен

		for (int i = 0; i < data.Length; i++)//импорт полученных данных о чанках
		{
			worldController.SetData(data[i], new Vector2Int(dataX[i], dataZ[i]));
		}

		for (int i = 0; i < owners.Length; i++)//установка владельцев территорий
		{
			worldController.SetOwner(owners[i], new Vector2Int(ownersX[i], ownersZ[i]), _playerName);
		}

		var blockCount = block.Length / 5;
		for (int i = 0; i < blockCount; i++)//редактирование блоков
		{
			var index = i * 5;
			if (blockFix[i])
				worldController.FixBack(
					new Vector3Int(block[index++], block[index++], block[index++]),
					block[index++],
					block[index++]);
			else
				worldController.SetBlockNet(
					new Vector3Int(block[index++], block[index++], block[index++]),
					block[index++],
					block[index++],
					_playerName);
		}

		Debug.Log("SetRequests " + requestOwner.Length);
		for (int i = 0; i < requestOwner.Length; i++)//обработка запросов на получение данных от других игроков
		{
			worldController.SetRequest(requestOwner[i], new Vector2Int(requestX[i], requestZ[i]));
		}
	}
	private void RequestSerializationLog()
	{
		RequestSerialization();
	}

	public void SendNetworkData(
		int[] _block, bool[] _blockFix,
		string[] _owners, int[] _ownersX, int[] _ownersZ,
		string[] _data, int[] _dataX, int[] _dataZ,
		int[] _requestX, int[] _requestZ, string[] _requestOwner)
	{
		block = _block;
		blockFix = _blockFix;
		owners = _owners;
		ownersX = _ownersX;
		ownersZ = _ownersZ;
		data = _data;
		dataX = _dataX;
		dataZ = _dataZ;
		requestX = _requestX;
		requestZ = _requestZ;
		requestOwner = _requestOwner;
		RequestSerializationLog();
	}
}