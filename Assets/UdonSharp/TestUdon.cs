using UdonSharp;

public class TestUdon : UdonSharpBehaviour
{
	/*[SerializeField]
	private MeshFilter meshFilter;
	[SerializeField]
	private MeshRenderer meshRenderer;
	[SerializeField]
	private Texture2D texture;

	private void Start()
	{
		meshRenderer.material.SetTexture("_MainTex", texture);

		Mesh mesh = new Mesh();
		mesh.name = "ItemMesh";

		Vector3[] vertices = new Vector3[]
		{
			new Vector3(0, 0, 0), // нижний левый
            new Vector3(1, 0, 0), // нижний правый
            new Vector3(0, 1, 0), // верхний левый
            new Vector3(1, 1, 0), // верхний правый
        };

		int[] triangles = new int[]
		{
			0, 1, 2,
			2, 1, 3
		};

		Color[] colors = new Color[]
		{
			new Color(0, 0, 0),

			new Color(1, 0, 0),
			new Color(0, 1, 0),
			new Color(1, 1, 0)
		};

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.colors = colors;
		mesh.RecalculateNormals();

		meshFilter.mesh = mesh;
	}*/
}
