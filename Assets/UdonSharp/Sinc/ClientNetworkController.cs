using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ClientNetworkController : UdonSharpBehaviour
{
	[SerializeField] private NetworkManager networkManager;
	[SerializeField] private DebugConsole debugConsole;
	[SerializeField] private WorldController worldController;
	[UdonSynced] private string syncData = "";
	private string playerName = "";

	private void Start()
	{
		playerName = Networking.GetOwner(gameObject).displayName;
		debugConsole.Message($"[{playerName}]  joined");
		if (Networking.IsOwner(gameObject)) networkManager.SetMyController(this);
	}

	public void SetBlock(Vector3Int pos, int block)
	{
		worldController.SetBlock(pos, block);
		syncData = $"{pos.x}/{pos.y}/{pos.z}/{block}";
		RequestSerialization();
	}

	private void OnDestroy()
	{
		debugConsole.Message($"[{playerName}]  left");
	}

	public override void OnDeserialization()
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
