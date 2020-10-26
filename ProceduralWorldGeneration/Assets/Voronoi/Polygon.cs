using System;
using System.Collections.Generic;
using AtomosZ.Tutorials.Voronoi;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	/// <summary>
	/// An area centered around a Centroid (DSite), composed of corners (VSite) and edges (VEdge)
	/// </summary>
	public class Polygon
	{
		public Centroid centroid;
		public List<Corner> corners = new List<Corner>();
		/// <summary>
		/// Edges that seperate polygons
		/// </summary>
		public List<VEdge> voronoiEdges = new List<VEdge>();
		public List<Corner> oobCorners = new List<Corner>();

		public bool isInvalid = false;


		public Polygon(Centroid centroidSite)
		{
			centroid = centroidSite;

			GetCornersAndEdges();
		}

		public string GetName()
		{
			string name = "";
			for (int i = 0; i < corners.Count; ++i)
			{
				name += corners[i].num;
				if (i != corners.Count - 1)
					name += "-";
			}

			return name;
		}

		private void GetCornersAndEdges()
		{
			foreach (var dTriangle in centroid.dTriangles)
			{
				if (dTriangle.isInvalidated)
					continue;
				var corner = dTriangle.GetCorner();
				if (corners.Contains(corner)) // corners get merged
					continue;
				VoronoiHelper.Associate(this, corner);

				foreach (var tri in centroid.dTriangles)
				{
					if (tri == dTriangle || tri.isInvalidated)
						continue;

					if (dTriangle.SharesEdgeWith(tri))
					{
						if (!corner.TryGetEdgeWith(tri.GetCorner(), out VEdge vEdge))
						{ // a new edge was created
							voronoiEdges.Add(vEdge);
							vEdge.polygons.Add(this);
						}
						else if (!voronoiEdges.Contains(vEdge))
						{
							voronoiEdges.Add(vEdge);
							vEdge.polygons.Add(this);
						}

					}
				}
			}

			if (voronoiEdges.Count < 3)
			{
				Invalidate();
			}
		}

		public void Invalidate()
		{
			Debug.Log("Invalid Polygon - destroy: " + GetName());
			isInvalid = true;
			foreach (var corner in corners)
				corner.polygons.Remove(this);
			corners = null;
			voronoiEdges = null;
			VoronoiGraph.invalidatedPolygons.Add(this);
		}
	}



	/// <summary>
	/// An area centered around a Corner (VSite), composed of centroids (DSite) and edges (DEdge)
	/// @TODO: find corners that are too close and merge them.
	/// </summary>
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
		/// <summary>
		/// Marks this triangle as no longer needed because its Centroid has been merged with another.
		/// </summary>
		public bool isInvalidated { get; private set; }

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
				if (isInvalidated)
					throw new System.Exception("Cannot create corner on an invalidated triangle");
				corner = new Corner(this, VoronoiGraph.cornerCount++);
				VoronoiGraph.uniqueCorners.Add(corner);
			}

			return corner;
		}


		public void Invalidate()
		{
			isInvalidated = true;
			Destroy();
			corner = null;
			edges = null;
			// may be safe to delete entirely
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