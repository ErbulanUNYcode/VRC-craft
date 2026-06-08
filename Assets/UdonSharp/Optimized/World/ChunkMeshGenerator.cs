using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ChunkMeshGenerator : UdonSharpBehaviour
{
	private Mesh[] meshes = new Mesh[9];

	public Mesh GetMesh(int type)
	{
		var m = meshes[type];

		if (m == null)
		{


			m.vertices = p;
			m.triangles = t;
			m.colors = c;
		}

		return m;
	}

	private Mesh CreateType1()
	{
		var m = new Mesh();
		var q = 385;
		var p = new Vector3[q * 4];
		var c = new Color32[q * 4];
		var uv = new Vector2[q * 4];
		var t = new int[q * 6];
		var id = 0;
		for (var i = 0; i < 32; i++)
		{
			for (var j = 0; j < 128; j += 32)
			{
				var id4 = id * 4;
				p[id4] = new Vector3(i, j, 0);
				c[id4] = new Color32(0
			}
		}


		return m;
	}

}
