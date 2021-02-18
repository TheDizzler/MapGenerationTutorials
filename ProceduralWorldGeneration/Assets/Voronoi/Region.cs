using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AtomosZ.Voronoi.Regions
{
	public class Region : MonoBehaviour
	{
		public static int count = 0;

		public int id = 0;
		public float nudgeToCenterAmount = .2f;
		public float borderWidth = .127f;

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




		public void CreateRegion(Polygon poly)
		{
			id = count++;
			polygon = poly;
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

			CreateMeshes();

			MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = topMesh;
		}



		public void SetRegionHeight(Tutorials.FlatWorld.NoiseSettings noiseSettings, float heightScale, Material regionMat)
		{
			regionHeight = Tutorials.FlatWorld.Noise.GetHeightAtPoint(polygon.centroid.position, noiseSettings);
			this.heightScale = heightScale;
			borders.transform.localPosition = new Vector3(0, 0, regionHeight * heightScale) + VEdge.borderZOffset;
			//UpdateMeshHeights();
			topMeshRenderer.sharedMaterial = regionMat;
			sideMeshRenderer.sharedMaterial = regionMat;

			topMesh.RecalculateNormals();
			//sideMesh.RecalculateNormals();
		}

		public void UpdateMeshHeights()
		{
			var verts = topMeshFilter.sharedMesh.vertices;
			Vector3[] normals = new Vector3[verts.Length];
			for (int i = 0; i < edgeVertices.Count; ++i)
			{
				verts[i].z = regionHeight * heightScale;
			}
			topMeshFilter.sharedMesh.SetVertices(verts);

			//bakedNormals = CalculateNormals();
			//mesh.normals = bakedNormals;

			topMesh.RecalculateNormals();
			//FlatShading();
			//mesh.uv = uvs;

		}


		public void ToggleBorder(bool bordersEnabled)
		{
			borders.gameObject.SetActive(bordersEnabled);
		}

		private void CreateNoisyEdges()
		{
			List<VEdge> vEdges = polygon.GetVoronoiEdges();
			foreach (VEdge edge in vEdges)
			{
				edge.CreateNoisyEdge(VoronoiGenerator.instance.subdivisions);

				GameObject border = Instantiate(borderRenderer, borders);
				LineRenderer lr = border.GetComponent<LineRenderer>();
				lr.useWorldSpace = false;
				lr.startColor = Color.black;
				lr.endColor = Color.black;
				lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
				lr.widthMultiplier = borderWidth;
				lr.numCapVertices = 4;
				lr.numCornerVertices = 4;

				lr.positionCount = edge.segments.Count;
				lr.SetPositions(edge.segments.ToArray());

				border.transform.localPosition = VEdge.borderZOffset;
			}

			for (int i = 1; i < vEdges.Count; ++i)
			{
				if (!vEdges[i].SharesCorner(vEdges[i - 1], out Corner noneed))
					Debug.Log("polygon " + polygon.id + " Srsly?"); // should be able to remove this check after a little more testing
			}

			VEdge last = vEdges[0];
			for (int i = 1; i < vEdges.Count; ++i)
			{
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
		}

		/// <summary>
		/// Corners are nudged inwards a bit to create distinction between polygons and prevent overlapping borders.
		/// </summary>
		private void CreateBorder()
		{
			LineRenderer lr = gameObject.AddComponent<LineRenderer>();
			lr.startColor = Color.black;
			lr.endColor = Color.black;
			lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
			lr.widthMultiplier = borderWidth;
			lr.positionCount = vertices.Length + 1;
			lr.numCapVertices = 20;

			int i;
			Vector3 vert;
			Vector3 dirToCenter;
			Vector3 adjustedPos;
			for (i = 1; i < vertices.Length; ++i)
			{
				vert = vertices[i];
				dirToCenter = (vert - (Vector3)polygon.centroid.position).normalized;
				adjustedPos = vert - dirToCenter * nudgeToCenterAmount;
				lr.SetPosition(i - 1, adjustedPos);
			}

			vert = vertices[1]; // index 0 is center!
			dirToCenter = (vert - (Vector3)polygon.centroid.position).normalized;
			adjustedPos = vert - dirToCenter * nudgeToCenterAmount;
			lr.SetPosition(i - 1, adjustedPos);
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


		private void CreateMeshes()
		{
			// get all vertices from noisy edges
			edgeVertices = new List<Vector3>();

			var vedges = polygon.GetVoronoiEdges();
			for (int i = 0; i < vedges.Count; ++i)
			{
				foreach (var point in vedges[i].segments)
					if (!edgeVertices.Contains(point))
						edgeVertices.Add(point);
			}

			Vector3 up = Vector3.Cross(edgeVertices[0] - polygon.centroid.position, edgeVertices[1] - polygon.centroid.position);
			if (up.z > 0)
			{ // edges are wound in the wrong order
				edgeVertices.Reverse();
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
			topMeshFilter.sharedMesh = topMesh;

			sideVertices = new Vector3[edgeVertices.Count * 2];
			Vector3[] normals = new Vector3[sideVertices.Length];
			sideTriangles = new List<int>();
			topTriangles = new List<int>();
			for (int vertexIndex = 0; vertexIndex < sideVertices.Length; vertexIndex += 2)
			{
				sideVertices[vertexIndex] = edgeVertices[vertexIndex / 2];
				sideVertices[vertexIndex + 1] = sideVertices[vertexIndex] + new Vector3(0, 0, 5);

				if (vertexIndex == sideVertices.Length - 2)
				{
					sideTriangles.Add(vertexIndex);     // top
					sideTriangles.Add(vertexIndex + 1); // bottom
					sideTriangles.Add(0); // top

					sideTriangles.Add(vertexIndex + 1); // bottom
					sideTriangles.Add(1); // bottom 
					sideTriangles.Add(0); // top
				}
				else
				{
					sideTriangles.Add(vertexIndex);     // top
					sideTriangles.Add(vertexIndex + 1); // bottom
					sideTriangles.Add(vertexIndex + 2); // top

					sideTriangles.Add(vertexIndex + 1); // bottom
					sideTriangles.Add(vertexIndex + 3); // bottom 
					sideTriangles.Add(vertexIndex + 2); // top
				}
			}
			
			sideMesh = new Mesh();
			sideMesh.SetVertices(sideVertices);
			sideMesh.triangles = sideTriangles.ToArray();
			//sideMesh.normals = CalculateNormals(sideVertices, sideTriangles);
			sideMesh.RecalculateNormals();
			sideMeshFilter.sharedMesh = sideMesh;
		}


			return mesh;
		}
	}
}