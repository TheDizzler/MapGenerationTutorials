using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi.Regions
{
	[ExecuteInEditMode]
	public class Region : MonoBehaviour
	{
		/// <summary>
		///  Min height of a voronoi column
		/// </summary>
		private static float baseHeight = 5;
		public static int count = 0;

		[Tooltip("Debug ID")]
		public int id = 0;
		public float nudgeToCenterAmount = .2f;
		public float borderWidth = .127f;
		[Tooltip("This is negative because -z is up, +z is down")]
		public float regionHeight;
		[SerializeField] private GameObject borderRenderer = null;
		[SerializeField] private Transform borders = null;

		[HideInInspector]
		public Polygon polygon;
		private Mesh topMesh;
		private MeshFilter topMeshFilter;
		private MeshRenderer topMeshRenderer;
		private Mesh sideMesh;
		private MeshFilter sideMeshFilter;
		private MeshRenderer sideMeshRenderer;

		/// <summary>
		/// Index 0 == polygon centroid.
		/// </summary>
		private Vector3[] topVertices;
		private Vector3[] sideVertices;
		//private Vector2[] uvs;
		private List<int> topTriangles;
		private List<int> sideTriangles;
		/// <summary>
		/// The vertices that make up the border of the region PLUS the center (index 0)
		/// </summary>
		private List<Vector3> edgeVertices;


		private List<RegionBorder> regionBorders;
		private float previousRegionHeight;


		public void CreateRegion(Polygon poly)
		{
			id = count++;
			polygon = poly;
			polygon.region = this;
			ValidatePolygon(polygon);

			CreateNoisyEdges();

			GameObject topMeshGO = new GameObject();
			topMeshGO.transform.SetParent(transform, false);
			topMeshGO.name = "top mesh";
			topMeshFilter = topMeshGO.AddComponent<MeshFilter>();
			topMeshRenderer = topMeshGO.AddComponent<MeshRenderer>();

			GameObject sideMeshGO = new GameObject();
			sideMeshGO.transform.SetParent(transform, false);
			sideMeshGO.name = "side mesh";
			sideMeshFilter = sideMeshGO.AddComponent<MeshFilter>();
			sideMeshRenderer = sideMeshGO.AddComponent<MeshRenderer>();
		}



		public void SetRegionHeight(int mapResolution, HeightMap heightMap, Material regionMat, Material sideMat)
		{
			regionHeight = -heightMap.values[ // negative because -z is up, +z is down
				(int)(polygon.centroid.position.x * mapResolution),
				(int)(polygon.centroid.position.y * mapResolution)];
			previousRegionHeight = regionHeight;
			polygon.centroid.position.z = regionHeight;
			borders.transform.localPosition = new Vector3(0, 0, regionHeight) + RegionBorder.borderZOffset;
			minValue = heightMap.minValue;
			maxValue = heightMap.maxValue;
			topMeshRenderer.sharedMaterial = regionMat;
			sideMeshRenderer.sharedMaterial = sideMat;
			
		}


		public void SetBorderHeights()
		{
			OrderEdgeSegments();
			foreach (var regionBorder in regionBorders)
			{
				if (regionBorder.edge.heightsSet)
				{
					regionBorder.ComputeVertices();
					continue;
				}

				regionBorder.edge.heightsSet = true;

				float startCornerHeight = 0;
				Corner startCorner = regionBorder.startCorner;
				List<Polygon> connectedPolygons = new List<Polygon>();
				connectedPolygons.AddRange(startCorner.polygons);
				foreach (Polygon polygon in connectedPolygons)
				{
					startCornerHeight += polygon.region.regionHeight;
				}

				startCornerHeight /= connectedPolygons.Count;
				Vector3 newPos = regionBorder.startCorner.position;
				newPos.z = startCornerHeight;
				startCorner.position = newPos;

				float endCornerHeight = 0;
				Corner endCorner = regionBorder.endCorner;
				connectedPolygons.Clear();
				connectedPolygons.AddRange(endCorner.polygons);
				foreach (Polygon polygon in connectedPolygons)
				{
					endCornerHeight += polygon.region.regionHeight;
				}

				endCornerHeight /= connectedPolygons.Count;
				newPos = regionBorder.endCorner.position;
				newPos.z = endCornerHeight;
				endCorner.position = newPos;

				var edge = regionBorder.edge;
				float segmentCount = edge.segments.Count;
				for (int segmentIndex = 0; segmentIndex < segmentCount; ++segmentIndex)
				{
					float newHeight = Mathf.Lerp(startCornerHeight, endCornerHeight,
						segmentIndex / (segmentCount - 1));
					newPos = edge.segments[segmentIndex];
					newPos.z = newHeight;
					edge.segments[segmentIndex] = newPos;
				}

				regionBorder.ComputeVertices();
			}
		}


		public void UpdateMeshHeights()
		{
			float diff = regionHeight - previousRegionHeight;
			var verts = topMeshFilter.sharedMesh.vertices;
			for (int i = 0; i < verts.Length; ++i)
			{
				verts[i].z = verts[i].z + diff;
			}
			topMeshFilter.sharedMesh.SetVertices(verts);
			topMesh.RecalculateNormals();

			verts = sideMeshFilter.sharedMesh.vertices;
			for (int i = 0; i < verts.Length; i += 2)
			{
				verts[i].z = verts[i].z + diff;
			}
			sideMeshFilter.sharedMesh.SetVertices(verts);
			sideMesh.RecalculateNormals();

			previousRegionHeight = regionHeight;
		}


		public void ToggleBorder(bool bordersEnabled)
		{
			borders.gameObject.SetActive(bordersEnabled);
		}


		/// <summary>
		/// This assumes edges are in order.
		/// </summary>
		private void CreateNoisyEdges()
		{
			regionBorders = new List<RegionBorder>();
			List<VEdge> vEdges = polygon.GetVoronoiEdges();
			foreach (VEdge edge in vEdges)
			{
				RegionBorder border = new RegionBorder(edge, Instantiate(borderRenderer, borders));
				regionBorders.Add(border);
			}


		}

		/// <summary>
		/// Put all edge segments in order for vertex wrapping
		/// NOTE: this order is not preserved and may change when the next
		/// polygon that shares this edge creates its border.
		/// </summary>
		private void OrderEdgeSegments()
		{
			List<VEdge> vEdges = polygon.GetVoronoiEdges();
			VEdge last = vEdges[0];
			for (int i = 1; i < vEdges.Count; ++i)
			{
				if (!vEdges[i].SharesCorner(vEdges[i - 1], out Corner noneed))
					Debug.Log("polygon " + polygon.id + " Srsly?"); // should be able to remove this check after a little more testing

				var current = vEdges[i];
				if (!last.SharesCorner(current, out Corner sharedCorner))
				{// should be able to remove this check after a little more testing
					throw new System.Exception("Fark in region " + id);
				}

				if (sharedCorner != last.end)
				{
					if (i != 1) // should be able to remove this check after a little more testing
						throw new System.Exception("we're farked");
					last.ReverseSegments();
				}

				if (sharedCorner != current.start)
					current.ReverseSegments();

				last = current;
			}

			foreach (var border in regionBorders)
			{
				border.SetCorners();
			}
		}

		void OnMouseEnter()
		{
			Debug.Log("entered " + polygon.GetName());
		}


		private void ValidatePolygon(Polygon polygon)
		{
			foreach (var corner in polygon.corners)
			{
				if (corner.isOOB || corner.isInvalidated)
				{
					VoronoiGenerator.debugCorners.Add(corner);
					VoronoiGenerator.debugPolygons.Add(polygon);
					throw new System.Exception("Corner is OOB or is invalidated. CornerID: " + corner.num);
				}

				if (!corner.polygons.Contains(polygon))
				{
					VoronoiGenerator.debugCorners.Add(corner);
					throw new System.Exception("Corner does not contain this polygon!");
				}
			}

			foreach (var edge in polygon.GetVoronoiEdges())
			{
				if (edge.GetPolygonCount() == 0 || edge.GetPolygonCount() > 2)
				{
					VoronoiGenerator.debugEdges.Add(edge);
					throw new System.Exception("edge has invalid num of polygons");
				}

				if (!polygon.corners.Contains(edge.start) || !polygon.corners.Contains(edge.end))
				{
					VoronoiGenerator.debugCorners.Add(edge.start);
					VoronoiGenerator.debugCorners.Add(edge.end);
					VoronoiGenerator.debugEdges.Add(edge);
					throw new System.Exception("edge has corner not contained in polygon");
				}

				if (!edge.GetPolygons().Contains(polygon))
				{
					VoronoiGenerator.debugEdges.Add(edge);
					throw new System.Exception("edge does not contain this polygon!");
				}
			}
		}


		public void CreateMeshes()
		{
			// get all vertices from noisy edges
			edgeVertices = new List<Vector3>();

			for (int i = 0; i < regionBorders.Count; ++i)
			{
				var border = regionBorders[i];
				for (int j = 0; j < border.borderVertices.Count - 1; ++j)
					edgeVertices.Add(border.borderVertices[j]);
			}

			Vector3 up = Vector3.Cross(edgeVertices[0] - polygon.centroid.position, edgeVertices[1] - polygon.centroid.position);
			if (up.z > 0)
			{ // edges are wound in the wrong order
				edgeVertices.Reverse();
				regionBorders.Reverse();
				foreach (var regionBorder in regionBorders)
					regionBorder.Reverse();
			}

			topVertices = new Vector3[edgeVertices.Count + 1];
			topVertices[0] = polygon.centroid.position;
			topTriangles = new List<int>();
			for (int vertexIndex = 1; vertexIndex < topVertices.Length; ++vertexIndex)
			{
				topVertices[vertexIndex] = edgeVertices[vertexIndex - 1];
				if (vertexIndex == topVertices.Length - 1)
				{
					topTriangles.Add(vertexIndex);
					topTriangles.Add(1);
					topTriangles.Add(0);
				}
				else
				{
					topTriangles.Add(vertexIndex);
					topTriangles.Add(vertexIndex + 1);
					topTriangles.Add(0);
				}
			}

			topMesh = new Mesh();
			topMesh.SetVertices(topVertices);
			topMesh.triangles = topTriangles.ToArray();
			topMesh.RecalculateNormals();
			topMeshFilter.sharedMesh = topMesh;


			MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = topMesh;


			sideVertices = new Vector3[edgeVertices.Count * 6]; // top and bottom and 3 copies (one for each triangle)
			sideTriangles = new List<int>();
			for (int vertexIndex = 0; vertexIndex < sideVertices.Length; vertexIndex += 6)
			{
				int edgeVertex = vertexIndex / 6;
				Vector3 vert = new Vector3(edgeVertices[edgeVertex].x, edgeVertices[edgeVertex].y, baseHeight);
				if (edgeVertex == edgeVertices.Count - 1)
				{
					sideVertices[vertexIndex] = edgeVertices[edgeVertex];
					sideVertices[vertexIndex + 1] = vert;
					sideVertices[vertexIndex + 2] = edgeVertices[0];


					sideVertices[vertexIndex + 3] = vert;
					sideVertices[vertexIndex + 4] = edgeVertices[0];
					sideVertices[vertexIndex + 5] = new Vector3(edgeVertices[0].x, edgeVertices[0].y, baseHeight);
				}
				else
				{
					sideVertices[vertexIndex] = edgeVertices[edgeVertex];
					sideVertices[vertexIndex + 1] = vert;
					sideVertices[vertexIndex + 2] = edgeVertices[edgeVertex + 1];

					sideVertices[vertexIndex + 3] = vert;
					sideVertices[vertexIndex + 4] = edgeVertices[edgeVertex + 1];
					sideVertices[vertexIndex + 5] = new Vector3(edgeVertices[edgeVertex + 1].x, edgeVertices[edgeVertex + 1].y, baseHeight);

				}

				sideTriangles.Add(vertexIndex);     // top
				sideTriangles.Add(vertexIndex + 1); // bottom
				sideTriangles.Add(vertexIndex + 2); // top

				sideTriangles.Add(vertexIndex + 5); // bottom
				sideTriangles.Add(vertexIndex + 4); // top
				sideTriangles.Add(vertexIndex + 3); // bottom
			}

			sideMesh = new Mesh();
			sideMesh.SetVertices(sideVertices);
			sideMesh.triangles = sideTriangles.ToArray();
			sideMesh.RecalculateNormals();
			sideMeshFilter.sharedMesh = sideMesh;
		}


		private class RegionBorder
		{
			/// <summary>
			/// Just a little nudge to pop the border out a bit for visibility.
			/// </summary>
			public static readonly Vector3 borderZOffset = new Vector3(0, 0, -.15f);
			public static Color borderColor = Color.black;
			public static float borderWidth = .127f;


			public VEdge edge;
			public Corner startCorner;
			public Corner endCorner;
			public LineRenderer lineRenderer;
			public List<Vector3> borderVertices = new List<Vector3>();


			public RegionBorder(VEdge edge, GameObject border)
			{
				this.edge = edge;
				edge.CreateNoisyEdge(VoronoiGenerator.instance.subdivisions);

				lineRenderer = border.GetComponent<LineRenderer>();
				lineRenderer.useWorldSpace = false;
				lineRenderer.startColor = borderColor;
				lineRenderer.endColor = borderColor;
				lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
				lineRenderer.widthMultiplier = borderWidth;
				lineRenderer.numCapVertices = 4;
				lineRenderer.numCornerVertices = 4;
				lineRenderer.positionCount = edge.segments.Count;
				lineRenderer.SetPositions(edge.segments.ToArray());

				border.transform.localPosition = borderZOffset;
			}

			/// <summary>
			/// Edge segments MUST be in the proper order for THIS polygon before computing!
			/// </summary>
			public void ComputeVertices()
			{
				borderVertices.Clear();
				for (int i = 0; i < edge.segments.Count; ++i)
				{
					borderVertices.Add(edge.segments[i]);
				}

				if (!Mathf.Approximately(edge.start.position.x, borderVertices[0].x)
					&& !Mathf.Approximately(edge.start.position.y, borderVertices[0].y))
					Debug.Log("WTF start");

				if (!Mathf.Approximately(edge.end.position.x, borderVertices[borderVertices.Count - 1].x)
					&& !Mathf.Approximately(edge.end.position.y, borderVertices[borderVertices.Count - 1].y))
					Debug.Log("WTF end");
			}

			public void Reverse()
			{
				borderVertices.Reverse();
				var temp = startCorner;
				startCorner = endCorner;
				endCorner = temp;
			}

			public void SetCorners()
			{
				startCorner = edge.start;
				endCorner = edge.end;
			}
		}
	}
}