
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

public class Inventory : UdonSharpBehaviour
{
	[SerializeField] private CellController[] cells;
	[SerializeField] private CellController[] arms;
	[SerializeField] private CellController[] craftingCells;
	[SerializeField] private CellController crafted;
	private VRCPlayerApi localPlayer;
	[SerializeField] private Transform order;
	[SerializeField] private NetworkManager net;
	[SerializeField] private WorldController worldController;

	public Vector3 OrderOffset
	{
		get
		{
			if (inVR)
				return order.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
			else
				return order.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
		}
	}

	[SerializeField] private Transform used;
	[SerializeField] private CellController usedCell;

	[SerializeField] private DebugConsole debugConsole;
	public int usedID = 0;

	private DateTime lastClickTime = DateTime.MinValue;

	private CellController selected;

	private bool inVR;
	private bool scrolled = false;

	public void Deselect()
	{
		if (selected == null) return;
		selected.Shuffle(selected);
		selected = null;
	}

	public int TryGive(int id, int count)
	{
		foreach (CellController cell in cells)
		{
			count = cell.TryGive(id, count);
			if (count == 0) return 0;
		}
		return count;
	}

	public void TryGive(Vector3 pos, CellController inputCell)
	{
		foreach (CellController cell in cells)
		{
			var offset = cell.transform.localPosition - pos;
			if (Mathf.Abs(offset.x) < 9 && Mathf.Abs(offset.y) < 9)
			{
				cell.TryGive(inputCell);
				return;
			}
		}
	}

	private void Start()
	{
		localPlayer = Networking.LocalPlayer;
		inVR = localPlayer.IsUserInVR();
		for (var i = 1; i < 27; i++)
		{
			TryGive(i, 64);
		}
	}

	public bool Click(CellController cell)
	{
		if (cell == selected && lastClickTime.AddSeconds(0.5) > DateTime.Now)
		{
			foreach (var c in cells)
			{
				if (c == cell) continue;
				cell.TryGive(c);
			}
			selected = null;
			lastClickTime = DateTime.MinValue;
			return false;
		}

		lastClickTime = DateTime.Now;

		if (selected == null)
		{
			selected = cell;
			return true;
		}

		selected.Shuffle(cell);
		selected = null;
		return false;
	}

	public void TryClick(CellController cell)
	{

		if (selected == null) return;

		selected.Shuffle(cell);
		selected = null;
	}

	public void TryShuffle(Vector3 pos, CellController inputCell)
	{
		foreach (var cell in cells)
		{
			var offset = cell.transform.localPosition - pos;
			if (Mathf.Abs(offset.x) < 9 && Mathf.Abs(offset.y) < 9)
			{
				inputCell.Shuffle(cell);
				selected = null;
				return;
			}
		}
	}
	public bool TryOne(Vector3 pos, int id)
	{
		foreach (var cell in cells)
		{
			var offset = cell.transform.localPosition - pos;
			if (Mathf.Abs(offset.x) < 9 && Mathf.Abs(offset.y) < 9)
			{
				return cell.TryGive(id, 1) == 0;
			}
		}

		return false;
	}

	public void ChangeUsed(int id)
	{
		usedID = id;
		used.localPosition = new Vector3(usedID * 18 - 72, 0, 0);
	}

	private void Update()
	{
		usedCell.Sync(cells[usedID]);
		used.localPosition = new Vector3(usedID * 18 - 72, 0, 0);

		if (inVR) return;
		if (Input.GetKeyDown(KeyCode.Alpha1)) usedID = 0;
		if (Input.GetKeyDown(KeyCode.Alpha2)) usedID = 1;
		if (Input.GetKeyDown(KeyCode.Alpha3)) usedID = 2;
		if (Input.GetKeyDown(KeyCode.Alpha4)) usedID = 3;
		if (Input.GetKeyDown(KeyCode.Alpha5)) usedID = 4;
		if (Input.GetKeyDown(KeyCode.Alpha6)) usedID = 5;
		if (Input.GetKeyDown(KeyCode.Alpha7)) usedID = 6;
		if (Input.GetKeyDown(KeyCode.Alpha8)) usedID = 7;
		if (Input.GetKeyDown(KeyCode.Alpha9)) usedID = 8;

		var scroll = Input.GetAxisRaw("Mouse ScrollWheel");

		if (scroll != 0)
		{
			usedID -= (int)(scroll * 10);
			usedID = (usedID + 9) % 9;
		}
	}

	public override void InputLookVertical(float value, UdonInputEventArgs args)
	{
		if (!inVR) return;

		if (scrolled)
		{
			if (value == 0) scrolled = false;
			return;
		}

		if (value > 0.8f)
		{
			usedID++;
			usedID = (usedID + 9) % 9;
			used.localPosition = new Vector3(usedID * 18 - 72, 0, 0);
			scrolled = true;
		}
		else if (value < -0.8f)
		{
			usedID--;
			usedID = (usedID + 9) % 9;
			used.localPosition = new Vector3(usedID * 18 - 72, 0, 0);
			scrolled = true;
		}
	}

	public void ClickBlock(Vector3Int selected, Vector3Int air, bool set)
	{
		var trying = cells[usedID].TryClickBlock(selected, air, set);
		if (trying == null) return;
		var pos = trying[0] == (int)SetPosition.selected ? selected : air;
		worldController.SetBlockLocal(pos, trying[1]);
	}
}