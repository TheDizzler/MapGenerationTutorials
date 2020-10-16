using System.Collections.Generic;
using AtomosZ.Tutorials.Voronoi;
using UnityEngine;


namespace AtomosZ.Voronoi
{
	/// <summary>
	/// An area centered around a Centroid (DSite), composed of corners (VSite) and edges (VEdge)
	/// </summary>
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

			GetCornersAndEdges();
			// check if edges have end points oob
			if (VoronoiGenerator.fixCorners)
				FixOoBCorners();
		}

		private void GetCornersAndEdges()
		{
			foreach (var dTriangle in centroid.dTriangles)
			{
				var corner = dTriangle.GetCorner();
				corner.polygons.Add(this);
				corners.Add(corner);
				name += corner.num;

				foreach (var tri in centroid.dTriangles)
				{
					if (tri == dTriangle || tri.isInvalidated)
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

			if (voronoiEdges.Count < 3)
				Debug.Log("Invalid Polygon - destroy");
		}

		private void FixOoBCorners()
		{
			for (int i = 0; i < voronoiEdges.Count; ++i)
			{
				var edge = voronoiEdges[i];
				if (VoronoiGenerator.TryGetBoundsIntersection(edge.start.position, edge.end.position,
					out Dictionary<VoronoiGenerator.MapSide, Vector2> intersections, out byte corner))
				{
					if (intersections.Count == 2) // corner cutter
					{
						// create new corner at map corner
						Vector2 newPos;
						switch (corner)
						{
							case VoronoiGenerator.TopRight:
								newPos = VoronoiGenerator.mapBounds.max;
								break;
							case VoronoiGenerator.TopLeft:
								newPos = new Vector2(VoronoiGenerator.mapBounds.xMin, VoronoiGenerator.mapBounds.yMax);
								break;
							case VoronoiGenerator.BottomLeft:
								newPos = VoronoiGenerator.mapBounds.min;
								break;
							case VoronoiGenerator.BottomRight:
								newPos = new Vector2(VoronoiGenerator.mapBounds.xMax, VoronoiGenerator.mapBounds.yMin);
								break;
							default:
								newPos = Vector2.zero;
								continue;
						}

						Corner newCorner = new Corner(newPos, VoronoiGraph.cornerCount++);
						corners.Add(newCorner);
						newCorner.polygons.Add(this);
						VoronoiGraph.uniqueCorners.Add(newCorner);

						Corner p1 = edge.start;
						Corner p2 = edge.end;

						var edges = p1.GetConnectedEdgesIn(this);
						VEdge p1OtherEdge = edges[0] == edge ? edges[1] : edges[0];
						edges = p2.GetConnectedEdgesIn(this);
						VEdge p2OtherEdge = edges[0] == edge ? edges[1] : edges[0];

						// replace end point with new corner
						edge.ReplaceSite(p2, newCorner);
						newCorner.TryGetEdgeWith(p2, out VEdge newEdge);
						voronoiEdges.Add(newEdge);

						MovePointToBorderIntersect(p1, p1OtherEdge);
						MovePointToBorderIntersect(p2, p2OtherEdge);

						// the chances of having two corner cutters in one polygon is highly
						// unlikely and SHOULD be impossible with reasonable map constraints
						break;
					}
				}
			}
		}



		private void MovePointToBorderIntersect(Corner corner, VEdge otherEdge)
		{
			// move original corners to border of map
			if (!VoronoiGenerator.TryGetFirstMapBoundsIntersection(
				corner.position, otherEdge.GetOppositeSite(corner).position, out Vector2 intersectPoint))
			{
				// this edge is completely oob. Corners need merging.
				// The good news is we don't have to worry about reading modified lists.
				FindOtherOoBCornerAndMerge(corner, otherEdge);
			}
			else
			{
				corner.position = intersectPoint;
			}
		}

		private void FindOtherOoBCornerAndMerge(Corner mergePoint, VEdge connectedEdge)
		{
			// find other corner/edge
			Corner removePoint = connectedEdge.GetOppositeSite(mergePoint);
			var edges = removePoint.GetConnectedEdgesIn(this);
			VEdge p3OtherEdge = edges[0] == connectedEdge ? edges[1] : edges[0];


			if (!VoronoiGenerator.TryGetFirstMapBoundsIntersection(
				removePoint.position, p3OtherEdge.GetOppositeSite(removePoint).position, out Vector2 intersectPoint))
			{
				//Debug.LogWarning("Oh ffs");
				MergeCorners(mergePoint, removePoint);
				FindOtherOoBCornerAndMerge(mergePoint, p3OtherEdge);
			}
			else
			{
				mergePoint.position = intersectPoint;
				MergeCorners(mergePoint, removePoint);
			}

			VoronoiGraph.uniqueCorners.Remove(removePoint);
			VoronoiGraph.uniqueVEdges.Remove(connectedEdge);
		}

		private void MergeCorners(Corner mergeInto, Corner remove)
		{
			if (!mergeInto.TryGetEdgeWith(remove, out VEdge connectingEdge))
				throw new System.Exception("Can't merge two corners that don't share an edge");

			// merge all connected edges
			foreach (var edge in remove.connectedEdges)
			{
				if (edge == connectingEdge)
					continue;
				edge.ReplaceSite(remove, mergeInto);
				mergeInto.connectedEdges.Add(edge);
			}

			remove.connectedEdges = null;

			// clean up polygons
			foreach (var polygon in remove.polygons)
			{
				polygon.voronoiEdges.Remove(connectingEdge);
				if (!mergeInto.polygons.Contains(polygon))
				{
					mergeInto.polygons.Add(polygon);
					polygon.corners.Add(mergeInto);
				}

				polygon.corners.Remove(remove);
			}

			remove.polygons = null;

			if (remove.delaunayTriangle != null)
			{
				remove.delaunayTriangle.Invalidate();
			}
		}
	}



	/// <summary>
	/// An area centered around a Corner (VSite), composed of centroids (DSite) and edges (DEdge)
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
				corner = new Corner(this, VoronoiGraph.cornerCount++);
				if (!VoronoiGraph.uniqueCorners.Add(corner))
				{
					// this will do nothing. What we need to do is check the position. However, triangles SHOULD all unique anyway.
					//Debug.Log("Non-unique corner: " + this.center.num);
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