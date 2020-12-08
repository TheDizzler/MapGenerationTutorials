using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AtomosZ.Voronoi.Regions
{
	public class Region : MonoBehaviour
	{

		public float nudgeToCenterAmount = .2f;
		public float borderWidth = .127f;

		[SerializeField] private GameObject borderRenderer = null;
		[SerializeField] private Transform borders = null;

		private Polygon polygon;
		private MeshFilter meshFilter;
		private Mesh mesh;

		/// <summary>
		/// Index 0 == polygon centroid.
		/// </summary>
		public Vector3[] vertices;
		public int[] triangles;

		private int triangleIndex = 0;


		public void CreateRegion(Polygon poly)
		{
			polygon = poly;
			ValidatePolygon(polygon);

			meshFilter = GetComponent<MeshFilter>();
			mesh = CreateMesh();
			meshFilter.sharedMesh = mesh;
			MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = mesh;


			CreateNoisyEdges();
			//CreateBorder();
		}


		private void CreateNoisyEdges()
		{
			foreach (VEdge edge in polygon.voronoiEdges)
			{
				edge.CreateNoisyEdge(VoronoiGenerator.instance.subdivisions);
				GameObject border = Instantiate(borderRenderer, borders);
				LineRenderer lr = border.GetComponent<LineRenderer>();
				lr.startColor = Color.black;
				lr.endColor = Color.black;
				lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
				lr.widthMultiplier = borderWidth;
				lr.numCapVertices = 4;
				lr.numCornerVertices = 4;

				lr.positionCount = edge.segments.Count;
				lr.SetPositions(edge.segments.ToArray());
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

			foreach (var edge in polygon.voronoiEdges)
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
			// order corners
			OrderCorners();

			vertices = new Vector3[polygon.corners.Count + 1]; // each corner plus the center
			vertices[0] = polygon.centroid.position;

			// make triangles using two corners plus center
			triangles = new int[polygon.voronoiEdges.Count * 3]; // one triangle per edge
			for (int i = 0; i < polygon.corners.Count; ++i)
			{
				vertices[i + 1] = polygon.corners[i].position;
				if (i == polygon.corners.Count - 1)
					AddTriangle(i + 1, 1);
				else
					AddTriangle(i + 1, i + 2);
			}

			Mesh mesh = new Mesh();
			mesh.SetVertices(vertices);
			mesh.triangles = triangles;
			mesh.RecalculateNormals();

			List<Vector3> normals = new List<Vector3>();
			mesh.GetNormals(normals);
			if (normals[0].z > 0) // corners were in reverse order
			{
				vertices = vertices.Reverse().ToArray();
				Vector3 moveToLast = vertices[0];
				vertices[0] = vertices[vertices.Length - 1];
				vertices[vertices.Length - 1] = moveToLast;
				mesh.SetVertices(vertices);
				polygon.corners.Reverse();

			}

			mesh.RecalculateNormals();
			return mesh;
		}

		/// <summary>
		/// must be adjacent corners.
		/// </summary>
		/// <param name="corner1"></param>
		/// <param name="corner2"></param>
		private void AddTriangle(int corner1, int corner2)
		{
			if ((triangleIndex + 2) >= triangles.Length)
			{
				VoronoiGenerator.debugPolygons.Add(polygon);
				throw new System.Exception("Triangles exceeds our allocation. Index required: " + (triangleIndex + 2) + ". Allocated: " + triangles.Length
					+ "\nPolygon: " + polygon.centroid.position);
			}

			triangles[triangleIndex++] = corner1;
			triangles[triangleIndex++] = corner2;
			triangles[triangleIndex++] = 0;
		}

		/// <summary>
		/// Travels around the polygon, corner-by-corner and puts corners in order 
		///  either clockwise or counterclockwise...currently no way to know which way :O
		/// </summary>
		private void OrderCorners()
		{
			List<Corner> ordered = new List<Corner>();
			Corner first = polygon.corners[0];
			ordered.Add(first);
			List<Corner> neighbours = first.GetConnectedCornersIn(polygon);
			Corner next = neighbours[0];

			while (true)
			{
				ordered.Add(next);
				neighbours = next.GetConnectedCornersIn(polygon);

				if (!ordered.Contains(neighbours[0]))
				{
					next = neighbours[0];
				}
				else if (!ordered.Contains(neighbours[1]))
				{
					next = neighbours[1];
				}
				else
				{
					break;
				}

				if (ordered.Count > 10)
					throw new System.Exception("We most certainly fucked up");
			}


			polygon.corners = ordered;
		}
	}
}