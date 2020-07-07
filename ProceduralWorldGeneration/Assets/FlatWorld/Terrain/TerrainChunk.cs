using System;
using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	public class TerrainChunk
	{
		private const float colliderGenerationDistThreshold = 5f;

		public event Action<TerrainChunk, bool> OnVisibilityChanged;

		public Vector2 coord;

		private Vector2 sampleCenter;
		private GameObject meshObject;
		private Bounds bounds;
		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;
		private MeshCollider meshCollider;
		private LODInfo[] detailLevels;
		private LODMesh[] lodMeshes;
		private int colliderLODIndex;
		private Transform viewer;
		private HeightMap heightMap;
		private bool heightMapReceived;
		private int previousLODIndex = -1;
		private bool hasSetCollider = false;
		private HeightMapSettings heightMapSettings;
		private MeshSettings meshSettings;
		private float maxViewDist;

		Vector2 viewerPosition
		{
			get { return new Vector2(viewer.position.x, viewer.position.z); }
		}

		public TerrainChunk(Vector2 coords, HeightMapSettings heightMapSettings, MeshSettings meshSettings,
			LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
		{
			this.coord = coords;
			this.heightMapSettings = heightMapSettings;
			this.meshSettings = meshSettings;
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;
			this.viewer = viewer;
			maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;

			sampleCenter = coords * meshSettings.meshWorldSize / meshSettings.meshScale;
			Vector2 position = coord * meshSettings.meshWorldSize;
			bounds = new Bounds(sampleCenter, Vector2.one * meshSettings.meshWorldSize);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;

			meshObject.transform.position = new Vector3(position.x, 0, position.y);
			meshObject.transform.SetParent(parent, false);
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < lodMeshes.Length; ++i)
			{
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
				lodMeshes[i].updateCallback += UpdateTerrainChunk;
				if (i == colliderLODIndex)
					lodMeshes[i].updateCallback += UpdateCollisionMesh;
			}



		}

		public void Load()
		{
			ThreadedDataRequester.RequestData(
				() => HeightMapGenerator.GenerateHeightMap(
					meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine,
					heightMapSettings, sampleCenter),
				OnHeightMapReceived);
		}

		public void UpdateTerrainChunk()
		{
			if (!heightMapReceived)
				return;
			bool wasVisible = IsVisible();
			float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			bool visible = viewerDistFromNearestEdge <= maxViewDist;

			if (visible)
			{
				int lodIndex = 0;
				for (int i = 0; i < detailLevels.Length - 1; ++i)
				{
					if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
						lodIndex = i + 1;
					else
						break;
				}

				if (lodIndex != previousLODIndex)
				{
					LODMesh lodMesh = lodMeshes[lodIndex];
					if (lodMesh.hasMesh)
					{
						previousLODIndex = lodIndex;
						meshFilter.mesh = lodMesh.mesh;
					}
					else if (!lodMesh.hasRequestedMesh)
						lodMesh.RequestMesh(heightMap, meshSettings);
				}
			}

			if (wasVisible != visible)
			{
				SetVisible(visible);
				OnVisibilityChanged(this, visible);
			}
		}

		public void UpdateCollisionMesh()
		{
			if (hasSetCollider)
				return;

			float sqrDistFromViewerToEdge = bounds.SqrDistance(viewerPosition);

			if (sqrDistFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistThreshold)
				if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
					lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);

			if (sqrDistFromViewerToEdge < colliderGenerationDistThreshold * colliderGenerationDistThreshold)
				if (lodMeshes[colliderLODIndex].hasMesh)
				{
					meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
					hasSetCollider = true;
				}
		}

		public void SetVisible(bool isVisible)
		{
			meshObject.SetActive(isVisible);
		}

		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}


		void OnHeightMapReceived(object heightMapObject)
		{
			this.heightMap = (HeightMap)heightMapObject;
			heightMapReceived = true;

			UpdateTerrainChunk();
		}
	}

	public class LODMesh
	{
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		public event Action updateCallback;


		public LODMesh(int lod)
		{
			this.lod = lod;

		}

		public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
		{
			hasRequestedMesh = true;
			ThreadedDataRequester.RequestData(
				() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod),
				OnMeshDataReceived);
		}

		private void OnMeshDataReceived(object meshDataObject)
		{
			mesh = ((MeshData)meshDataObject).CreateMesh();
			hasMesh = true;
			updateCallback();
		}
	}
}