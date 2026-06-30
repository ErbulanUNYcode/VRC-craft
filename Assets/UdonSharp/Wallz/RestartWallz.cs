using UdonSharp;
using UnityEngine;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RestartWallz : UdonSharpBehaviour
{
	[SerializeField] private Wallz wallz;
	public override void Interact()
	{
		wallz._GlobalReset();
	}
}
