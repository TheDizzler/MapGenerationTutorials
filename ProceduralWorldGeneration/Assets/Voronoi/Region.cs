using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi.Regions
{
	public class Region : MonoBehaviour
	{
		private Polygon polygon;
		private MeshFilter meshFilter;
		/// <summary>
		/// Index 0 == polygon centroid.
		/// </summary>
		public Vector3[] vertices;
		public int[] triangles;

		private int triangleIndex = 0;


		public void CreateRegion(Polygon poly)
		{
			polygon = poly;
			//transform.position = polygon.centroid.position;
			meshFilter = GetComponent<MeshFilter>();
			meshFilter.sharedMesh = CreateMesh();
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
			mesh.triangles  = 
				triangles;
			mesh.RecalculateBounds();

			return mesh;
		}

		/// <summary>
		/// must be adjacent corners.
		/// </summary>
		/// <param name="corner1"></param>
		/// <param name="corner2"></param>
		private void AddTriangle(int corner1, int corner2)
		{
			triangles[triangleIndex++] = corner1;
			triangles[triangleIndex++] = corner2;
			triangles[triangleIndex++] = 0;
		}

		/// <summary>
		/// Travels around the polygon, corner-by-corner and puts corners in order 
		///  either clockwise or counterclockwise...currently no way to know which way :O
		/// </summary>
		public void OrderCorners()
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