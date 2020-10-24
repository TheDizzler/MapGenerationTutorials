using System.Collections.Generic;
using AtomosZ.Tutorials.Voronoi;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	/// <summary>
	/// aka a DSite, the point that a polygon is spawned around, and the corner of a DelaunayTriangle.
	/// </summary>
	public class Centroid : Site
	{
		private static int count = 0;

		public List<DEdge> connectedEdges = new List<DEdge>();
		public List<DelaunayTriangle> dTriangles = new List<DelaunayTriangle>();
		public int num;

		public Centroid(Vector2 pos) : base(pos)
		{
			num = count++;
		}
	}

	/// <summary>
	/// aka a VSite, a point that is a corner of a polygon, the center of a DelaunayTriangle, is triangulated by Centroids.
	/// Created in DelaunayTriangle.
	/// </summary>
	public class Corner : Site
	{
		/// <summary>
		/// Edges added at polygon creation phase.
		/// </summary>
		public List<VEdge> connectedEdges = new List<VEdge>();
		/// <summary>
		/// List of polygons that this contains this corner.
		/// </summary>
		public HashSet<Polygon> polygons = new HashSet<Polygon>();
		public DelaunayTriangle delaunayTriangle = null;
		public int num;
		public bool isOOB = false;
		public bool isMapCorner = false;

		public Corner(DelaunayTriangle triangle, int cornerNum) : base(triangle.realCenter)
		{
			this.delaunayTriangle = triangle;
			num = cornerNum;
			isOOB = !VoronoiGenerator.IsInMapBounds(position);
		}


		/// <summary>
		/// Used for creating artificial corners, i.e. ones that are forced
		/// instead of created through a DelaunayTriangle.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="cornerNum"></param>
		public Corner(Vector2 pos, int cornerNum, bool mapCorner = false) : base(pos)
		{
			num = cornerNum;
			isOOB = !VoronoiGenerator.IsInMapBounds(position);
			isMapCorner = mapCorner;
		}


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
			VoronoiGraph.uniqueVEdges.Add(sharedEdge);
			return false;
		}

		/// <summary>
		/// Retrieves all connected corners in inputed polygon. Should always equal 2.
		/// </summary>
		/// <param name="polygon"></param>
		/// <returns></returns>
		public List<Corner> GetConnectedCornersIn(Polygon polygon)
		{
			List<Corner> neighbours = new List<Corner>();
			foreach (var edge in connectedEdges)
			{
				Corner other = edge.GetOppositeSite(this);
				if (other.polygons.Contains(polygon))
					neighbours.Add(other);
			}

			if (neighbours.Count != 2) // this is probably a map corner
				throw new System.Exception("Corner has an unusual amount of neighbhours: " + neighbours.Count);

			return neighbours;
		}

		/// <summary>
		/// Retrieves all connected edges in polygon. Should always equal 2.
		/// </summary>
		/// <param name="polygon"></param>
		/// <returns></returns>
		public List<VEdge> GetConnectedEdgesIn(Polygon polygon)
		{
			List<VEdge> connections = new List<VEdge>();
			foreach (var edge in connectedEdges)
			{
				if (edge.GetOppositeSite(this).polygons.Contains(polygon))
					connections.Add(edge);
			}

			return connections;
		}

		public void RemoveFrom(Polygon polygon)
		{
			foreach (var edge in GetConnectedEdgesIn(polygon))
				edge.polygons.Remove(polygon);
			polygons.Remove(polygon);
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
}