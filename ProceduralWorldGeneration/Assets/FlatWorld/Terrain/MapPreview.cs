using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	public class MapPreview : MonoBehaviour
	{
		public enum DrawMode { Noise, FalloffMap, Mesh }
		public DrawMode drawMode;

		public Renderer textureRenderer;
		public MeshFilter meshFilter;
		public MeshRenderer meshRenderer;
		public MeshSettings meshSettings;
		public HeightMapSettings heightMapSettings;
		public TextureData textureData;
		public Material terrainMaterial;



		[Range(0, MeshSettings.NumSupportedLODs - 1)]
		public int editorPreviewLOD;



		private float[,] falloffMap;


		void Awake()
		{
			Destroy(gameObject);
		}

		public void DrawMapInEditor()
		{
			textureData.ApplyToMaterial(terrainMaterial);
			textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);


			HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
				meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);
			
			switch (drawMode)
			{
				case DrawMode.Noise:
					DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
					break;
				case DrawMode.FalloffMap:
					DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(
							FalloffGenerator.GenerateFalloffMap(meshSettings.NumVertsPerLine, heightMapSettings.falloffCurve),
						0, 1)));
					break;
				case DrawMode.Mesh:
					DrawMesh(MeshGenerator.GenerateTerrainMesh(
							heightMap.values, meshSettings, editorPreviewLOD));
					break;
			}
		}


		public void DrawTexture(Texture2D texture)
		{
			textureRenderer.sharedMaterial.mainTexture = texture;
			textureRenderer.transform.localScale = new Vector3(texture.width, texture.height, 10) / 10;

			textureRenderer.gameObject.SetActive(true);
			meshFilter.gameObject.SetActive(false);
		}

		public void DrawMesh(MeshData meshData)
		{
			meshFilter.sharedMesh = meshData.CreateMesh();
			meshFilter.transform.localScale = Vector3.one * meshSettings.meshScale;

			textureRenderer.gameObject.SetActive(false);
			meshFilter.gameObject.SetActive(true);
		}
	}
}