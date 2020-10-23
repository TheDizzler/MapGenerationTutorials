using System;
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
			// check if edges have end points oob
			if (!isInvalid)
			{
				if (VoronoiGenerator.fixCorners)
					FixOOBCorners();
			}
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
				corner.polygons.Add(this);
				corners.Add(corner);
				if (corner.isOOB)
					oobCorners.Add(corner);

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

		private void FixOOBCorners()
		{
			// does this polygon contain a map corner?
			for (int i = 0; i < voronoiEdges.Count; ++i)
			{
				var edge = voronoiEdges[i];
				if (VoronoiGenerator.TryGetBoundsIntersection(edge.start.position, edge.end.position,
					out Dictionary<VoronoiGenerator.MapSide, Vector2> intersections, out byte cornerByte))
				{
					if (intersections.Count == 2) // corner cutter
					{
						// find already created corner
						if (!VoronoiGraph.mapCorners.TryGetValue(cornerByte, out Corner newCorner))
						{
							throw new Exception("Couldn't find map corner!");
						}

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

						foreach (var oobCorner in oobCorners)
						{
							oobCorner.RemoveFrom(this);
							corners.Remove(oobCorner);
						}

						oobCorners.Clear();
						// the chances of having two corner cutters in one polygon is highly
						// unlikely and SHOULD be impossible with reasonable map constraints
						return;
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
				corner.isOOB = false;
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
				mergePoint.isOOB = false;
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
			for (int i = remove.connectedEdges.Count - 1; i >= 0; --i)
			{
				VEdge edge = remove.connectedEdges[i];
				if (edge == connectingEdge)
					continue;
				edge.ReplaceSite(remove, mergeInto);
			}

			remove.connectedEdges.Clear();

			// clean up polygons
			foreach (var polygon in remove.polygons)
			{
				polygon.voronoiEdges.Remove(connectingEdge);
				if (polygon.voronoiEdges.Count < 3)
				{
					polygon.Invalidate();
					continue;
				}

				if (!mergeInto.polygons.Contains(polygon))
				{
					mergeInto.polygons.Add(polygon);
					polygon.corners.Add(mergeInto);
				}

				polygon.corners.Remove(remove);
			}

			remove.polygons.Clear();

			if (remove.delaunayTriangle != null)
			{
				remove.delaunayTriangle.Invalidate();
			}
		}

		private void Invalidate()
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