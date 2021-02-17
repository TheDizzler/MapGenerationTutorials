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
		private MeshFilter meshFilter;
		private Mesh mesh;

		/// <summary>
		/// Index 0 == polygon centroid.
		/// </summary>
		public Vector3[] vertices;
		public List<int> triangles;

		private int triangleIndex = 0;


		public void CreateRegion(Polygon poly, Tutorials.Planets.ColorSettings colorSettings)
		{
			id = count++;
			polygon = poly;
			ValidatePolygon(polygon);

			CreateNoisyEdges();

			meshFilter = GetComponent<MeshFilter>();
			mesh = CreateMesh();
			meshFilter.sharedMesh = mesh;
			GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMaterial;
			MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = mesh;




			//CreateBorder();
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

		private Mesh CreateMesh()
		{
			// get all vertices from noisy edges
			List<Vector3> edgeVertices = new List<Vector3>();
			edgeVertices.Add(polygon.centroid.position);
			var corners = polygon.corners;
			for (int i = 0; i < corners.Count; ++i)
			{
				VEdge edge;
				if (i == corners.Count - 1)
					edge = corners[i].FindSharedEdgeWith(corners[0]);
				else
					edge = corners[i].FindSharedEdgeWith(corners[i + 1]);
				foreach (var point in edge.segments)
					if (!edgeVertices.Contains(point))
						edgeVertices.Add(point);
			}


			Vector3 up = Vector3.Cross(edgeVertices[1] - edgeVertices[0], edgeVertices[2] - edgeVertices[0]);
			if (up.z > 0)
			{
				Debug.Log("Region: " + id + " is reversed! up: " + up);
				edgeVertices.Reverse();
				Vector3 moveToLast = edgeVertices[0];
				edgeVertices[0] = edgeVertices[edgeVertices.Count - 1];
				edgeVertices[edgeVertices.Count - 1] = moveToLast;
			}
			vertices = new Vector3[edgeVertices.Count * 2 - 1]; // don't need the central vertex for bottom

			triangles = new List<int>();
			Vector3[] bottomVerts = new Vector3[edgeVertices.Count - 1];
			int topTriCount = (edgeVertices.Count - 1) * 3;

			for (int i = 0; i < edgeVertices.Count; ++i)
				vertices[i] = edgeVertices[i];
			int vertexIndex;
			for (vertexIndex = 1; vertexIndex < edgeVertices.Count; ++vertexIndex)
			{
				/// make top face
				if (vertexIndex == edgeVertices.Count - 1)
				{
					triangles.Add(vertexIndex);
					triangles.Add(1);
					triangles.Add(0);
				}
				else
				{
					triangles.Add(vertexIndex);
					triangles.Add(vertexIndex + 1);
					triangles.Add(0);
				}

				// make mirrored bottom face but with no central vertex
				bottomVerts[vertexIndex - 1] = edgeVertices[vertexIndex] + new Vector3(0, 0, 10);
				vertices[vertexIndex + edgeVertices.Count - 1] = bottomVerts[vertexIndex - 1];

				/// create triangles that make up the sides of the polygon
				if (vertexIndex == edgeVertices.Count - 1)
				{
					triangles.Add(edgeVertices.Count);
					triangles.Add(1);
					triangles.Add(vertexIndex);

					triangles.Add(vertexIndex + edgeVertices.Count - 1);
					triangles.Add(edgeVertices.Count);
					triangles.Add(vertexIndex);
				}
				else
				{
					triangles.Add(vertexIndex + edgeVertices.Count);
					triangles.Add(vertexIndex + 1);
					triangles.Add(vertexIndex);

					triangles.Add(vertexIndex + edgeVertices.Count - 1);
					triangles.Add(vertexIndex + edgeVertices.Count);
					triangles.Add(vertexIndex);
				}
				// bottom is not closed off
			}

			Mesh mesh = new Mesh();
			mesh.SetVertices(vertices);
			mesh.triangles = triangles.ToArray();
			mesh.RecalculateNormals();

			return mesh;
		}
	}
}