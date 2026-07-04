
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class CamTest : UdonSharpBehaviour
{
	[SerializeField] private Camera testCam;
	[SerializeField] private Toggle[] layers;

	private void Start()
	{
		for (int i = 0; i < 32; i++)
		{
			var layerName = LayerMask.LayerToName(i);
			if (layerName == "") layerName = "NoName";
			layerName = i + " - " + layerName;
			layers[i].GetComponentInChildren<Text>().text = layerName;
			layers[i].isOn = testCam.cullingMask == (testCam.cullingMask | (1 << i));
		}
	}

	private void ChangeLayer(int layer)
	{
		var value = layers[layer].isOn;
		Debug.Log("Layer " + layer + " " + value);
		testCam.cullingMask = value ? (testCam.cullingMask | (1 << layer)) : (testCam.cullingMask & ~(1 << layer));
	}

	public void CheckLayer0() { ChangeLayer(0); }
	public void CheckLayer1() { ChangeLayer(1); }
	public void CheckLayer2() { ChangeLayer(2); }
	public void CheckLayer3() { ChangeLayer(3); }
	public void CheckLayer4() { ChangeLayer(4); }
	public void CheckLayer5() { ChangeLayer(5); }
	public void CheckLayer6() { ChangeLayer(6); }
	public void CheckLayer7() { ChangeLayer(7); }
	public void CheckLayer8() { ChangeLayer(8); }
	public void CheckLayer9() { ChangeLayer(9); }
	public void CheckLayer10() { ChangeLayer(10); }
	public void CheckLayer11() { ChangeLayer(11); }
	public void CheckLayer12() { ChangeLayer(12); }
	public void CheckLayer13() { ChangeLayer(13); }
	public void CheckLayer14() { ChangeLayer(14); }
	public void CheckLayer15() { ChangeLayer(15); }
	public void CheckLayer16() { ChangeLayer(16); }
	public void CheckLayer17() { ChangeLayer(17); }
	public void CheckLayer18() { ChangeLayer(18); }
	public void CheckLayer19() { ChangeLayer(19); }
	public void CheckLayer20() { ChangeLayer(20); }
	public void CheckLayer21() { ChangeLayer(21); }
	public void CheckLayer22() { ChangeLayer(22); }
	public void CheckLayer23() { ChangeLayer(23); }
	public void CheckLayer24() { ChangeLayer(24); }
	public void CheckLayer25() { ChangeLayer(25); }
	public void CheckLayer26() { ChangeLayer(26); }
	public void CheckLayer27() { ChangeLayer(27); }
	public void CheckLayer28() { ChangeLayer(28); }
	public void CheckLayer29() { ChangeLayer(29); }
	public void CheckLayer30() { ChangeLayer(30); }
	public void CheckLayer31() { ChangeLayer(31); }
}
