using UdonSharp;
using UnityEngine;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class WorldMeshGenetator : UdonSharpBehaviour
{
	[SerializeField] private MeshFilter xMesh;
	[SerializeField] private MeshFilter yMesh;
	[SerializeField] private MeshFilter zMesh;

	void Start()
	{
		//x
		{
			Mesh mesh = new Mesh();
			mesh.name = "WorldMeshX";

			var TileSize = 6;
			var TileSizeFour = TileSize * 4;
			var TileSizeFourFour = TileSizeFour * 4;
			var TileSizeSix = TileSize * 6;
			var TileSizeFourSix = TileSizeFour * 6;
			Vector3[] vertices = new Vector3[TileSizeFourFour * 193];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[TileSizeFourSix * 193];



			for (int i = 0; i <= 192; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					for (int k = 0; k < 6; k++)
					{               //6*4*4                  6*4
						var v0 = i * TileSizeFourFour + j * TileSizeFour + k * 4;
						var v1 = v0 + 1;
						var v2 = v0 + 2;
						var v3 = v0 + 3;
						var offset = new Vector3(i, j * 32, k * 32);
						vertices[v0] = new Vector3(-96f, 32, -64f) + offset;
						vertices[v1] = new Vector3(-96f, 32, -96f) + offset;
						vertices[v2] = new Vector3(-96f, 0, -64f) + offset;
						vertices[v3] = new Vector3(-96f, 0, -96f) + offset;
						colors[v0] = new Color(1, 0, 0, 0);
						colors[v1] = new Color(0, 1, 0, 0);
						colors[v2] = new Color(0, 0, 1, 0);
						colors[v3] = new Color(0, 0, 0, 1);
						var t = i * TileSizeFourSix + j * TileSizeSix + k * 6;
						triangles[t] = v0;
						triangles[t + 1] = v1;
						triangles[t + 2] = v2;
						triangles[t + 3] = v3;
						triangles[t + 4] = v2;
						triangles[t + 5] = v1;
					}
				}
			}

			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.colors = colors;
			mesh.RecalculateNormals();
			xMesh.mesh = mesh;
		}

		//z
		{
			Mesh mesh = new Mesh();
			mesh.name = "WorldMeshX";

			var TileSize = 6;
			var TileSizeFour = TileSize * 4;
			var TileSizeFourFour = TileSizeFour * 4;
			var TileSizeSix = TileSize * 6;
			var TileSizeFourSix = TileSizeFour * 6;
			Vector3[] vertices = new Vector3[TileSizeFourFour * 193];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[TileSizeFourSix * 193];



			for (int i = 0; i <= 192; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					for (int k = 0; k < 6; k++)
					{               //6*4*4                  6*4
						var v0 = i * TileSizeFourFour + j * TileSizeFour + k * 4;
						var v1 = v0 + 1;
						var v2 = v0 + 2;
						var v3 = v0 + 3;
						var offset = new Vector3(k * 32, j * 32, i);
						vertices[v0] = new Vector3(-96f, 32, -96f) + offset;
						vertices[v1] = new Vector3(-64f, 32, -96f) + offset;
						vertices[v2] = new Vector3(-96f, 0, -96f) + offset;
						vertices[v3] = new Vector3(-64f, 0, -96f) + offset;
						colors[v0] = new Color(1, 0, 0, 0);
						colors[v1] = new Color(0, 1, 0, 0);
						colors[v2] = new Color(0, 0, 1, 0);
						colors[v3] = new Color(0, 0, 0, 1);
						var t = i * TileSizeFourSix + j * TileSizeSix + k * 6;
						triangles[t] = v0;
						triangles[t + 1] = v1;
						triangles[t + 2] = v2;
						triangles[t + 3] = v3;
						triangles[t + 4] = v2;
						triangles[t + 5] = v1;
					}
				}
			}

			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.colors = colors;
			mesh.RecalculateNormals();
			zMesh.mesh = mesh;
		}

		//y
		{
			Mesh mesh = new Mesh();
			mesh.name = "WorldMeshY";

			var TileSize = 6;
			var TileSizeFour = TileSize * 4;
			var TileSizeQuad = TileSize * TileSize;
			var TileSizeQuadFour = TileSizeQuad * 4;
			var TileSizeSix = TileSize * 6;
			var TileSizeQuadSix = TileSizeQuad * 6;
			Vector3[] vertices = new Vector3[TileSizeQuadFour * 129];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[TileSizeQuadSix * 129];



			for (int i = 0; i <= 128; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					for (int k = 0; k < 6; k++)
					{
						var v0 = i * TileSizeQuadFour + j * TileSizeFour + k * 4;
						var v1 = v0 + 1;
						var v2 = v0 + 2;
						var v3 = v0 + 3;
						var offset = new Vector3(j * 32, i, k * 32);
						vertices[v0] = new Vector3(-96f, 0, -64f) + offset;
						vertices[v1] = new Vector3(-64f, 0, -64f) + offset;
						vertices[v2] = new Vector3(-96f, 0, -96f) + offset;
						vertices[v3] = new Vector3(-64f, 0, -96f) + offset;
						colors[v0] = new Color(1, 0, 0, 0);
						colors[v1] = new Color(0, 1, 0, 0);
						colors[v2] = new Color(0, 0, 1, 0);
						colors[v3] = new Color(0, 0, 0, 1);
						var t = i * TileSizeQuadSix + j * TileSizeSix + k * 6;
						triangles[t] = v1;
						triangles[t + 1] = v2;
						triangles[t + 2] = v3;
						triangles[t + 3] = v2;
						triangles[t + 4] = v1;
						triangles[t + 5] = v0;
					}
				}
			}

			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.colors = colors;
			mesh.RecalculateNormals();
			yMesh.mesh = mesh;
		}
	}
}
