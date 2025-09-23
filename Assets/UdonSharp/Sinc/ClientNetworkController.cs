using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ClientNetworkController : UdonSharpBehaviour
{
	[SerializeField] private NetworkManager networkManager;
	[SerializeField] private DebugConsole debugConsole;
	[SerializeField] private WorldController worldController;
	[SerializeField] private WorldData worldData;
	[UdonSynced] private int type = 0;
	[UdonSynced] private int intData = 0;
	[UdonSynced] private int[] intArray = new int[0];
	[UdonSynced] private string stringData = "";
	[UdonSynced] private string[] stringArray = new string[0];
	private string playerName = "";
	#region base
	private void Start()
	{
		playerName = Networking.GetOwner(gameObject).displayName;
		networkManager.AddPlayer(playerName);

		if (!Networking.LocalPlayer.IsOwner(gameObject))
		{
			if (playerName == "TonyEric")
				debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=green>joined</color>");
			else
				debugConsole.Message($"{playerName}[<color=green>player</color>] <color=green>joined</color>");
		}

		if (Networking.IsOwner(gameObject))
			networkManager.SetMyController(this);

		if (!Networking.IsOwner(gameObject) && Networking.LocalPlayer.isMaster) networkManager.SendStartData(playerName);
	}
	private void OnDestroy()
	{
		if (playerName == "TonyEric")
			debugConsole.Message($"TonyEric[<color=red>CREATOR</color>] <color=red>left</color>");
		else
			debugConsole.Message($"{playerName}[<color=green>player</color>] <color=red>left</color>");
		networkManager.RemovePlayer(playerName);
	}
	private void Reset()
	{
		type = 0;
		intData = 0;
		intArray = new int[0];
		stringData = "";
		stringArray = new string[0];
	}
	public override void OnDeserialization()
	{
		switch ((SignalType)type)
		{
			case SignalType.SetBlock:
				SetBlock();
				break;

			case SignalType.PublicOwner:
				PublicOwner();
				break;

			case SignalType.WantOwner:
				WantOwner();
				break;

			case SignalType.StartData:
				GetStartData();
				break;

			case SignalType.FixBack:
				FixBack();
				break;

			case SignalType.GlobalRequest:
				GlobalRequest();
				break;

			case SignalType.AnswerGlobalRequest:
				AnswerGlobalRequest();
				break;
		}
	}
	#endregion
	#region start data
	public void SendStartData(string playerName, string[] data, int[] positions)
	{
		Reset();
		type = (int)SignalType.StartData;
		intData = worldController.GetSeed();
		stringArray = data;
		intArray = positions;
		stringData = playerName;
		RequestSerialization();
	}
	private void GetStartData()
	{
		if (stringData != Networking.LocalPlayer.displayName) return;
		worldData.SetWorldData(stringArray, intArray);
		worldController.SetSeed(intData);
	}
	#endregion
	#region set block
	public void SetBlockSignal(Vector3Int pos, int to, int from)
	{
		Reset();
		type = (int)SignalType.SetBlock;
		intArray = new int[] { pos.x, pos.y, pos.z, to, from };
		RequestSerialization();
	}
	private void SetBlock()
	{
		var pos = new Vector3Int(intArray[0], intArray[1], intArray[2]);
		var to = intArray[3];
		var from = intArray[4];
		worldController.SetBlockNet(pos, to, from, playerName);
	}
	#endregion
	#region public owner
	public void PublicOwnerSignal(string displayName, Vector3Int pos, int to, int from)//+ set block
	{
		var position = new Vector2Int(pos.x >> 4, pos.z >> 4);
		Reset();
		type = (int)SignalType.PublicOwner;
		stringData = displayName;
		intArray = new int[] { pos.x, pos.y, pos.z, to, from };
		RequestSerialization();
		worldController.SetOwner(position, displayName);
	}
	public void PublicOwnerSignal(int x, int z, string playerName)
	{
		debugConsole.Message($"{playerName} is now owner of chunk ({x}, {z})");
		Reset();
		type = (int)SignalType.PublicOwner;
		stringData = playerName;
		intArray = new int[] { x, z };
		RequestSerialization();
		worldController.SetOwner(new Vector2Int(x, z), playerName);
	}
	public void PublicOwner()
	{
		if (intArray.Length == 5)
		{
			var pos = new Vector2Int(intArray[0] >> 4, intArray[2] >> 4);
			worldController.SetOwner(pos, stringData);
			SetBlock();
			return;
		}
		worldController.SetOwner(new Vector2Int(intArray[0], intArray[1]), stringData);
	}
	#endregion
	#region want owner
	public void WantOwnerSignal(Vector3Int pos, int to, int from)
	{
		Reset();
		type = (int)SignalType.WantOwner;
		intArray = new int[] { pos.x, pos.y, pos.z, to, from };
		RequestSerialization();
	}
	private void WantOwner()
	{
		if (Networking.LocalPlayer.isMaster)
		{
			debugConsole.Message($"{playerName} wants owner of chunk ({intArray[0] >> 4}, {intArray[2] >> 4})");
			var pos = new Vector2Int(intArray[0] >> 4, intArray[2] >> 4);
			if (!worldData.HasOwner(pos))
				networkManager.PublicOwner(pos.x, pos.y, playerName);
		}
		SetBlock();
	}
	#endregion
	#region fix back
	public void FixBackSignal(Vector3Int pos, int oldTo, int fix, string playerName)
	{
		Reset();
		type = (int)SignalType.FixBack;
		intArray = new int[] { pos.x, pos.y, pos.z, oldTo, fix };
		stringData = playerName;
		RequestSerialization();
	}

	public void FixBackSignal(Vector3Int[] fixPositions, int[] fixOldToBlocks, int[] fixBlocks, string[] fixOwners)
	{
		Reset();
		type = (int)SignalType.FixBack;
		stringArray = fixOwners;
		intArray = new int[fixPositions.Length * 5];
		for (int i = 0; i < fixPositions.Length; i++)
		{
			intArray[i * 5] = fixPositions[i].x;
			intArray[i * 5 + 1] = fixPositions[i].y;
			intArray[i * 5 + 2] = fixPositions[i].z;
			intArray[i * 5 + 3] = fixOldToBlocks[i];
			intArray[i * 5 + 4] = fixBlocks[i];
		}
		RequestSerialization();
	}

	private void FixBack()
	{
		if (stringArray.Length != 0)
		{
			var fixPositions = new Vector3Int[stringArray.Length];
			var fixOldToBlocks = new int[stringArray.Length];
			var fixBlocks = new int[stringArray.Length];
			for (int i = 0; i < stringArray.Length; i++)
			{
				fixPositions[i] = new Vector3Int(intArray[i * 5], intArray[i * 5 + 1], intArray[i * 5 + 2]);
				fixOldToBlocks[i] = intArray[i * 5 + 3];
				fixBlocks[i] = intArray[i * 5 + 4];
			}

			worldController.FixBack(fixPositions, fixOldToBlocks, fixBlocks, stringArray);

			return;
		}

		var pos = new Vector3Int(intArray[0], intArray[1], intArray[2]);
		var oldTo = intArray[3];
		var fix = intArray[4];
		var playerName = stringData;

		worldController.FixBack(pos, oldTo, fix, playerName);
	}

	#endregion
	#region global request
	public void GlobalRequestSignal(Vector2Int[] wantChunks, Vector2Int[] requestedChunks, string[] requestedOwners, Vector2Int[] discardedChunks, string[] discardedData)
	{
		Reset();
		type = (int)SignalType.GlobalRequest;
		if (discardedChunks == null)
		{
			discardedChunks = new Vector2Int[0];
			discardedData = new string[0];
		}

		intData = requestedChunks.Length;
		stringArray = new string[discardedData.Length + requestedOwners.Length];
		Array.Copy(discardedData, 0, stringArray, 0, discardedData.Length);
		Array.Copy(requestedOwners, 0, stringArray, discardedData.Length, requestedOwners.Length);
		intArray = new int[(wantChunks.Length + requestedChunks.Length + discardedChunks.Length) * 2];
		var counter = 0;
		//wantChunks
		for (int i = 0; i < wantChunks.Length; i++)
		{
			intArray[counter++] = wantChunks[i].x;
			intArray[counter++] = wantChunks[i].y;
		}
		//requestedChunks
		for (int i = 0; i < requestedChunks.Length; i++)
		{
			intArray[counter++] = requestedChunks[i].x;
			intArray[counter++] = requestedChunks[i].y;
		}
		//discardedData
		for (int i = 0; i < discardedChunks.Length; i++)
		{
			intArray[counter++] = discardedChunks[i].x;
			intArray[counter++] = discardedChunks[i].y;
		}

		RequestSerialization();
	}

	public void GlobalRequest()
	{
		debugConsole.Message($"{playerName} is requesting {intData} chunks");
		var counter = 0;

		int discardedCount = stringArray.Length - intData;
		int wantCount = intArray.Length / 2 - stringArray.Length;

		var wantChunks = new Vector2Int[wantCount];
		for (int i = 0; i < wantCount; i++)
			wantChunks[i] = new Vector2Int(intArray[counter++], intArray[counter++]);

		var requestedChunks = new Vector2Int[intData];
		for (int i = 0; i < intData; i++)
			requestedChunks[i] = new Vector2Int(intArray[counter++], intArray[counter++]);

		var discardedChunks = new Vector2Int[discardedCount];
		for (int i = 0; i < discardedCount; i++)
			discardedChunks[i] = new Vector2Int(intArray[counter++], intArray[counter++]);

		var requestedOwners = new string[intData];
		var discardedData = new string[discardedCount];

		Array.Copy(stringArray, 0, requestedOwners, 0, intData);
		Array.Copy(stringArray, intData, discardedData, 0, discardedCount);

		worldController.GlobalRequest(wantChunks, requestedChunks, requestedOwners, discardedData, discardedChunks, playerName);
	}

	#endregion
	#region answer to global request
	public void AnswerGlobalRequestSignal(string[] requestedData, Vector2Int[] requestedPositions, string playerName)
	{
		Reset();
		type = (int)SignalType.AnswerGlobalRequest;
		stringData = playerName;
		stringArray = requestedData;
		intArray = new int[requestedPositions.Length * 2];
		var counter = 0;
		for (int i = 0; i < requestedPositions.Length; i++)
		{
			intArray[counter++] = requestedPositions[i].x;
			intArray[counter++] = requestedPositions[i].y;
		}
		RequestSerialization();
	}

	public void AnswerGlobalRequest()
	{
		if (stringData != Networking.LocalPlayer.displayName) return;
		debugConsole.Message($"{playerName} answered your global request with {stringArray.Length} chunks");
		worldController.AnswerGlobalRequest(stringArray, intArray);
	}
	#endregion
}

public enum SignalType
{
	SetBlock,
	WantOwner,
	PublicOwner,
	FixBack,
	GlobalRequest,
	WantsOwner,
	StartData,
	AnswerGlobalRequest
}