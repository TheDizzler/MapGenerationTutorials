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

		private Polygon polygon;
		[HideInInspector]
		public Polygon polygon;
		private MeshFilter meshFilter;
		private Mesh mesh;

		/// <summary>
		/// Index 0 == polygon centroid.
		/// </summary>
		public Vector3[] vertices;
		public int[] triangles;

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
				lr.startColor = Color.black;
				lr.endColor = Color.black;
				lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
				lr.widthMultiplier = borderWidth;
				lr.numCapVertices = 4;
				lr.numCornerVertices = 4;

				lr.positionCount = edge.segments.Count;
				lr.SetPositions(edge.segments.ToArray());
			}

			for (int i = 1; i < vEdges.Count; ++i)
			{
				if (!vEdges[i].SharesCorner(vEdges[i - 1], out Corner noneed))
					Debug.Log("polygon " + polygon.id + " Srsly?");
			}

			VEdge last = vEdges[0];
			for (int i = 1; i < vEdges.Count; ++i)
			{
				var current = vEdges[i];
				if (!last.SharesCorner(current, out Corner sharedCorner))
				{
					throw new System.Exception("Fark in region " + id);
				}

				if (sharedCorner != last.end)
				{
					if (i != 1)
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

			vertices = new Vector3[edgeVertices.Count];


			triangles = new int[(edgeVertices.Count - 1) * 3];
			vertices = edgeVertices.ToArray();
			for (int i = 1; i < edgeVertices.Count; ++i)
			{
				if (i == edgeVertices.Count - 1)
					AddTriangle(i, 1);
				else
					AddTriangle(i, i + 1);
			}

			///Using the Triangulator - does not work with central vertex
			//for (int i = 0; i < edgeVertices.Count; ++i)
			//	vertices[i] = edgeVertices[i];
			//Triangulator tr = new Triangulator(vertices);
			//var tris = tr.Triangulate();
			////triangles = tris;

			//int[] verticesFound = new int[vertices.Length];
			//foreach (var tri in tris)
			//{
			//	verticesFound[tri] += 1;
			//}

			//List<int> lostVerts = new List<int>();
			//for (int i = 0; i < verticesFound.Length; ++i)
			//{
			//	if (verticesFound[i] < 2)
			//		lostVerts.Add(i);

			//}
			//triangles = new int[(edgeVertices.Count - 1) * 3];
			//for (int i = 0; i < tris.Length; ++i)
			//	triangles[i] = tris[i];
			//triangles[triangles.Length - 3] = lostVerts[1];
			//triangles[triangles.Length - 2] = lostVerts[0];
			//triangles[triangles.Length - 1] = 0;


			Debug.Log("Region " + id + " vertices " + vertices.Length);
			Debug.Log("Region " + id + " triangles " + triangles.Length);


			/// Simple polygon - corners only
			//vertices = new Vector3[polygon.corners.Count + 1]; // each corner plus the center
			//vertices[0] = polygon.centroid.position;

			//// make triangles using two corners plus center
			//triangles = new int[polygon.voronoiEdges.Count * 3]; // one triangle per edge
			//for (int i = 0; i < polygon.corners.Count; ++i)
			//{
			//	vertices[i + 1] = polygon.corners[i].position;
			//	if (i == polygon.corners.Count - 1)
			//		AddTriangle(i + 1, 1);
			//	else
			//		AddTriangle(i + 1, i + 2);
			//}


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
	}
}