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
							VoronoiHelper.Associate(this, vEdge);
						}
						else if (!voronoiEdges.Contains(vEdge))
						{
							VoronoiHelper.Associate(this, vEdge);
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
		/// Corners may get merged or removed, so 
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
			if (isInvalidated)
				throw new System.Exception("Cannot get corner on an invalidated triangle");

			if (corner == null)
			{
				if (!VoronoiGraph.TryGetNearCorner(realCenter, out corner))
				{
					corner = new Corner(this, VoronoiGraph.cornerCount++);
					VoronoiGraph.uniqueCorners.Add(corner);
					if (!corner.isOOB)
					{
						bool isCloseToTop = VoronoiGenerator.topRight.y - realCenter.y < VoronoiGenerator.minDistCornerAndBorder;
						bool isCloseToRight = VoronoiGenerator.topRight.x - realCenter.x < VoronoiGenerator.minDistCornerAndBorder;
						bool isCloseToLeft = realCenter.x - VoronoiGenerator.bottomLeft.x < VoronoiGenerator.minDistCornerAndBorder;
						bool isCloseToBottom = realCenter.y - VoronoiGenerator.bottomLeft.y < VoronoiGenerator.minDistCornerAndBorder;
						if (isCloseToTop)
						{
							if (isCloseToRight)
							{
								VoronoiGraph.uniqueCorners.Remove(corner);
								--VoronoiGraph.cornerCount;
								corner = VoronoiGraph.mapCorners[VoronoiGenerator.TopRightCornerByte];
							}
							else if (isCloseToLeft)
							{
								VoronoiGraph.uniqueCorners.Remove(corner);
								--VoronoiGraph.cornerCount;
								corner = VoronoiGraph.mapCorners[VoronoiGenerator.TopLeftCornerByte];
							}
							else
							{
								corner.position.y = VoronoiGenerator.topRight.y;
							}
						}
						else if (isCloseToBottom)
						{
							if (isCloseToRight)
							{
								VoronoiGraph.uniqueCorners.Remove(corner);
								--VoronoiGraph.cornerCount;
								corner = VoronoiGraph.mapCorners[VoronoiGenerator.BottomRightCornerByte];
							}
							else if (isCloseToLeft)
							{
								VoronoiGraph.uniqueCorners.Remove(corner);
								--VoronoiGraph.cornerCount;
								corner = VoronoiGraph.mapCorners[VoronoiGenerator.BottomLeftCornerByte];
							}
							else
							{
								corner.position.y = VoronoiGenerator.bottomRight.y;
							}
						}
						else if (isCloseToRight)
						{
							corner.position.x = VoronoiGenerator.bottomRight.x;
						}
						else if (isCloseToLeft)
						{
							corner.position.x = VoronoiGenerator.bottomLeft.x;
						}
					}
				}
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