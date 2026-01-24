using System;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NetworkManager : UdonSharpBehaviour
{
	[SerializeField] private WorldController worldController;

	#region variables
	private ClientNetworkController myNetworkController;
	private SnapshotShare mySnapshotController;
	public string lastCommand = "";
	#endregion

	private bool hasRequest = false;
	private float lastRequestTime = 0f;
	private float lastSendTime = 0f;
	#region set block
	int[] _block = new int[40];
	bool[] _blockFix = new bool[8];
	int blockCount = 0;
	#endregion
	#region owners
	private string[] _owners = new string[180];
	private int[] _ownersX = new int[180];
	private int[] _ownersZ = new int[180];
	private int ownersCount = 0;
	#endregion
	#region data
	private string[] _data = new string[160];
	private int[] _dataX = new int[160];
	private int[] _dataZ = new int[160];
	private int dataCount = 0;
	#endregion
	#region request
	private int[] _requestX = new int[160];
	private int[] _requestZ = new int[160];
	private string[] _requestOwner = new string[160];
	private int requestCount = 0;

	private void FixedUpdate()
	{
		if (myNetworkController == null)
		{
			Debug.Log($"no controller");
			return;
		}
		if (Time.time - lastSendTime > 5f)
		{
			worldController.SendActualData();
			lastSendTime = Time.time;
		}
		if (!hasRequest) return;
		if (Time.time - lastRequestTime < 0.1f) return;
		Debug.Log($"manager update");

		if (blockCount > 0)
			Debug.Log($"block {blockCount}");
		var block = new int[blockCount * 5];
		var blockFix = new bool[blockCount];
		Array.Copy(_block, block, blockCount * 5);
		Array.Copy(_blockFix, blockFix, blockCount);

		if (ownersCount > 0)
			Debug.Log($"owners {ownersCount}");
		var owners = new string[ownersCount];
		var ownersX = new int[ownersCount];
		var ownersZ = new int[ownersCount];
		Array.Copy(_owners, owners, ownersCount);
		Array.Copy(_ownersX, ownersX, ownersCount);
		Array.Copy(_ownersZ, ownersZ, ownersCount);

		if (dataCount > 0)
			Debug.Log($"data {dataCount}");
		if (dataCount > 0) lastSendTime = Time.time;
		var data = new string[dataCount];
		var dataX = new int[dataCount];
		var dataZ = new int[dataCount];
		Array.Copy(_data, data, dataCount);
		Array.Copy(_dataX, dataX, dataCount);
		Array.Copy(_dataZ, dataZ, dataCount);

		if (requestCount > 0)
			Debug.Log($"request {requestCount}");
		var requestX = new int[requestCount];
		var requestZ = new int[requestCount];
		var requestOwner = new string[requestCount];
		Array.Copy(_requestX, requestX, requestCount);
		Array.Copy(_requestZ, requestZ, requestCount);
		Array.Copy(_requestOwner, requestOwner, requestCount);

		Debug.Log($"send to network");
		myNetworkController.SendNetworkData(
			block, blockFix,
			owners, ownersX, ownersZ,
			data, dataX, dataZ,
			requestX, requestZ, requestOwner);
		Reset();
	}

	public void SetMyController(ClientNetworkController controller)
	{
		myNetworkController = controller;
		Reset();
	}
	public void SetMySnapshotController(SnapshotShare snapshotShare)
	{
		mySnapshotController = snapshotShare;
	}
	internal void SendStartData(string playerName)
	{
		lastCommand = $"SendStartData {playerName}";
		Debug.Log($"send start data to {playerName}");
		mySnapshotController.SendSnapshot(playerName);
		Debug.Log($"send start data to {playerName} done");
	}
	#endregion
	private void Reset()
	{
		blockCount = 0;
		ownersCount = 0;
		dataCount = 0;
		requestCount = 0;
		hasRequest = false;
		lastRequestTime = Time.time;
	}

	public void SetBlock(Vector3Int pos, int to, int from, bool fix)
	{
		lastCommand = $"SetBlock {pos.x} {pos.y} {pos.z} {to} {from} {fix}";
		Debug.Log(lastCommand);
		_blockFix[blockCount] = fix;
		var index = blockCount * 5;
		_block[index++] = pos.x;
		_block[index++] = pos.y;
		_block[index++] = pos.z;
		_block[index++] = to;
		_block[index++] = from;
		blockCount++;
		hasRequest = true;
	}
	public void SetOwner(string owner, Vector2Int pos)
	{
		lastCommand = $"SetOwner {owner} {pos.x} {pos.y}";
		Debug.Log(lastCommand);
		int left = 0;
		int right = ownersCount - 1;

		while (left <= right)
		{
			int mid = (left + right) >> 1;

			int cmp = _ownersX[mid] - pos.x;
			if (cmp == 0) cmp = _ownersZ[mid] - pos.y;

			if (cmp == 0)
			{
				_owners[mid] = owner;
				return;
			}
			else if (cmp < 0)
				left = mid + 1;
			else
				right = mid - 1;
		}

		if (ownersCount >= _owners.Length)
			return;

		if (left < ownersCount)
		{
			Array.Copy(_owners, left, _owners, left + 1, ownersCount - left);
			Array.Copy(_ownersX, left, _ownersX, left + 1, ownersCount - left);
			Array.Copy(_ownersZ, left, _ownersZ, left + 1, ownersCount - left);
		}

		_owners[left] = owner;
		_ownersX[left] = pos.x;
		_ownersZ[left] = pos.y;
		ownersCount++;
	}
	public void SetData(string data, Vector2Int pos)
	{
		if (data == null)
		{
			Debug.LogError($"data is null");
			return;
		}
		lastCommand = $"SetData {pos.x} {pos.y} {data.Length}";
		Debug.Log(lastCommand);
		_data[dataCount] = data;
		_dataX[dataCount] = pos.x;
		_dataZ[dataCount] = pos.y;
		dataCount++;
		hasRequest = true;
	}
	public void SetRequest(string owner, Vector2Int pos)
	{
		lastCommand = $"SetRequest {owner} {pos.x} {pos.y}";
		Debug.Log(lastCommand);
		_requestOwner[requestCount] = owner;
		_requestX[requestCount] = pos.x;
		_requestZ[requestCount] = pos.y;
		requestCount++;
		hasRequest = true;
	}
}