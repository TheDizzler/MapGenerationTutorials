using System.Collections.Generic;
using UnityEngine;


namespace AtomosZ.Voronoi
{
	public class Polygon
	{
		public string name = "";
		public Color color;

		public Centroid centroid;
		public List<Corner> corners = new List<Corner>();
		/// <summary>
		/// Edges that seperate polygons
		/// </summary>
		public List<VEdge> voronoiEdges = new List<VEdge>();


		public Polygon(Centroid centroidSite)
		{
			centroid = centroidSite;

			foreach (var dTriangle in centroid.dTriangles)
			{
				var corner = dTriangle.GetCorner();
				corner.polygons.Add(this);
				corners.Add(corner);
				name += corner.num;

				foreach (var tri in centroid.dTriangles)
				{
					if (tri == dTriangle)
						continue;

					if (dTriangle.SharesEdgeWith(tri))
					{
						if (!corner.TryGetEdgeWith(tri.GetCorner(), out VEdge vEdge))
						{ // a new edge was created
							voronoiEdges.Add(vEdge);
						}
						else if (!voronoiEdges.Contains(vEdge))
							voronoiEdges.Add(vEdge);
					}
				}
			}
		}
	}




	public class DelaunayTriangle
	{
		public Centroid p1, p2, p3;
		/// <summary>
		/// Because the corners may be outside the map extremities, the actual triangle center
		/// may not be the same.
		/// </summary>
		public Vector2 realCenter;
		public float radius;
		public float radiusSqr;
		public List<DEdge> edges;

		private Corner corner;


		public DelaunayTriangle(Centroid p1, Centroid p2, Centroid p3, Vector2 center, float radius)
		{
			this.p1 = p1;
			this.p2 = p2;
			this.p3 = p3;
			p1.dTriangles.Add(this);
			p2.dTriangles.Add(this);
			p3.dTriangles.Add(this);

			realCenter = center;
			this.radius = radius;
			radiusSqr = radius * radius;
		}

		/// <summary>
		/// Gets corner. Creates if doesn't already exist.
		/// </summary>
		/// <returns></returns>
		public Corner GetCorner()
		{
			if (corner == null)
			{
				corner = new Corner(realCenter, VoronoiGraph.cornerCount++);
				if (!VoronoiGraph.uniqueCorners.Add(corner))
				{
					// this will do nothing. What we need to do is check the position. However, triangles SHOULD all unique anyway.
					//Debug.Log("Non-unique corner: " + this.center.num);
				}
			}

			return corner;
		}

		public void Destroy()
		{
			p1.dTriangles.Remove(this);
			p2.dTriangles.Remove(this);
			p3.dTriangles.Remove(this);
		}

		/// <summary>
		/// Returns any edges that have not been found yet.
		/// </summary>
		public List<DEdge> CalculateEdges()
		{
			List<DEdge> newEdges = new List<DEdge>();
			edges = new List<DEdge>();
			bool edgeFound = false;
			foreach (var edge in p1.connectedEdges)
			{
				if (edge.Contains(p2))
				{
					edges.Add(edge);
					edgeFound = true;
					break;
				}
			}

			if (!edgeFound)
			{
				DEdge edge = new DEdge(p1, p2);
				newEdges.Add(edge);
				edges.Add(edge);
			}
			else
				edgeFound = false;

			foreach (var edge in p1.connectedEdges)
			{
				if (edge.Contains(p3))
				{
					edges.Add(edge);
					edgeFound = true;
					break;
				}
			}

			if (!edgeFound)
			{
				DEdge edge = new DEdge(p1, p3);
				newEdges.Add(edge);
				edges.Add(edge);
			}
			else
				edgeFound = false;

			foreach (var edge in p2.connectedEdges)
			{
				if (edge.Contains(p3))
				{
					edges.Add(edge);
					edgeFound = true;
					break;
				}
			}

			if (!edgeFound)
			{
				DEdge edge = new DEdge(p2, p3);
				newEdges.Add(edge);
				edges.Add(edge);
			}

			return newEdges;
		}

		public DEdge[] GetEdgesConnectedTo(Centroid edgeConnection)
		{
			DEdge[] dEdges = new DEdge[2];
			int i = 0;
			foreach (var edge in edges)
				if (edge.end == edgeConnection || edge.start == edgeConnection)
					dEdges[i++] = edge;
			return dEdges;
		}

		public bool SharesEdgeWith(DelaunayTriangle tri)
		{
			foreach (DEdge edge in edges)
			{
				if (tri.edges.Contains(edge))
					return true;
			}
			return false;
		}
	}
}