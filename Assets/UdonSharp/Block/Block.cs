
using System;
using UdonSharp;
using UnityEngine;

public class Block : UdonSharpBehaviour
{
	[SerializeField]
	private char id;
	[SerializeField]
	private int index;
	[SerializeField]
	private BlockVisualType visualType;
	[SerializeField]
	private GameObject icon;
	[SerializeField]
	private GameObject block;

	public char GetID() => id;
	public int GetIndex() => index;
	public BlockVisualType GetVisualType() => visualType;
	public GameObject GetIcon() => icon;
	public GameObject GetBlock() => block;
}

[Serializable]
public enum BlockVisualType
{
	full,
	transparent,
	noFull
}

[Serializable]
public enum BlockType
{
	stone,
	grass
}
