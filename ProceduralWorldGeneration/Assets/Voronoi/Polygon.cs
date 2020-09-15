using System.Collections.Generic;
using UnityEngine;


namespace AtomosZ.Voronoi
{
	public class Polygon
	{
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
			//List<DelaunayTriangle> checkedTriangles = new List<DelaunayTriangle>();
			foreach (var dTriangle in centroid.dTriangles)
			{
				var corner = dTriangle.center;
				corner.polygons.Add(this);
				corners.Add(corner);


				foreach (var tri in centroid.dTriangles)
				{
					if (tri == dTriangle)
						continue;

					if (dTriangle.SharesEdgeWith(tri))
					{
						if (!corner.TryGetEdgeWith(tri.center, out VEdge vEdge))
						{ // a new edge was created
							voronoiEdges.Add(vEdge);
						}
						else if (!voronoiEdges.Contains(vEdge))
							voronoiEdges.Add(vEdge);
					}
				}
			}

			foreach (var corner in corners)
			{

			}
		}
	}

	/// <summary>
	/// aka a DSite, the point that a polygon is spawned around.
	/// </summary>
	public class Centroid : Site
	{
		public List<DEdge> connectedEdges = new List<DEdge>();
		public List<DelaunayTriangle> dTriangles = new List<DelaunayTriangle>();

		public Centroid(Vector2 pos) : base(pos) { }
	}

	/// <summary>
	/// aka a VSite, a point that is a corner of a polygon.
	/// </summary>
	public class Corner : Site
	{
		public List<VEdge> connectedEdges = new List<VEdge>();
		/// <summary>
		/// List of polygons that this contains this corner.
		/// </summary>
		public List<Polygon> polygons = new List<Polygon>();


		public Corner(Vector2 pos) : base(pos) { }

		/// <summary>
		/// Check if a VEdge already exists between the two corners.
		/// Returns the edge if already exists and creates it if it doesn't.
		/// </summary>
		/// <param name="other"></param>
		/// <param name="sharedEdge"></param>
		/// <returns></returns>
		public bool TryGetEdgeWith(Corner other, out VEdge sharedEdge)
		{
			foreach (VEdge edge in connectedEdges)
			{
				if (other.connectedEdges.Contains(edge))
				{
					sharedEdge = edge;
					return true;
				}
			}

			sharedEdge = new VEdge(this, other);

			return false;
		}
	}

	public abstract class Site
	{
		public Vector2 position;


		public Site(Vector2 pos)
		{
			position = pos;
		}
	}


	public class VEdge : Edge<Corner>
	{
		public VEdge(Corner p1, Corner p2) : base(p1, p2)
		{
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}
	}

	public class DEdge : Edge<Centroid>
	{
		public DEdge(Centroid p1, Centroid p2) : base(p1, p2)
		{
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}
	}

	/// <summary>
	/// A line that connects to sites.
	/// </summary>
	public abstract class Edge<TSite> where TSite : Site
	{
		public TSite start, end;


		public Edge(TSite p1, TSite p2)
		{
			start = p1;
			end = p2;
		}

		public bool Contains(TSite p2)
		{
			return start == p2 || end == p2;
		}
	}

	public class DelaunayTriangle
	{
		public Centroid p1, p2, p3;
		public Corner center; // can't construct corners here as they are shared between triangles.
		public float radius;
		public float radiusSqr;
		public List<DEdge> edges;


		public DelaunayTriangle(Centroid p1, Centroid p2, Centroid p3, Vector2 center, float radius)
		{
			this.p1 = p1;
			this.p2 = p2;
			this.p3 = p3;
			p1.dTriangles.Add(this);
			p2.dTriangles.Add(this);
			p3.dTriangles.Add(this);
			this.center = new Corner(center);
			this.radius = radius;
			radiusSqr = radius * radius;
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