using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;
using AtomosZ.Voronoi.Regions;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	/// <summary>
	/// An area centered around a Centroid (DSite), composed of corners (VSite) and edges (VEdge)
	/// </summary>
	public class Polygon
	{
		public static int count = 0;

		public int id = 0;
		public Centroid centroid;
		public List<Corner> corners = new List<Corner>();
		/// <summary>
		/// Edges that seperate polygons.
		/// </summary>
		private List<VEdge> voronoiEdges = new List<VEdge>();
		public List<Corner> oobCorners = new List<Corner>();

		/// <summary>
		/// Polygon has at least one corner on a map border.
		/// </summary>
		public bool isOnBorder;
		public bool isInvalid = false;

		public Region region;


		public Polygon(Centroid centroidSite)
		{
			id = count++;
			centroid = centroidSite;

			GetCornersAndEdges();
		}

		public string GetName()
		{
			string name = id +": ";
			for (int i = 0; i < corners.Count; ++i)
			{
				name += corners[i].num;
				if (i != corners.Count - 1)
					name += "-";
			}

			return name;
		}


		public List<VEdge> GetVoronoiEdges()
		{
			return voronoiEdges;
		}

		public void Add(VEdge edge)
		{
			voronoiEdges.Add(edge);
		}

		public void Remove(VEdge edge)
		{
			voronoiEdges.Remove(edge);
		}

		public bool Contains(VEdge edge)
		{
			return voronoiEdges.Contains(edge);
		}

		public void SortEdges()
		{
			// Sort edges
			List<VEdge> newOrder = new List<VEdge>();
			List<Corner> newCornerOrder = new List<Corner>();
			VEdge last = voronoiEdges[0];
			newOrder.Add(last);
			voronoiEdges.Remove(last);

			for (int i = 0; i < voronoiEdges.Count;)
			{
				VEdge current = voronoiEdges[i];
				if (current.SharesCorner(last, out Corner sharedCorner))
				{
					newCornerOrder.Add(sharedCorner);
					newOrder.Add(current);
					voronoiEdges.Remove(current);
					last = current;
					i = 0;
				}
				else
					++i;
			}

			if (voronoiEdges.Count != 0)
				throw new System.Exception("Faaaaawk");

			if (!last.SharesCorner(newOrder[0], out Corner shared))
				throw new System.Exception("All hell broke loose");
			newCornerOrder.Insert(0, shared);

			if (newCornerOrder.Count != corners.Count)
				throw new System.Exception("Fawrk");

			corners = newCornerOrder;
			voronoiEdges = newOrder;

			for (int i = 1; i < voronoiEdges.Count; ++i)
			{
				if (!voronoiEdges[i].SharesCorner(voronoiEdges[i - 1], out Corner noneed))
					Debug.Log("Srsly?");
			}

			for (int i = 1; i < corners.Count; ++i)
			{
				if (newCornerOrder[i].FindSharedEdgeWith(newCornerOrder[i - 1]) == null)
				{
					throw new System.Exception("All hell broke loose");
				}
			}
		}

		private void GetCornersAndEdges()
		{
			foreach (var dTriangle in centroid.dTriangles)
			{
				if (dTriangle.isInvalidated) // this can't happen at this point....
					continue;
				var corner = dTriangle.GetCorner();
				VoronoiHelper.Associate(this, corner);

				foreach (var tri in centroid.dTriangles)
				{
					if (tri == dTriangle || tri.isInvalidated)
						continue;

					if (dTriangle.SharesEdgeWith(tri))
					{
						corner.TryGetEdgeWith(tri.GetCorner(), out VEdge vEdge);
						if (!voronoiEdges.Contains(vEdge))
						{
							VoronoiHelper.Associate(this, vEdge);
						}
					}
				}
			}

			if (voronoiEdges.Count < 3)
			{
				Invalidate();
				return;
			}
		}


		public void Invalidate()
		{
			Debug.Log("Invalid Polygon - destroy: " + GetName());
			isInvalid = true;
			foreach (var corner in corners)
				corner.polygons.Remove(this);
			foreach (var edge in voronoiEdges)
				edge.Remove(this);
			corners = null;
			voronoiEdges = null;
			VoronoiGraph.invalidatedPolygons.Add(this);
		}

		public void CenterCentroid()
		{
			Vector3 total = Vector3.zero;
			int count = 0;
			foreach (var corner in corners)
			{
				total += corner.position;
				++count;
			}

			centroid.position = total / count;
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
		public Vector3 realCenter;
		public float radius;
		public float radiusSqr;
		public List<DEdge> edges;
		/// <summary>
		/// Marks this triangle as no longer needed because its Centroid has been merged with another.
		/// </summary>
		public bool isInvalidated { get; private set; }

		private Corner corner;


		public DelaunayTriangle(Centroid p1, Centroid p2, Centroid p3, Vector3 center, float radius)
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
				// check if this corner is close to a border/map corner
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
							corner.isOnBorder = true;
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
							corner.isOnBorder = true;
						}
					}
					else if (isCloseToRight)
					{
						corner.position.x = VoronoiGenerator.bottomRight.x;
						corner.isOnBorder = true;
					}
					else if (isCloseToLeft)
					{
						corner.position.x = VoronoiGenerator.bottomLeft.x;
						corner.isOnBorder = true;
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
		/// Returns any edges that have not been found yet (i.e. new edges).
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