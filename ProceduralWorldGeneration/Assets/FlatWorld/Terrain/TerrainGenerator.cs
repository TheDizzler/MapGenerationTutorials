using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	public class TerrainGenerator : MonoBehaviour
	{
		private const float viewerMoveThresholdForChunkUpdate = 25f;
		private const float sqrviewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

		public MeshSettings meshSettings;
		public HeightMapSettings heightMapSettings;
		public TextureData textureSettings;
		public Transform viewer;
		public Material mapMaterial;
		public int colliderLODIndex;
		public LODInfo[] detailLevels;


		private Vector2 viewerPosition;
		private Vector2 viewerPositionOld = Vector2.positiveInfinity;
		private float meshWorldSize;
		private int chunksVisibleInViewDist;

		private Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
		private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();


		void Start()
		{
			gameObject.AddComponent<ThreadedDataRequester>();
			textureSettings.ApplyToTextureMaterial(mapMaterial);
			textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

			float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
			meshWorldSize = meshSettings.meshWorldSize;
			chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);
		}

		void Update()
		{
			viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

			if (viewerPosition != viewerPositionOld)
				foreach (var chunk in visibleTerrainChunks)
					chunk.UpdateCollisionMesh();

			if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrviewerMoveThresholdForChunkUpdate)
			{
				viewerPositionOld = viewerPosition;
				UpdateVisibleChunks();
			}
		}

		private void UpdateVisibleChunks()
		{
			HashSet<Vector2> alreadyUpdatedChunks = new HashSet<Vector2>();
			for (int i = visibleTerrainChunks.Count - 1; i >= 0; --i)
			{
				alreadyUpdatedChunks.Add(visibleTerrainChunks[i].coord);
				visibleTerrainChunks[i].UpdateTerrainChunk();
			}

			int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
			int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

			for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; ++yOffset)
				for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; ++xOffset)
				{
					Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
					if (!terrainChunks.TryGetValue(viewedChunkCoord, out TerrainChunk chunk))
					{
						TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings,
								detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
						terrainChunks.Add(viewedChunkCoord,
							newChunk);
						newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load();
					}
					else if (!alreadyUpdatedChunks.Contains(viewedChunkCoord))
					{
						chunk.UpdateTerrainChunk();
					}
				}
		}

		void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
		{
			if (isVisible)
				visibleTerrainChunks.Add(chunk);
			else
				visibleTerrainChunks.Remove(chunk);
		}
	}

	[Serializable]
	public struct LODInfo
	{
		[Range(0, MeshSettings.NumSupportedLODs - 1)]
		public int lod;
		public float visibleDistThreshold;

		public float sqrVisibleDistThreshold
		{
			get { return visibleDistThreshold * visibleDistThreshold; }
		}
	}
}