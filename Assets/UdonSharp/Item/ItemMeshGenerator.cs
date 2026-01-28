using UdonSharp;
using UnityEngine;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ItemMeshGenerator : UdonSharpBehaviour
{
	private Mesh[] meshes = new Mesh[256];

	public Mesh GetMesh(int id)
	{
		if (meshes[id] != null)
		{
			return meshes[id];
		}

		//id as color red in vertices color
		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[8];
		vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
		vertices[1] = new Vector3(0.5f, -0.5f, -0.5f);
		vertices[2] = new Vector3(0.5f, 0.5f, -0.5f);
		vertices[3] = new Vector3(-0.5f, 0.5f, -0.5f);
		vertices[4] = new Vector3(-0.5f, -0.5f, 0.5f);
		vertices[5] = new Vector3(0.5f, -0.5f, 0.5f);
		vertices[6] = new Vector3(0.5f, 0.5f, 0.5f);
		vertices[7] = new Vector3(-0.5f, 0.5f, 0.5f);
		mesh.vertices = vertices;
		int[] triangles = new int[]
		{
			0, 2, 1, 0, 3, 2,
			1, 2, 6, 6, 5, 1,
			4, 5, 6, 6, 7, 4,
			2, 3, 7, 7, 6, 2,
			0, 7, 3, 0, 4, 7,
			0, 1, 5, 0, 5, 4
		};
		mesh.triangles = triangles;
		Color32[] colors = new Color32[8];
		for (int i = 0; i < 8; i++)
		{
			colors[i] = new Color32((byte)Mathf.RoundToInt((vertices[i].x + 0.5f) * 255), (byte)Mathf.RoundToInt((vertices[i].y + 0.5f) * 255), (byte)Mathf.RoundToInt((vertices[i].z + 0.5f) * 255), (byte)id);
		}
		mesh.colors32 = colors;
		mesh.RecalculateNormals();
		mesh.name = "IndexCube_" + id;
		meshes[id] = mesh;
		return mesh;
	}
}
