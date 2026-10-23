using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC_MINE.Data;
namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class InvSystem : UdonSharpBehaviour
	{
		[SerializeField] private PerSystem perSystemSample;
		[SerializeField] private InvCell[] pcInv;
		[SerializeField] private InvCell[] vrInv;
		[SerializeField] private InvCell[] pcArm;
		[SerializeField] private InvCell[] vrArm;
		[SerializeField] private InvCell[] pcDesk;
		[SerializeField] private InvCell[] vrDesk;
		private InvCell[] desk;
		[SerializeField] private InvCell pcCraft;
		[SerializeField] private InvCell vrCraft;
		[SerializeField] private RectTransform pcSelectRect;
		[SerializeField] private RectTransform vrSelectRect;
		[SerializeField] private TextMeshProUGUI pcSelectText;
		[SerializeField] private TextMeshProUGUI vrSelectText;
		[SerializeField] private GameObject pcNameObject;
		[SerializeField] private TextMeshProUGUI pcNameText;
		[SerializeField] private GameObject vrNameObject;
		[SerializeField] private TextMeshProUGUI vrNameText;
		GameObject nameObject;
		private TextMeshProUGUI nameText;
		private RectTransform selectRect;
		private TextMeshProUGUI selectText;
		private InvCell enteredCell;
		private InvCell selectedCell;
		private void Start()
		{
			isUserInVR = Networking.LocalPlayer.IsUserInVR();
			selectRect = isUserInVR ? vrSelectRect : pcSelectRect;
			selectText = isUserInVR ? vrSelectText : pcSelectText;
			nameObject = isUserInVR ? vrNameObject : pcNameObject;
			nameText = isUserInVR ? vrNameText : pcNameText;
			var inv = isUserInVR ? vrInv : pcInv;
			var arm = isUserInVR ? vrArm : pcArm;
			desk = isUserInVR ? vrDesk : pcDesk;
			var craft = isUserInVR ? vrCraft : pcCraft;
			//build stack + arm + hot pull
			foreach (var cell in inv)
			{
				cell.SetDesk(desk);
				cell.SetArm(arm);
				cell.SetInv(inv);
			}
			foreach (var cell in desk)
			{
				cell.SetDesk(desk);
				cell.SetArm(arm);
				cell.SetInv(inv);
			}
			//craft
			craft.SetInv(inv);
			selectRect.gameObject.SetActive(false);
			nameObject.SetActive(false);
		}
		public void ExitCell(InvCell cell)
		{
			if (enteredCell == cell)
			{
				enteredCell = null;
				nameObject.SetActive(false);
			}
		}

		public void EnterCell(InvCell cell, string name)
		{
			enteredCell = cell;
			if (dropStarted || dropStarted1) selectText.text = selectedCell.TryDropTo(enteredCell, selectText.text);
			if (name != null)
			{
				nameObject.transform.localPosition = cell.transform.localPosition + Vector3.right * 8;
				nameObject.SetActive(true);
				nameText.text = name;
			}
		}

		private bool isUserInVR;
		private bool dropStarted = false;
		private bool dropStarted1 = false;
		public override void InputUse(bool value, UdonInputEventArgs args)
		{
			if (isUserInVR && args.handType == HandType.LEFT) return;
			LMB(value);
		}

		public override void InputDrop(bool value, UdonInputEventArgs args)
		{
			if (isUserInVR) return;
			RMB(value);
		}

		public override void InputGrab(bool value, UdonInputEventArgs args)
		{
			if (!isUserInVR) return;
			if (args.handType == HandType.LEFT) return;
			RMB(value);
		}

		private void LMB(bool value)
		{
			if (value)
			{
				if (selectedCell == null)
				{
					if (enteredCell != null)
					{
						var selected = enteredCell.TrySelect();
						if (selected != null)
						{
							selectedCell = enteredCell;
							selectRect.gameObject.SetActive(true);
							selectRect.localPosition = selectedCell.transform.localPosition;
							selectText.text = selected;
						}
					}
				}
				else if (!dropStarted1)
				{
					dropStarted = true;
					selectedCell.SetDropType(true);
					if (enteredCell != null) selectText.text = selectedCell.TryDropTo(enteredCell, selectText.text);
				}
			}
			else
			{
				if (dropStarted)
				{
					selectText.text = selectedCell.TryDrop(selectText.text, enteredCell);
					dropStarted = false;
					if (selectText.text == "")
					{
						selectedCell = null;
						selectRect.gameObject.SetActive(false);
					}
				}
			}
		}

		private void RMB(bool value)
		{
			if (value)
			{
				if (selectedCell == null)
				{
					if (enteredCell != null)
					{
						var selected = enteredCell.TrySelectHalf();
						if (selected != null)
						{
							selectedCell = enteredCell;
							selectRect.gameObject.SetActive(true);
							selectRect.localPosition = selectedCell.transform.localPosition;
							selectText.text = selected;
						}
					}
				}
				else if (!dropStarted)
				{
					dropStarted1 = true;
					selectedCell.SetDropType(false);
					if (enteredCell != null) selectText.text = selectedCell.TryDropTo(enteredCell, selectText.text);
				}
			}
			else
			{
				if (dropStarted1)
				{
					selectText.text = selectedCell.TryDrop(selectText.text, enteredCell);
					dropStarted1 = false;
					if (selectText.text == "")
					{
						selectedCell = null;
						selectRect.gameObject.SetActive(false);
					}
				}
			}
		}

		public void Close()
		{
			if (selectedCell == null) return;
			selectRect.gameObject.SetActive(false);
			nameObject.SetActive(false);
			if (dropStarted || dropStarted1) selectedCell.TryDrop(selectText.text, null);
			dropStarted = false;
			dropStarted1 = false;
			selectedCell.SetDropType(true);
			selectedCell.TryDropTo(selectedCell, selectText.text);
			selectedCell.TryDrop(selectText.text, null);
			selectedCell = null;
		}
	}
}
