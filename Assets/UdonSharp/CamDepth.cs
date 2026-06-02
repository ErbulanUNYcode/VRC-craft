
using UdonSharp;
using UnityEngine;

public class CamDepth : UdonSharpBehaviour
{
	[SerializeField] private Camera cam;
	[SerializeField] private Shader depthShader;
	void Start()
	{
		cam.SetReplacementShader(depthShader, "");
	}
}
