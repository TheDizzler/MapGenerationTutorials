using System.Collections.Generic;
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

			if (VoronoiGenerator.MergeNearCorners)
				MergeNearCorners();

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

		private void MergeNearCorners()
		{
			foreach (var corner in uniqueCorners)
			{
				if (corner.isInvalidated)
					continue;
				if (TryGetNearCorner(corner, out Corner closeCorner))
				{
					VoronoiHelper.MergeCorners(closeCorner, corner);
					removeCorners.Add(closeCorner);
				}
			}
		}

		private bool TryGetNearCorner(Corner testCorner, out Corner closeCorner)
		{
			foreach (var corner in uniqueCorners)
			{
				if (corner == testCorner)
					continue;
				if (corner.isInvalidated)
					continue;
				if ((corner.position - testCorner.position).sqrMagnitude < VoronoiGenerator.minSqrDistBetweenCorners)
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
			Polygon currentPolygon = null;
			Corner lastCorner = null;
			bool found = false;

			foreach (var polygon in polygons)
			{
				if (polygon.isInvalid || polygon.oobCorners.Count == 0)
					continue;

				foreach (var edge in polygon.voronoiEdges)
				{
					if (!(edge.start.isOOB ^ edge.end.isOOB))
						continue; // edge completely OOB || edge is completely inbounds, therefore uninteresting to us

					currentPolygon = polygon;
					lastCorner = edge.start.isOOB ? edge.end : edge.start;
					if (!lastCorner.isOnBorder)
					{
						// create a new on border corner to start on
						VoronoiGenerator.TryGetFirstMapBoundsIntersection(edge.start.position, edge.end.position, out Vector2 intersectPoint);
						BisectEdge(edge, intersectPoint, out Corner newCorner, out VEdge newEdge);
						lastCorner = newCorner;
					}

					found = true;
					break;
				}

				if (found)
					break;
			}

			List<Polygon> visited = new List<Polygon>();
			Corner firstCorner = lastCorner;
			VEdge last = null;
			while (found)
			{
				VEdge sharedEdge = null;
				visited.Add(currentPolygon);
				// find other edge that intersects map border
				if (!VoronoiGenerator.TryGetBoundsIntersection(currentPolygon, out Dictionary<VEdge, Vector2> intersections))
				{
					// if no intersections probably means that corners have already been corrected.
					// find other border corner and move on
					Debug.Log("No intersections here");
					found = false;
					foreach (var otherCorner in currentPolygon.corners)
					{
						if (otherCorner == lastCorner)
							continue;
						if (otherCorner.isOnBorder)
						{
							Debug.Log("\tfound border corner");
							if (!lastCorner.TryGetEdgeWith(otherCorner, out VEdge newBorderEdge))
								VoronoiHelper.Associate(currentPolygon, newBorderEdge);
							else
								Debug.Log("edge already existed?");
							if (newBorderEdge == last)
								Debug.LogError("WTF!");
							found = true;
							sharedEdge = null;
							foreach (var edge in otherCorner.connectedEdges)
								if (edge.Contains(currentPolygon) && edge.GetPolygonCount() > 1)
									sharedEdge = edge;
							if (sharedEdge == null)
								Debug.Log("Oh shit");
							lastCorner = otherCorner;
							break;
						}
					}

					if (!found)
					{
						Debug.Log("\tthis polygon is probably only connected to border by one corner");
						foreach (var edge in currentPolygon.voronoiEdges)
						{
							if (edge.GetPolygonCount() == 1)
							{
								Debug.Log("We have a corner polygon!");
								// get corner that's closest to mapCorner
								Corner closestCorner = VoronoiGenerator.GetClosestCornerToMapCorner(currentPolygon, out Corner mapCorner);
								// merge corner with mapCorner
								for (int i = closestCorner.connectedEdges.Count - 1; i >= 0; --i)
									closestCorner.connectedEdges[i].ReplaceSite(closestCorner, mapCorner);

								closestCorner.isInvalidated = true;
								removeCorners.Add(closestCorner);

								sharedEdge = null;
								foreach (var otherEdge in mapCorner.connectedEdges)
									if (otherEdge.Contains(currentPolygon) && otherEdge.GetPolygonCount() > 1)
										sharedEdge = otherEdge;
								if (sharedEdge == null)
									Debug.Log("Oh shit");
								lastCorner = mapCorner;
								break;
							}
						}
					}
				}
				else if (intersections.Count >= 2)
				{
					if (!TryGetCornerCutterEdge(currentPolygon, out VEdge cornerCutterEdge, out byte cornerByte))
					{
						Debug.Log("Special case corner polygon");
						// get closest map corner
						float closestSqrDist = float.MaxValue;
						Corner mapCorner = null;
						foreach (var kvp in mapCorners)
						{
							float dist = (currentPolygon.centroid.position - kvp.Value.position).sqrMagnitude;
							if (dist < closestSqrDist)
							{
								closestSqrDist = dist;
								mapCorner = kvp.Value;
							}
						}

						VEdge cornerCutEdge = null;
						VEdge otherEdge = null;
						Vector2 intersectPoint = Vector2.negativeInfinity;
						foreach (var vv in intersections)
						{
							if (vv.Key.start.isOnBorder || vv.Key.end.isOnBorder)
							{
								cornerCutEdge = vv.Key;
							}
							else
							{
								otherEdge = vv.Key;
								intersectPoint = vv.Value;
							}
						}

						if (cornerCutEdge == null)
						{
							Debug.Log("Starting on corner cut?");
							break;
						}
						else
						{
							// create new edge to mapCorner and add to polygon
							BisectEdge(cornerCutEdge, mapCorner, out VEdge newBorderEdge);
							// create new corner at intersection point (non-corner-cutter) and attach to mapCorner
							BisectEdge(otherEdge, intersectPoint, out Corner newCorner, out VEdge newEdge);

							mapCorner.TryGetEdgeWith(newCorner, out VEdge newOtherBorderEdge);
							VoronoiHelper.Associate(currentPolygon, newBorderEdge);
							VoronoiHelper.Associate(currentPolygon, newOtherBorderEdge);

							sharedEdge = otherEdge;
							lastCorner = newCorner;
						}
					}
					else
					{
						Debug.Log("Corner cutter!");

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

						VoronoiHelper.Associate(currentPolygon, newBorderEdge1);
						VoronoiHelper.Associate(currentPolygon, newBorderEdge2);

						currentPolygon.voronoiEdges.Remove(cornerCutterEdge);
						uniqueVEdges.Remove(cornerCutterEdge);

						sharedEdge = otherEdge;
						lastCorner = newCorner;
					}
				}
				else
				{
					// this may be a corner
					VEdge otherEdge = null;
					Vector2 intersectPoint = Vector2.zero;
					foreach (var vv in intersections)
					{
						otherEdge = vv.Key;
						intersectPoint = vv.Value;
					}

					if (otherEdge.GetPolygonCount() == 1)
					{
						Debug.Log("this is likely a special corner case");
						// get closest mapCorner
						Corner closestCorner = VoronoiGenerator.GetClosestCornerToMapCorner(currentPolygon, out Corner mapCorner);
						BisectEdge(otherEdge, mapCorner, out VEdge oobEdge);
						lastCorner.TryGetEdgeWith(mapCorner, out VEdge borderEdge);
						VoronoiHelper.Associate(currentPolygon, borderEdge);

						Corner otherCorner = otherEdge.GetOppositeSite(mapCorner);
						if (!otherCorner.isOnBorder)
						{
							// get border that is perpendicular to borderEdge and move it to the border
							Vector2 dir = (mapCorner.position - lastCorner.position).normalized;
							Debug.Log(dir);
							Vector2 perp = Vector2.Perpendicular(dir);
							Debug.Log(perp);
							if (!Mathf.Approximately(perp.y, 0))
							{ // new edge is vertical
								if (dir.x > 0) // we're traveling right towards the mapCorner
									otherCorner.position.x = VoronoiGenerator.topRight.x; // so clamp to right border
								else // we're travelling left towards mapCorner
									otherCorner.position.x = VoronoiGenerator.topLeft.x; // so clamp to left border
							}
							else
							{ // new edge is horizontal
								if (dir.y > 0) // we're traveling up towards mapCorner
									otherCorner.position.y = VoronoiGenerator.topRight.y; // so clamp to top border
								else // travelling down towards mapCorner
									otherCorner.position.y = VoronoiGenerator.bottomRight.y;
							}

							otherCorner.isOnBorder = true;
						}

						lastCorner = otherCorner;

						sharedEdge = null;
						foreach (var edge in lastCorner.connectedEdges)
							if (edge.Contains(currentPolygon) && edge.GetPolygonCount() > 1)
								sharedEdge = edge;
						if (sharedEdge == null)
							Debug.Log("Oh shit");
					}
					else
					{
						// create new corner at intersection and connect to lastCorner
						Debug.Log("regular border polygon");

						// create new Corner at intersect point and bisect edge
						BisectEdge(otherEdge, intersectPoint, out Corner newCorner, out VEdge newOOBEdge);

						// connect first corner to new point
						lastCorner.TryGetEdgeWith(newCorner, out VEdge newBorderEdge);
						lastCorner = newCorner;
						last = newBorderEdge;
						sharedEdge = otherEdge;
					}
				}


				if (lastCorner == firstCorner)
					break; // we've come full circle

				found = false;
				foreach (var poly in sharedEdge.GetPolygons())
				{
					if (poly == currentPolygon || visited.Contains(poly))
						continue;
					found = true;
					currentPolygon = poly;
				}

				if (!found)
					Debug.LogWarning("border fixing ended early");
				// if found = false something probably has gone wrong
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
		/// <param name="intersectPoint">point on map border where new corner is created</param>
		/// <param name="newCorner"></param>
		/// <param name="newEdge">new, compeletely OOB edge</param>
		private void BisectEdge(VEdge edge, Vector2 intersectPoint, out Corner newCorner, out VEdge newEdge)
		{
			newCorner = new Corner(intersectPoint, cornerCount++);
			newCorner.isOnBorder = true;
			uniqueCorners.Add(newCorner);
			Corner oobCorner = edge.start.isOOB ? edge.start : edge.end;
			edge.ReplaceSite(oobCorner, newCorner);
			newCorner.TryGetEdgeWith(oobCorner, out newEdge);
		}

		/// <summary>
		/// Similar to other BisectEdge() but instead of creating a new corner at location
		/// takes an existing corner and creates two edges with corner in center.
		/// </summary>
		/// <param name="edge">becomes completely INBOUND edge</param>
		/// <param name="centerCorner"></param>
		/// <param name="newEdge">new, compeletely OOB edge</param>
		private void BisectEdge(VEdge edge, Corner centerCorner, out VEdge newEdge)
		{
			Corner oobCorner = edge.start.isOOB ? edge.start : edge.end;
			edge.ReplaceSite(oobCorner, centerCorner);
			centerCorner.TryGetEdgeWith(oobCorner, out newEdge);
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