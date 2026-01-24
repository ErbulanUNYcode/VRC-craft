using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class SnapshotShare : UdonSharpBehaviour
{
	[SerializeField] private WorldData worldData;
	[SerializeField] private WorldController worldController;
	[SerializeField] private NetworkManager networkManager;
	[UdonSynced] string[] _data;
	[UdonSynced] string[] _owners;
	[UdonSynced] int[] _xPos;
	[UdonSynced] int[] _zPos;
	[UdonSynced] int _seed;
	[UdonSynced] string _for;

	void Start()
	{
		if (Networking.LocalPlayer.IsOwner(gameObject))
		{
			networkManager.SetMySnapshotController(this);
		}
	}

	public override void OnDeserialization()
	{
		if (_for != Networking.LocalPlayer.displayName) return;
		var positions = new Vector2Int[_xPos.Length];
		for (int i = 0; i < positions.Length; i++)
		{
			positions[i] = new Vector2Int(_xPos[i], _zPos[i]);
		}
		worldData.LoadSnapshot(_data, _owners, positions);
		worldController.SetSeed(_seed);
	}

	public void SendSnapshot(string playerName)
	{
		_data = worldData.GetWorldData();
		_owners = worldData.GetOwners();
		var positions = worldData.GetPositions();
		_xPos = new int[positions.Length];
		_zPos = new int[positions.Length];
		for (int i = 0; i < positions.Length; i++)
		{
			_xPos[i] = positions[i].x;
			_zPos[i] = positions[i].y;
		}
		_seed = worldController.GetSeed();
		_for = playerName;
		RequestSerialization();
	}
}
