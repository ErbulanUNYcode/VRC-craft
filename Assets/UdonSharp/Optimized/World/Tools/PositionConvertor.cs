
using TMPro;
using UdonSharp;
using UnityEngine;

public class PositionConvertor : UdonSharpBehaviour
{
	[SerializeField] TextMeshProUGUI text;
	private void Update()
	{
		var pos = Vector3Int.FloorToInt(transform.position);
		var chunk = new Vector2Int(pos.x >> 4, pos.z >> 4);
		var index = (pos.x & 15) + ((pos.z & 15) << 4) + (pos.y << 8);
		text.text = $"pos: {pos}\nchunk: {chunk}\nindex: {index}";
	}
}
