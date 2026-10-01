using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC_MINE.UI;

public class InvSystem : UdonSharpBehaviour
{
	[SerializeField] private RectTransform pcSelectRect;
	[SerializeField] private RectTransform vrSelectRect;
	private RectTransform selectRect;
	private InvCell selectedCell;
	private void Awake()
	{
		selectRect = Networking.LocalPlayer.IsUserInVR() ? vrSelectRect : pcSelectRect;
	}
	public void DeselectedCell(InvCell invCell)
	{
		if (selectedCell == invCell) selectedCell = null;
	}

	public void SelectedCell(InvCell invCell)
	{
		selectedCell = invCell;
		selectedCell.Select(selectRect);
	}
}
