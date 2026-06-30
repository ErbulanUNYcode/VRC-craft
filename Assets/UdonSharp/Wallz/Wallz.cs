using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Wallz : UdonSharpBehaviour
{
	[SerializeField] Transform player1Zone;
	[SerializeField] Transform player2Zone;
	[SerializeField] AudioSource click;
	[SerializeField] AudioSource win;
	[SerializeField] private GameObject resetButton1;
	[SerializeField] private GameObject resetButton2;
	[SerializeField] private Canvas canvas;
	[SerializeField] private TextMeshProUGUI player1Text;
	[SerializeField] private TextMeshProUGUI player2Text;

	[SerializeField] private Transform ghostChip;
	private VRCPickup currentPickup;
	[SerializeField] private VRCPickup chip1;
	[SerializeField] private VRCPickup chip2;
	[SerializeField] private Transform ghostWall;
	[SerializeField] private VRCPickup[] walls1;
	[SerializeField] private VRCPickup[] walls2;
	[SerializeField] private Vector3 walls1DefaultPos;
	[SerializeField] private Vector3 walls2DefaultPos;

	private int currentWall1 = 0;
	private int currentWall2 = 0;
	[UdonSynced] private bool player1joined = false;
	[UdonSynced] private bool player2joined = false;
	[UdonSynced] private bool turn = false;//false = player1, true = player2
	[UdonSynced] private bool endGame = false;

	private VRCPlayerApi localPlayer;

	bool u1, d1, r1, l1, u2, d2, r2, l2;
	bool desktopTurn;

	public void _GlobalReset()
	{
		Networking.SetOwner(localPlayer, gameObject);
		ResetGame();
	}

	VRCTweenHandle tween;
	private void EndGame()
	{
		if (Networking.IsOwner(gameObject)) endGame = true;
		chip1.pickupable = false;
		chip2.pickupable = false;
		if (currentWall1 < walls1.Length) walls1[currentWall1].GetComponent<Collider>().enabled = false;
		if (currentWall2 < walls2.Length) walls2[currentWall2].GetComponent<Collider>().enabled = false;
		resetButton1.gameObject.SetActive(false);
		resetButton2.gameObject.SetActive(false);
		win.Play();
		if (turn)
		{
			VRCTween.TweenLocalPosition(player2Zone, new Vector3(0, 0.01f, 0.444f), 0.8f, VRCTweenEase.Linear);
			VRCTween.TweenScale(player2Zone, new Vector3(0.884f, 0.014f, 0), 0.8f, VRCTweenEase.Linear);
			VRCTween.TweenLocalPosition(player1Zone, Vector3.up * 0.01f, 0.8f, VRCTweenEase.Linear);
			tween = VRCTween.TweenScale(player1Zone, new Vector3(0.884f, 0.014f, 0.884f), 0.8f, VRCTweenEase.Linear);
			tween.OnComplete(this, nameof(_OnGameEnded));
		}
		else
		{
			VRCTween.TweenLocalPosition(player1Zone, new Vector3(0, 0.01f, -0.444f), 0.8f, VRCTweenEase.Linear);
			VRCTween.TweenScale(player1Zone, new Vector3(0.884f, 0.014f, 0), 0.8f, VRCTweenEase.Linear);
			VRCTween.TweenLocalPosition(player2Zone, Vector3.up * 0.01f, 0.8f, VRCTweenEase.Linear);
			tween = VRCTween.TweenScale(player2Zone, new Vector3(0.884f, 0.014f, 0.884f), 0.8f, VRCTweenEase.Linear);
			tween.OnComplete(this, nameof(_OnGameEnded));
		}
	}

	public void _OnGameEnded()
	{
		Debug.Log("Game ended");
		resetButton1.gameObject.SetActive(true);
		resetButton2.gameObject.SetActive(true);
	}

	private void ResetGame()
	{
		player1Zone.localScale = new Vector3(0.884f, 0.014f, 0.084f);
		player2Zone.localScale = new Vector3(0.884f, 0.014f, 0.084f);
		player1Zone.localPosition = new Vector3(0, 0.01f, -0.4f);
		player2Zone.localPosition = new Vector3(0, 0.01f, 0.4f);

		endGame = false;
		resetButton1.SetActive(false);
		resetButton2.SetActive(false);
		up = new bool[81];
		down = new bool[81];
		right = new bool[81];
		left = new bool[81];

		for (int i = 72; i < 81; i++) up[i] = true;
		for (int i = 0; i < 9; i++) down[i] = true;
		for (int i = 0; i < 81; i += 9) left[i] = true;
		for (int i = 8; i < 81; i += 9) right[i] = true;

		horizontal = new bool[64];
		vertical = new bool[64];

		for (int i = 0; i < walls1.Length; i++)
		{
			var wall = walls1[i];
			wall.Drop();
			wall.GetComponent<Collider>().enabled = false;
			wall.transform.localPosition = walls1DefaultPos + Vector3.forward / 2 * i;
			wall.transform.localEulerAngles = new Vector3(0, 45, 0);
		}
		chip1.Drop();
		chip1.pickupable = true;
		walls1[0].GetComponent<Collider>().enabled = true;

		for (int i = 0; i < walls2.Length; i++)
		{
			var wall = walls2[i];
			wall.Drop();
			wall.GetComponent<Collider>().enabled = false;
			wall.transform.localPosition = walls2DefaultPos - Vector3.forward / 2 * i;
			wall.transform.localEulerAngles = new Vector3(0, 45, 0);
		}
		chip2.Drop();
		chip2.pickupable = false;

		ghostChip.gameObject.SetActive(false);
		ghostWall.gameObject.SetActive(false);

		chip1.transform.localPosition = new Vector3(4, 0, 0);
		chip2.transform.localPosition = new Vector3(4, 0, 8);
		p1 = new Vector2Int(4, 0);
		p2 = new Vector2Int(4, 8);
		currentWall1 = 0;
		currentWall2 = 0;

		if (Networking.IsOwner(gameObject))
		{
			player1joined = false;
			player2joined = false;
			turn = false;
			RequestSerialization();
		}
	}

	private void Start()
	{
		localPlayer = Networking.LocalPlayer;
		if (localPlayer.IsUserInVR())
		{
			foreach (var wall in walls1) wall.ExactGrip = null;
			foreach (var wall in walls2) wall.ExactGrip = null;
		}
		ResetGame();
	}

	private Vector3 RayToPlane(Vector3 origin, Vector3 dir)
	{
		if (origin.y <= 0f || dir.y >= -0.0001f)
			return new Vector3(-2f, 0f, -2f);

		float t = -origin.y / dir.y;

		return new Vector3(
			origin.x + dir.x * t,
			0f,
			origin.z + dir.z * t
		);
	}

	private void Update()
	{
		if (!localPlayer.IsUserInVR() && Input.GetAxisRaw("Mouse ScrollWheel") != 0) desktopTurn = !desktopTurn;

		player1Text.text = player1joined ? Networking.GetOwner(chip1.gameObject).displayName : "Waiting for player 1";
		player2Text.text = player2joined ? Networking.GetOwner(chip2.gameObject).displayName : "Waiting for player 2";

		if (chip1.IsHeld)
		{
			var w = walls1[0];
			if (w.IsHeld && w.currentPlayer == localPlayer)
			{
				w.Drop();
				w.transform.localPosition = walls1DefaultPos;
			}
		}

		if (chip2.IsHeld)
		{
			var w = walls2[0];
			if (w.IsHeld && w.currentPlayer == localPlayer)
			{
				w.Drop();
				w.transform.localPosition = walls2DefaultPos;
			}
		}

		if (currentPickup == null)
		{
			if (chip1.IsHeld)
			{
				WallzOnPickup(chip1);
				currentPickup = chip1;
			}
			else if (chip2.IsHeld)
			{
				WallzOnPickup(chip2);
				currentPickup = chip2;
			}
			else if (currentWall1 < walls1.Length && walls1[currentWall1].IsHeld)
			{
				WallzOnPickup(walls1[currentWall1]);
				currentPickup = walls1[currentWall1];
			}
			else if (currentWall2 < walls2.Length && walls2[currentWall2].IsHeld)
			{
				WallzOnPickup(walls2[currentWall2]);
				currentPickup = walls2[currentWall2];
			}
		}
		else
		{
			if (!currentPickup.IsHeld)
			{
				WallzOnDrop(currentPickup);
				currentPickup = null;
			}
		}

		if (chip1.IsHeld && chip1.currentPlayer == localPlayer)
		{
			Vector3Int p;
			if (localPlayer.IsUserInVR())
			{
				p = Vector3Int.RoundToInt(chip1.transform.localPosition);
			}
			else
			{
				var data = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
				var parent = chip1.transform.parent;
				p = Vector3Int.RoundToInt(RayToPlane(parent.InverseTransformPoint(data.position), parent.InverseTransformDirection(data.rotation * Vector3.forward)));
			}

			p.y = p.z;
			ghostChip.localPosition = new Vector3(p.x, 0, p.y);

			if (p.x < 0 || p.x > 8 || p.y < 0 || p.y > 8 || (Vector2Int)p == p1 || (Vector2Int)p == p2) ghostChip.gameObject.SetActive(false);
			else
			{
				ghostChip.gameObject.SetActive
				(
					(p.x == p1.x && ((p.y == p1.y + 1 && u1) || (p.y == p1.y - 1 && d1))) ||
					(p.y == p1.y && ((p.x == p1.x + 1 && r1) || (p.x == p1.x - 1 && l1))) ||
					(
						(
							(p1.x == p2.x && ((p2.y == p1.y + 1 && u1) || (p2.y == p1.y - 1 && d1))) ||
							(p1.y == p2.y && ((p2.x == p1.x + 1 && r1) || (p2.x == p1.x - 1 && l1)))
						) &&
						(
							(p.x == p2.x && ((p.y == p2.y + 1 && u2) || (p.y == p2.y - 1 && d2))) ||
							(p.y == p2.y && ((p.x == p2.x + 1 && r2) || (p.x == p2.x - 1 && l2)))
						)
					)
				);
				return;
			}
		}


		if (chip2.IsHeld && chip2.currentPlayer == localPlayer)
		{
			Vector3Int p;
			if (localPlayer.IsUserInVR())
			{
				p = Vector3Int.RoundToInt(chip2.transform.localPosition);
			}
			else
			{
				var data = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
				var parent = chip2.transform.parent;
				p = Vector3Int.RoundToInt(RayToPlane(parent.InverseTransformPoint(data.position), parent.InverseTransformDirection(data.rotation * Vector3.forward)));
			}

			p.y = p.z;
			ghostChip.localPosition = new Vector3(p.x, 0, p.y);
			if (p.x < 0 || p.x > 8 || p.y < 0 || p.y > 8 || (Vector2Int)p == p1 || (Vector2Int)p == p2) ghostChip.gameObject.SetActive(false);
			else
			{
				if (p.x < 0 || p.x > 8 || p.y < 0 || p.y > 8 || (Vector2Int)p == p1 || (Vector2Int)p == p2) ghostChip.gameObject.SetActive(false);
				else
				{
					ghostChip.gameObject.SetActive
					(
						(p.x == p2.x && ((p.y == p2.y + 1 && u2) || (p.y == p2.y - 1 && d2))) ||
						(p.y == p2.y && ((p.x == p2.x + 1 && r2) || (p.x == p2.x - 1 && l2))) ||
						(
							(
								(p1.x == p2.x && ((p2.y == p1.y + 1 && u1) || (p2.y == p1.y - 1 && d1))) ||
								(p1.y == p2.y && ((p2.x == p1.x + 1 && r1) || (p2.x == p1.x - 1 && l1)))
							) &&
							(
								(p.x == p1.x && ((p.y == p1.y + 1 && u1) || (p.y == p1.y - 1 && d1))) ||
								(p.y == p1.y && ((p.x == p1.x + 1 && r1) || (p.x == p1.x - 1 && l1)))
							)
						)
					);
					return;
				}
			}
		}

		if (currentWall1 < walls1.Length)
		{
			var w = walls1[currentWall1];
			if (w.IsHeld && w.currentPlayer == localPlayer)
			{
				Vector3Int p;
				if (localPlayer.IsUserInVR())
				{
					p = Vector3Int.RoundToInt(w.transform.localPosition);
				}
				else
				{
					var data = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
					var parent = w.transform.parent;
					p = Vector3Int.RoundToInt(RayToPlane(parent.InverseTransformPoint(data.position), parent.InverseTransformDirection(data.rotation * Vector3.forward)));
				}
				p.y = p.z;
				ghostWall.transform.localPosition = new Vector3(p.x, 0, p.y);
				var vec = w.transform.localRotation * Vector3.right;
				var rot = localPlayer.IsUserInVR() ? Mathf.Abs(vec.x) > Mathf.Abs(vec.z) : desktopTurn;
				ghostWall.transform.localRotation = Quaternion.Euler(0, rot ? 0 : 90, 0);
				if (p.x < 0 || p.x > 7 || p.y < 0 || p.y > 7) ghostWall.gameObject.SetActive(false);
				else
				{
					if (rot)
					{
						if (horizontal[p.x + p.y * 8]) ghostWall.gameObject.SetActive(false);
						else ghostWall.gameObject.SetActive(HasPath());
					}
					else
					{
						if (vertical[p.x + p.y * 8]) ghostWall.gameObject.SetActive(false);
						else ghostWall.gameObject.SetActive(HasPath());
					}
				}
			}
		}

		if (currentWall2 < walls2.Length)
		{
			var w = walls2[currentWall2];
			if (w.IsHeld && w.currentPlayer == localPlayer)
			{
				Vector3Int p;
				if (localPlayer.IsUserInVR())
				{
					p = Vector3Int.RoundToInt(w.transform.localPosition);
				}
				else
				{
					var data = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
					var parent = w.transform.parent;
					p = Vector3Int.RoundToInt(RayToPlane(parent.InverseTransformPoint(data.position), parent.InverseTransformDirection(data.rotation * Vector3.forward)));
				}
				p.y = p.z;
				ghostWall.transform.localPosition = new Vector3(p.x, 0, p.y);
				var vec = w.transform.localRotation * Vector3.right;
				var rot = localPlayer.IsUserInVR() ? Mathf.Abs(vec.x) > Mathf.Abs(vec.z) : desktopTurn;
				ghostWall.transform.localRotation = Quaternion.Euler(0, rot ? 0 : 90, 0);
				if (p.x < 0 || p.x > 7 || p.y < 0 || p.y > 7) ghostWall.gameObject.SetActive(false);
				else
				{
					if (rot)
					{
						if (horizontal[p.x + p.y * 8]) ghostWall.gameObject.SetActive(false);
						else ghostWall.gameObject.SetActive(HasPath());
					}
					else
					{
						if (vertical[p.x + p.y * 8]) ghostWall.gameObject.SetActive(false);
						else ghostWall.gameObject.SetActive(HasPath());
					}
				}
			}
		}
	}

	private void WallzOnPickup(VRCPickup pickup)
	{
		if (pickup == chip1 || (currentWall1 < walls1.Length && pickup == walls1[currentWall1]))
		{
			chip1.pickupable = false;
			if (currentWall1 < walls1.Length)
			{
				walls1[currentWall1].GetComponent<Collider>().enabled = false;
			}
			if (pickup.currentPlayer != localPlayer) return;
			desktopTurn = true;
			ReLoad();
			Networking.SetOwner(localPlayer, gameObject);
			if (!player1joined)
			{
				Networking.SetOwner(pickup.currentPlayer, chip1.gameObject);
				foreach (var w in walls1) Networking.SetOwner(pickup.currentPlayer, w.gameObject);
				player1joined = true;
				resetButton1.SetActive(true);
				RequestSerialization();
			}
		}

		if (pickup == chip2 || (currentWall2 < walls2.Length && pickup == walls2[currentWall2]))
		{
			chip2.pickupable = false;
			if (currentWall2 < walls2.Length)
			{
				walls2[currentWall2].GetComponent<Collider>().enabled = false;
			}
			if (pickup.currentPlayer != localPlayer) return;
			desktopTurn = true;
			ReLoad();
			Networking.SetOwner(localPlayer, gameObject);
			if (!player2joined)
			{
				Networking.SetOwner(pickup.currentPlayer, chip2.gameObject);
				foreach (var w in walls2) Networking.SetOwner(pickup.currentPlayer, w.gameObject);
				player2joined = true;
				resetButton2.SetActive(true);
				RequestSerialization();
			}
		}
	}

	private void WallzOnDrop(VRCPickup pickup)
	{
		click.Play();
		if (Networking.GetOwner(pickup.gameObject) != localPlayer) return;

		if (pickup == chip1)
		{
			if (ghostChip.gameObject.activeSelf)
			{
				chip1.transform.localPosition = ghostChip.localPosition;
				chip1.transform.rotation = ghostChip.rotation;
				ghostChip.gameObject.SetActive(false);
				p1 = new Vector2Int(Mathf.RoundToInt(chip1.transform.localPosition.x), Mathf.RoundToInt(chip1.transform.localPosition.z));
				turn = true;
				if (!player2joined || Networking.GetOwner(chip2.gameObject) == localPlayer)
				{
					chip2.pickupable = true;
					if (currentWall2 < walls2.Length)
					{
						walls2[currentWall2].GetComponent<Collider>().enabled = true;
					}
				}
				if (p1.y == 8) EndGame();
				RequestSerialization();
			}
			else
			{
				chip1.transform.localPosition = new Vector3(p1.x, 0, p1.y);
				chip1.transform.rotation = ghostChip.rotation;
				chip1.pickupable = true;
				if (currentWall1 < walls1.Length)
				{
					walls1[currentWall1].GetComponent<Collider>().enabled = true;
				}
			}
		}

		if (pickup == chip2)
		{
			if (ghostChip.gameObject.activeSelf)
			{
				chip2.transform.localPosition = ghostChip.localPosition;
				chip2.transform.rotation = ghostChip.rotation;
				ghostChip.gameObject.SetActive(false);
				p2 = new Vector2Int(Mathf.RoundToInt(chip2.transform.localPosition.x), Mathf.RoundToInt(chip2.transform.localPosition.z));
				turn = false;
				if (Networking.GetOwner(chip1.gameObject) == localPlayer)
				{
					chip1.pickupable = true;
					if (currentWall1 < walls1.Length)
					{
						walls1[currentWall1].GetComponent<Collider>().enabled = true;
					}
				}
				if (p2.y == 0) EndGame();
				RequestSerialization();
			}
			else
			{
				chip2.transform.localPosition = new Vector3(p2.x, 0, p2.y);
				chip2.transform.rotation = ghostChip.rotation;
				chip2.pickupable = true;
				if (currentWall2 < walls2.Length)
				{
					walls2[currentWall2].GetComponent<Collider>().enabled = true;
				}
			}
		}

		if (currentWall1 < walls1.Length)
		{
			var w = walls1[currentWall1];
			if (w == pickup)
			{
				if (ghostWall.gameObject.activeSelf)
				{
					w.transform.localPosition = ghostWall.localPosition;
					w.transform.localRotation = ghostWall.localRotation;
					ghostWall.gameObject.SetActive(false);
					currentWall1++;
					for (int i = currentWall1; i < walls1.Length; i++)
						VRCTween.TweenLocalPosition(walls1[i].gameObject, walls1DefaultPos + Vector3.forward / 2 * (i - currentWall1), 0.3f, VRCTweenEase.Linear);
					turn = true;
					if (!player2joined || Networking.GetOwner(chip2.gameObject) == localPlayer)
					{
						chip2.pickupable = true;
						if (currentWall2 < walls2.Length)
						{
							walls2[currentWall2].GetComponent<Collider>().enabled = true;
						}
					}
					RequestSerialization();
				}
				else
				{
					w.transform.localPosition = walls1DefaultPos;
					w.transform.localEulerAngles = new Vector3(0, 45, 0);
					w.GetComponent<Collider>().enabled = true;
					chip1.pickupable = true;
				}
			}
		}

		if (currentWall2 < walls2.Length)
		{
			var w = walls2[currentWall2];
			if (w == pickup)
			{
				if (ghostWall.gameObject.activeSelf)
				{
					w.transform.localPosition = ghostWall.localPosition;
					w.transform.localRotation = ghostWall.localRotation;
					ghostWall.gameObject.SetActive(false);
					currentWall2++;
					for (int i = currentWall2; i < walls2.Length; i++)
						VRCTween.TweenLocalPosition(walls2[i].gameObject, walls2DefaultPos - Vector3.forward / 2 * (i - currentWall2), 0.3f, VRCTweenEase.Linear);
					turn = false;
					if (Networking.GetOwner(chip1.gameObject) == localPlayer)
					{
						chip1.pickupable = true;
						if (currentWall1 < walls1.Length)
						{
							walls1[currentWall1].GetComponent<Collider>().enabled = true;
						}
					}
					RequestSerialization();
				}
				else
				{
					w.transform.localPosition = walls2DefaultPos;
					w.transform.localEulerAngles = new Vector3(0, 45, 0);
					w.GetComponent<Collider>().enabled = true;
					chip2.pickupable = true;
				}
			}
		}
	}

	private Vector2Int p1;
	private Vector2Int p2;

	private bool[] up;
	private bool[] down;
	private bool[] right;
	private bool[] left;
	private bool[] horizontal;
	private bool[] vertical;

	private void ReLoad()
	{
		oldPos = Vector2Int.left * 1000;
		if (!chip1.IsHeld)
			p1 = new Vector2Int(Mathf.RoundToInt(chip1.transform.localPosition.x), Mathf.RoundToInt(chip1.transform.localPosition.z));
		if (!chip2.IsHeld)
			p2 = new Vector2Int(Mathf.RoundToInt(chip2.transform.localPosition.x), Mathf.RoundToInt(chip2.transform.localPosition.z));

		foreach (var wall in walls1)
		{
			if (wall.IsHeld) continue;
			var p = wall.transform.localPosition;
			if (p.x < -0.001 || p.z < -0.001 || p.x > 8.001 || p.z > 8.001) continue;
			var pos = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
			vertical[pos.x + pos.y * 8] = true;
			horizontal[pos.x + pos.y * 8] = true;
			if (Quaternion.Angle(wall.transform.localRotation, Quaternion.identity) < 10)
			{
				if (pos.x > 0) horizontal[pos.x - 1 + pos.y * 8] = true;
				if (pos.x < 7) horizontal[pos.x + 1 + pos.y * 8] = true;
				up[pos.x + pos.y * 9] = true;
				up[pos.x + 1 + pos.y * 9] = true;
				pos.y++;
				down[pos.x + pos.y * 9] = true;
				down[pos.x + 1 + pos.y * 9] = true;
			}
			else
			{
				if (pos.y > 0) vertical[pos.x + (pos.y - 1) * 8] = true;
				if (pos.y < 7) vertical[pos.x + (pos.y + 1) * 8] = true;
				right[pos.x + pos.y * 9] = true;
				right[pos.x + pos.y * 9 + 9] = true;
				pos.x++;
				left[pos.x + pos.y * 9] = true;
				left[pos.x + pos.y * 9 + 9] = true;
			}
		}

		foreach (var wall in walls2)
		{
			if (wall.IsHeld) continue;
			var p = wall.transform.localPosition;
			if (p.x < -0.001 || p.z < -0.001 || p.x > 8.001 || p.z > 8.001) continue;
			var pos = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
			vertical[pos.x + pos.y * 8] = true;
			horizontal[pos.x + pos.y * 8] = true;
			if (Quaternion.Angle(wall.transform.localRotation, Quaternion.identity) < 10)
			{
				if (pos.x > 0) horizontal[pos.x - 1 + pos.y * 8] = true;
				if (pos.x < 7) horizontal[pos.x + 1 + pos.y * 8] = true;
				up[pos.x + pos.y * 9] = true;
				up[pos.x + 1 + pos.y * 9] = true;
				pos.y++;
				down[pos.x + pos.y * 9] = true;
				down[pos.x + 1 + pos.y * 9] = true;
			}
			else
			{
				if (pos.y > 0) vertical[pos.x + (pos.y - 1) * 8] = true;
				if (pos.y < 7) vertical[pos.x + (pos.y + 1) * 8] = true;
				right[pos.x + pos.y * 9] = true;
				right[pos.x + pos.y * 9 + 9] = true;
				pos.x++;
				left[pos.x + pos.y * 9] = true;
				left[pos.x + pos.y * 9 + 9] = true;
			}
		}

		int
			id1 = p1.x + p1.y * 9,
			id2 = p2.x + p2.y * 9;

		u1 = !up[id1];
		d1 = !down[id1];
		r1 = !right[id1];
		l1 = !left[id1];
		u2 = !up[id2];
		d2 = !down[id2];
		r2 = !right[id2];
		l2 = !left[id2];
	}

	public override void OnDeserialization()
	{
		if (endGame)
		{
			EndGame();
			return;
		}
		if (player1joined)
		{
			if (Networking.GetOwner(chip1.gameObject) != localPlayer)
			{
				chip1.pickupable = false;
				walls1[0].GetComponent<Collider>().enabled = false;
			}

			if (turn)
			{
				if (!player2joined || Networking.GetOwner(chip2.gameObject) == localPlayer)
				{
					chip2.pickupable = true;
					if (currentWall2 < walls2.Length)
					{
						walls2[currentWall2].GetComponent<Collider>().enabled = true;
					}
				}
			}
			else
			{
				if (Networking.GetOwner(chip1.gameObject) == localPlayer)
				{
					chip1.pickupable = true;
					if (currentWall1 < walls1.Length)
					{
						walls1[currentWall1].GetComponent<Collider>().enabled = true;
					}
				}
			}

			return;
		}

		ResetGame();
	}

	private int[] stack = new int[72];
	private Vector2Int oldPos;
	private bool oldTurn;
	private bool result;
	private bool HasPath()
	{
		var pos = new Vector2Int(Mathf.RoundToInt(ghostWall.localPosition.x), Mathf.RoundToInt(ghostWall.localPosition.z));
		var turn = Quaternion.Angle(ghostWall.localRotation, Quaternion.identity) < 10;
		if (pos == oldPos && turn == oldTurn) return result;
		oldPos = pos;
		oldTurn = turn;

		PlaceGhostWall();
		result = HasPathToTop() && HasPathToBottom();
		RemoveGhostWall();
		return result;
	}

	private bool HasPathToTop()
	{
		bool[] visited = new bool[81];

		int stackSize = 1;
		int start = p1.x + p1.y * 9;

		stack[0] = start;
		visited[start] = true;

		while (stackSize != 0)
		{
			int index = stack[--stackSize];

			if (index >= 72) return true;

			if (!up[index])
			{
				int next = index + 9;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize++] = next;
				}
			}

			if (!right[index])
			{
				int next = index + 1;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize++] = next;
				}
			}

			if (!left[index])
			{
				int next = index - 1;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize++] = next;
				}
			}

			if (!down[index])
			{
				int next = index - 9;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize++] = next;
				}
			}
		}

		return false;
	}

	private bool HasPathToBottom()
	{
		bool[] visited = new bool[81];

		int stackSize = 1;
		int start = p2.x + p2.y * 9;

		stack[0] = start;
		visited[start] = true;

		while (stackSize != 0)
		{
			int index = stack[--stackSize];

			if (index < 9) return true;

			if (!down[index])
			{
				int next = index - 9;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize] = next;
					stackSize++;
				}
			}

			if (!right[index])
			{
				int next = index + 1;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize] = next;
					stackSize++;
				}
			}

			if (!left[index])
			{
				int next = index - 1;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize] = next;
					stackSize++;
				}
			}

			if (!up[index])
			{
				int next = index + 9;
				if (!visited[next])
				{
					visited[next] = true;
					stack[stackSize] = next;
					stackSize++;
				}
			}
		}

		return false;
	}

	private void PlaceGhostWall()
	{
		if (oldTurn)
		{
			up[oldPos.x + oldPos.y * 9] = true;
			up[oldPos.x + 1 + oldPos.y * 9] = true;
			oldPos.y++;
			down[oldPos.x + oldPos.y * 9] = true;
			down[oldPos.x + 1 + oldPos.y * 9] = true;
			oldPos.y--;
		}
		else
		{
			right[oldPos.x + oldPos.y * 9] = true;
			right[oldPos.x + oldPos.y * 9 + 9] = true;
			oldPos.x++;
			left[oldPos.x + oldPos.y * 9] = true;
			left[oldPos.x + oldPos.y * 9 + 9] = true;
			oldPos.x--;
		}
	}

	private void RemoveGhostWall()
	{
		if (oldTurn)
		{
			up[oldPos.x + oldPos.y * 9] = false;
			up[oldPos.x + 1 + oldPos.y * 9] = false;
			oldPos.y++;
			down[oldPos.x + oldPos.y * 9] = false;
			down[oldPos.x + 1 + oldPos.y * 9] = false;
			oldPos.y--;
		}
		else
		{
			right[oldPos.x + oldPos.y * 9] = false;
			right[oldPos.x + oldPos.y * 9 + 9] = false;
			oldPos.x++;
			left[oldPos.x + oldPos.y * 9] = false;
			left[oldPos.x + oldPos.y * 9 + 9] = false;
			oldPos.x--;
		}
	}

	public override void OnPlayerLeft(VRCPlayerApi player)
	{
		if (endGame) return;
		if (player1joined && Networking.GetOwner(chip1.gameObject) == player) _GlobalReset();
		if (player2joined && Networking.GetOwner(chip2.gameObject) == player) _GlobalReset();
	}
}