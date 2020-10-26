using System.Collections.Generic;
using System.Linq;
using AtomosZ.Tutorials.Voronoi;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	public class VoronoiGraph
	{
		public static HashSet<Corner> uniqueCorners;
		public static Dictionary<byte, Corner> mapCorners;
		public static HashSet<VEdge> uniqueVEdges;
		public static int cornerCount = 0;
		public static List<Polygon> invalidatedPolygons;
		public static HashSet<VEdge> invalidatedEdges;

		private static DelaunayGraph delaunayGraph;

		public List<Polygon> polygons;
		private List<Corner> removeCorners;


		public VoronoiGraph(DelaunayGraph dGraph)
		{
			delaunayGraph = dGraph;
			cornerCount = 0;
			uniqueVEdges = new HashSet<VEdge>();
			uniqueCorners = new HashSet<Corner>();
			removeCorners = new List<Corner>();
			mapCorners = new Dictionary<byte, Corner>()
			{
				[VoronoiGenerator.TopRightCornerByte] = new Corner(VoronoiGenerator.topRight, cornerCount++, true),
				[VoronoiGenerator.TopLeftCornerByte] = new Corner(VoronoiGenerator.topLeft, cornerCount++, true),
				[VoronoiGenerator.BottomRightCornerByte] = new Corner(VoronoiGenerator.bottomRight, cornerCount++, true),
				[VoronoiGenerator.BottomLeftCornerByte] = new Corner(VoronoiGenerator.bottomLeft, cornerCount++, true),
			};

			uniqueCorners.Add(mapCorners[VoronoiGenerator.TopRightCornerByte]);
			uniqueCorners.Add(mapCorners[VoronoiGenerator.TopLeftCornerByte]);
			uniqueCorners.Add(mapCorners[VoronoiGenerator.BottomRightCornerByte]);
			uniqueCorners.Add(mapCorners[VoronoiGenerator.BottomLeftCornerByte]);


			invalidatedPolygons = new List<Polygon>();
			invalidatedEdges = new HashSet<VEdge>();

			polygons = new List<Polygon>();
			for (int i = 4; i < dGraph.centroids.Count; ++i) // first 4 centroids are map corners
			{
				Polygon poly = new Polygon(dGraph.centroids[i]);
				polygons.Add(poly);
			}

			if (VoronoiGenerator.fixEdges)
			{
				FixCorners();
			}

			int culled = 0;
			foreach (var polygon in invalidatedPolygons)
			{
				polygons.Remove(polygon);
				++culled;
			}

			for (int i = removeCorners.Count - 1; i >= 0; --i)
			{
				Corner removingCorner = removeCorners[i];
				for (int j = removingCorner.connectedEdges.Count - 1; j >= 0; --j)
				{
					VEdge edge = removingCorner.connectedEdges[j];
					removingCorner.connectedEdges.Remove(edge);
					uniqueVEdges.Remove(edge);
					Corner opposite = edge.GetOppositeSite(removingCorner);
					opposite.connectedEdges.Remove(edge);
					foreach (var poly in removingCorner.polygons)
					{
						poly.voronoiEdges.Remove(edge);
					}
				}

				removingCorner.connectedEdges.Clear();

				RemoveCorner(removingCorner);
			}
		}


		public static bool TryGetNearCorner(Vector2 position, out Corner closeCorner)
		{
			foreach (var corner in uniqueCorners)
			{
				if ((corner.position - position).sqrMagnitude < VoronoiGenerator.minSqrDistBetweenCorners)
				{
					closeCorner = corner;
					return true;
				}
			}

			closeCorner = null;
			return false;
		}

		private void FixCorners()
		{
			foreach (var polygon in polygons)
			{
				if (polygon.isInvalid || polygon.oobCorners.Count == 0)
					continue;
				// find polygons that are cutting off a map edge and
				// move a corner to the map corner to get the polygon nice and snug
				foreach (var edge in polygon.voronoiEdges)
				{
					if (edge.polygons.Count != 1)
						continue; // by necessity, map corner enclosing edges have only one polygon

					if (!(edge.start.isOOB ^ edge.end.isOOB))
						continue; // edge completely OOB || edge is completely inbounds, therefore uninteresting to us

					if (edge.start.isMapCorner || edge.end.isMapCorner)
						continue; // make sure we only change this edge once - may be redundant

					if (!VoronoiGenerator.TryGetClosestCorner(edge, out byte cornerByte))
						continue;

					Corner mapCorner = mapCorners[cornerByte];
					Corner oobCorner = edge.start.isOOB ? edge.start : edge.end;
					Corner insideCorner = edge.start.isOOB ? edge.end : edge.start;

					// replace in bounds corner with mapCorner
					for (int i = insideCorner.connectedEdges.Count - 1; i >= 0; --i)
					{
						var modifiedEdge = insideCorner.connectedEdges[i];
						modifiedEdge.ReplaceSite(insideCorner, mapCorner);
					}

					foreach (var movedPolygon in insideCorner.polygons)
					{
						VoronoiHelper.Associate(movedPolygon, mapCorner);
						movedPolygon.corners.Remove(insideCorner);
					}

					RemoveCorner(insideCorner);

					break; // have to break because we made an addition to the edge list. Should be finished here anyway.
				}
			}

			// start at corner and choose a polygon
			Polygon currentPolygon = null;
			Corner lastCorner = null;
			bool found = false;
			foreach (var mapCorner in mapCorners)
			{
				foreach (var polygon in mapCorner.Value.polygons)
				{
					currentPolygon = polygon;
					lastCorner = mapCorner.Value;
					found = true;
					break;
				}

				if (found)
					break;
			}

			if (currentPolygon == null)
				throw new System.Exception("Could not find a valid map corner with polygons");

			Corner firstCorner = lastCorner;
			while (true)
			{
				// find other edge that intersects map border
				if (!VoronoiGenerator.TryGetBoundsIntersection(currentPolygon, out Dictionary<VEdge, Vector2> intersections)
					|| intersections.Count != 1)
				{
					if (currentPolygon.corners.Any(corner => corner.isMapCorner))
					{ // border done -> go to next corner
						Corner mapCorner = currentPolygon.corners.FirstOrDefault(corner => corner.isMapCorner);

						lastCorner.TryGetEdgeWith(mapCorner, out VEdge newBorderEdge);
						VoronoiHelper.Associate(currentPolygon, newBorderEdge);
						lastCorner = mapCorner;
					}
					else
					{
						// this should be a polygon with an corner-cutting edge
						if (!TryGetCornerCutterEdge(currentPolygon, out VEdge cornerCutterEdge, out byte cornerByte))
						{
							throw new System.Exception("Invalid attempt to get a corner cutter edge");
						}

						// find other edge intersection
						VEdge otherEdge = null;
						Vector2 intersectionPoint = Vector2.negativeInfinity;
						foreach (var edge in intersections)
						{
							if (edge.Key != cornerCutterEdge)
							{
								otherEdge = edge.Key;
								intersectionPoint = edge.Value;
							}
						}

						// create new edges using mapCorner and other intersection point
						BisectEdge(otherEdge, intersectionPoint, out Corner newCorner, out VEdge newEdge);
						Corner mapCorner = mapCorners[cornerByte];
						lastCorner.TryGetEdgeWith(mapCorner, out VEdge newBorderEdge1);
						mapCorner.TryGetEdgeWith(newCorner, out VEdge newBorderEdge2);
						VoronoiHelper.Associate(currentPolygon, mapCorner);
						VoronoiHelper.Associate(currentPolygon, newCorner);

						VoronoiHelper.Associate(currentPolygon, newBorderEdge1);
						VoronoiHelper.Associate(currentPolygon, newBorderEdge2);

						currentPolygon.voronoiEdges.Remove(cornerCutterEdge);
						uniqueVEdges.Remove(cornerCutterEdge);

						lastCorner = newCorner; // this causes an infinite loop!
					}
				}
				else
				{
					VEdge edge = null;
					Vector2 intersectPoint = Vector2.zero;
					foreach (var vv in intersections)
					{
						edge = vv.Key;
						intersectPoint = vv.Value;
					}

					// create new Corner at intersect point and bisect edge
					BisectEdge(edge, intersectPoint, out Corner newCorner, out VEdge newOOBEdge);
					VoronoiHelper.Associate(currentPolygon, newCorner);

					// connect first corner to new point
					lastCorner.TryGetEdgeWith(newCorner, out VEdge newBorderEdge);
					VoronoiHelper.Associate(currentPolygon, newBorderEdge);
					lastCorner = newCorner;
				}

				// mark all remaining oob corners and edges in polygon to removed
				foreach (var corner in currentPolygon.corners)
					if (corner.isOOB)
						removeCorners.Add(corner);

				if (firstCorner == lastCorner)
				{ // done!
					break;
				}

				// move to next polygon connected to bisected edge
				found = false;
				foreach (var polygon in lastCorner.polygons)
				{
					if (polygon != currentPolygon)
					{
						found = true;
						currentPolygon = polygon;
						break;
					}
				}
			}
		}

		private bool TryGetCornerCutterEdge(Polygon polygon,
			out VEdge cornerCutterEdge, out byte cornerByte)
		{
			foreach (var edge in polygon.voronoiEdges)
			{
				if (VoronoiGenerator.TryGetBoundsIntersection(edge.start.position, edge.end.position,
						out Dictionary<VoronoiGenerator.MapSide, Vector2> intersections, out cornerByte))
				{
					if (intersections.Count == 2) // corner cutter
					{
						cornerCutterEdge = edge;
						return true;
					}
				}
			}

			cornerCutterEdge = null;
			cornerByte = 0;
			return false;
		}

		/// <summary>
		/// <para>edge: becomes completely INBOUND edge.</para>
		/// <para>intersectPoint: where new corner is created.</para>
		/// <para>newEdge: new, compeletely OOB edge.</para>
		/// </summary>
		/// <param name="edge">becomes completely INBOUND edge</param>
		/// <param name="intersectPoint">where new corner is created</param>
		/// <param name="newCorner"></param>
		/// <param name="newEdge">new, compeletely OOB edge</param>
		private void BisectEdge(VEdge edge, Vector2 intersectPoint, out Corner newCorner, out VEdge newEdge)
		{
			newCorner = new Corner(intersectPoint, cornerCount++);
			uniqueCorners.Add(newCorner);
			Corner oobCorner = edge.start.isOOB ? edge.start : edge.end;
			edge.ReplaceSite(oobCorner, newCorner);
			newCorner.TryGetEdgeWith(oobCorner, out newEdge);
		}


		public static void RemoveCorner(Corner corner)
		{
			foreach (var polygon in corner.polygons)
			{
				polygon.corners.Remove(corner);
				polygon.oobCorners.Remove(corner);
				if (polygon.corners.Count < 3)
					polygon.Invalidate();
			}

			corner.polygons.Clear();

			if (corner.delaunayTriangle != null)
			{
				corner.delaunayTriangle.Invalidate();
				delaunayGraph.triangles.Remove(corner.delaunayTriangle);
				corner.delaunayTriangle = null;
			}

			uniqueCorners.Remove(corner);
		}
	}
}