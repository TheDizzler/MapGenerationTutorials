using System;
using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;
using static AtomosZ.Voronoi.VoronoiGenerator;

namespace AtomosZ.Voronoi
{
	public class VoronoiGraph
	{
		public enum LogType { Normal, Warning, Error, Exception };

		public static HashSet<Corner> uniqueCorners;
		public static Dictionary<byte, Corner> mapCorners;
		public static HashSet<VEdge> uniqueVEdges;
		public static int cornerCount = 0;
		public static List<Polygon> invalidatedPolygons;
		public static HashSet<VEdge> invalidatedEdges;
		public static Dictionary<MapSide, List<BoundaryCrossingEdge>> boundCrossingEdges;

		private static DelaunayGraph delaunayGraph;

		public List<Polygon> polygons;
		private List<Corner> removeCorners;
		private VoronoiGenerator generator;
		private int breakCount = 0;
		private string logFilePath = Application.dataPath + @"\Voronoi\ErrorLogs\";
		private List<string> logMsgs;


		public VoronoiGraph(VoronoiGenerator vGen, DelaunayGraph dGraph)
		{
			generator = vGen;
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

			if (generator.clampToMapBounds)
			{
				ClampToMapBounds();
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

		/// <summary>
		/// Double corner cutting case seed: 637421845385189985
		/// </summary>
		private void ClampToMapBounds()
		{
			logMsgs = new List<string>();

			logMsgs.Add("****** Error found on Seed: " + generator.randomSeed + "******");
			logMsgs.Add("Map dimensions: " + generator.mapWidth + ", " + generator.mapHeight);
			logMsgs.Add("Region amt: " + generator.regionAmount);
			logMsgs.Add("MinSqrDistBtwnSites: " + generator.minSqrDistBtwnSites);
			logMsgs.Add("MinDistBtwnSiteAndBorder: " + generator.minDistBtwnSiteAndBorder);
			logMsgs.Add("MinDistBtwnCornerAndBorder: " + generator.minDistBtwnCornerAndBorder);
			logMsgs.Add("MinEdgeLengthToMerge: " + generator.minEdgeLengthToMerge);
			logMsgs.Add("Merge near corners: " + generator.mergeNearCorners);
			logMsgs.Add("******************************************\n");

			boundCrossingEdges = GetBoundCrossingEdges();
			// Sort edges clockwise starting from top left
			boundCrossingEdges[MapSide.Top].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2) { return e1.intersectPosition.x < e2.intersectPosition.x ? -1 : 1; });
			boundCrossingEdges[MapSide.Right].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2) { return e1.intersectPosition.y > e2.intersectPosition.y ? -1 : 1; });
			boundCrossingEdges[MapSide.Bottom].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2) { return e1.intersectPosition.x > e2.intersectPosition.x ? -1 : 1; });
			boundCrossingEdges[MapSide.Left].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2) { return e1.intersectPosition.y < e2.intersectPosition.y ? -1 : 1; });

			Corner lastCreatedBorderCorner = null;
			try
			{
				foreach (MapSide mapSide in (MapSide[])Enum.GetValues(typeof(MapSide)))
				{
					if (mapSide == MapSide.Inside)
						continue;
					lastCreatedBorderCorner = BisectEdgesToBounds(mapSide, lastCreatedBorderCorner);
					if (lastCreatedBorderCorner == null)
					{
						throw new Exception("See above logs");
					}
				}

				Log("Fixing final corner");
				if (FixCorner(lastCreatedBorderCorner, MapSide.Top) == null)
					throw new Exception("See above logs");
			}
			catch (Exception ex)
			{
				System.IO.File.WriteAllLines(logFilePath + generator.randomSeed + ".txt", logMsgs);
				Debug.LogException(ex);
				generator.useRandomSeed = false; // make sure we stay on this seed until the problem has been rectified
				return;
			}
		}

		private Corner BisectEdgesToBounds(MapSide mapSide, Corner lastCreatedBorderCorner)
		{
			Corner lastCorner = FixCorner(lastCreatedBorderCorner, mapSide);
			if (lastCorner == null)
				return null;

			for (int i = 0; i < boundCrossingEdges[mapSide].Count - 1; ++i)
			{
				var currentEV = boundCrossingEdges[mapSide][i];
				// make sure this position isn't already a corner
				if (currentEV.isOnBorder)
				{
					Log("Found corner on border");
					if (!currentEV.edge.HasCornerOnBorder(out List<Corner> borderCorners))
					{
						Log("\tborder corner mis-reported?", LogType.Error);
						return null;
					}

					if (borderCorners.Count > 1)
					{
						Log("\tSPECIAL CASE: edge has more than one border corner.", LogType.Error);
						return null;
					}

					lastCorner.TryGetEdgeWith(borderCorners[0], out VEdge sharedEdge);
					lastCorner = borderCorners[0];
				}
				else
				{
					BisectEdge(currentEV.edge, currentEV.intersectPosition, out Corner newCorner, out VEdge newEdge);
					newCorner.TryGetEdgeWith(lastCorner, out VEdge sharedEdge);
					lastCorner = newCorner;
				}
			}

			return lastCorner;
		}

		private Corner FixCorner(Corner lastCreatedBorderCorner, MapSide mapSide)
		{
			MapSide lastMapSide = mapSide == MapSide.Top ? MapSide.Left : (MapSide)((int)mapSide * .5f);
			Corner lastCorner;
			Corner mapCorner = mapCorners[(byte)((byte)mapSide + (byte)lastMapSide)];

			// check if last edge of lastMapSide and first edge of currenMapSide share a corner
			BoundaryCrossingEdge firstEV = boundCrossingEdges[mapSide][0];
			VEdge firstEdge = firstEV.edge;
			BoundaryCrossingEdge lastEV = boundCrossingEdges[lastMapSide][boundCrossingEdges[lastMapSide].Count - 1];
			VEdge lastEdge = lastEV.edge;

			if (firstEdge == lastEdge)
			{
				if (lastCreatedBorderCorner == null)
				{
					Log("oh so we're starting with a corner cutter, eh?");
					var secondEV = boundCrossingEdges[mapSide][1];
					if (secondEV.isOnBorder)
					{
						Log("Found corner on border", LogType.Warning);
						return null;
					}

					BisectEdge(secondEV.edge, secondEV.intersectPosition, out Corner newCorner, out VEdge newEdge);
					mapCorner.TryGetEdgeWith(newCorner, out VEdge newEdge2);

					boundCrossingEdges[mapSide].Remove(firstEV);
					boundCrossingEdges[mapSide].Remove(secondEV);
					lastCorner = newCorner;
				}
				else
				{
					Log("Fixing a corner cutter");
					mapCorner.TryGetEdgeWith(lastCreatedBorderCorner, out VEdge noUseEdge);
					boundCrossingEdges[mapSide].Remove(firstEV);
					boundCrossingEdges[lastMapSide].Remove(lastEV);
					VoronoiHelper.RemoveEdge(firstEdge);
					lastCorner = mapCorner;
				}
			}
			else if (firstEdge.SharesCorner(lastEdge, out Corner sharedCorner))
			{
				Log("Simple MapCorner found");
				if (sharedCorner.isOnBorder)
				{
					Log("\tFound corner on border");
					//return null;
				}

				VoronoiHelper.MergeCorners(sharedCorner, mapCorner, sharedCorner.isOnBorder);

				if (lastCreatedBorderCorner == null)
				{
					Log("\tDid we just start?");
				}
				else
				{
					mapCorner.TryGetEdgeWith(lastCreatedBorderCorner, out VEdge sharedEdge);
				}

				// remove no longer intersection edges
				boundCrossingEdges[lastMapSide].Remove(lastEV);
				boundCrossingEdges[mapSide].Remove(firstEV);
				lastCorner = mapCorner;
			}
			else
			{
				Log("Complex MapCorner found");

				Corner lastInCorner = lastEdge.start.isOOB ? lastEdge.end : lastEdge.start;
				Corner currentInCorner = firstEdge.start.isOOB ? firstEdge.end : firstEdge.start;

				VEdge inSharedEdge = currentInCorner.FindSharedEdgeWith(lastInCorner);
				if (inSharedEdge != null)
				{
					Log("\tNot too complex");
					// try find last intersection created on last mapSide
					if (lastEdge.GetPolygonCount() > 1)
					{   // if this number is greater than 1, we have issues
						Log("\tIlogical polygon count on corner edge: " + lastEdge.GetPolygonCount(), LogType.Error);
						//debugEdges.Add(firstEdge);
						//debugEdges.Add(lastEdge);
						debugEdges.Add(inSharedEdge);
						return null;
					}

					if (lastEV.isOnBorder)
					{
						Log("\tlast corner before mapCorner is a border corner. Relevant?");
						//	// merge border corner with mapCorner
						//	VoronoiHelper.MergeCorners(lastInCorner, mapCorner);
						//	return null;
					}
					//else

					// We could (1) merge the two points together then merge the result with the mapCorner
					// or (2) bisect the edge, using the mapCorner as the center.
					//		(1) could make some really odd polygon shapes
					//		(2) could bisect an edge that is already really short, creating a horrific spike
					// Alternatively, we could do (3), either (1) or (2) depending on length of edge
					bool edgeShort = Vector2.Distance(inSharedEdge.start.position, inSharedEdge.end.position) < generator.minEdgeLengthToMerge;
					Log("Edge length: " + Vector2.Distance(inSharedEdge.start.position, inSharedEdge.end.position));
					if (edgeShort)
					{
						// implementation of (1)
						VoronoiHelper.MergeCorners(lastInCorner, currentInCorner, lastEV.isOnBorder);
						VoronoiHelper.MergeCorners(currentInCorner, mapCorner, lastEV.isOnBorder);
					}
					else
					{
						// implementation of (2)
						BisectEdge(inSharedEdge, mapCorner, out VEdge newEdge);
						VoronoiHelper.RemoveEdge(lastEdge);
						VoronoiHelper.RemoveEdge(firstEdge);
					}


					if (lastCreatedBorderCorner == null)
					{
						Log("\tDid we just start?");
					}
					else
					{
						mapCorner.TryGetEdgeWith(lastCreatedBorderCorner, out VEdge sharedEdge);
					}

					// remove no longer intersection edges
					boundCrossingEdges[lastMapSide].Remove(lastEV);
					boundCrossingEdges[mapSide].Remove(firstEV);
					lastCorner = mapCorner;
				}
				else
				{
					//if (!lastEV.isCornerCutter)
					//{
					//	Log("\tFuck damn this could be trouble.", LogType.Warning);
					//	VoronoiGenerator.debugEdges.Add(lastEdge);
					//	VoronoiGenerator.debugEdges.Add(firstEdge);
					//	return null;
					//}

					lastCreatedBorderCorner.TryGetEdgeWith(mapCorner, out VEdge newEdge);
					VoronoiHelper.RemoveEdge(lastEdge);
					lastCorner = mapCorner;
				}
			}

			return lastCorner;
		}

		private Dictionary<MapSide, List<BoundaryCrossingEdge>> GetBoundCrossingEdges()
		{
			List<Corner> borderCorners = new List<Corner>();

			// List semi-oob edges with the intersection point
			Dictionary<MapSide, List<BoundaryCrossingEdge>> crossingEdges
				= new Dictionary<MapSide, List<BoundaryCrossingEdge>>();

			crossingEdges[MapSide.Left] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Right] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Bottom] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Top] = new List<BoundaryCrossingEdge>();

			foreach (var edge in uniqueVEdges) // !!Need to get corners on border too!!
			{
				if (VoronoiGenerator.TryGetBoundsIntersection(edge.start.position, edge.end.position,
					out Dictionary<MapSide, Vector2> intersections, out byte cornerByte))
				{
					bool isCornerCutter = false;
					if (intersections.Count == 2)
					{
						isCornerCutter = true;
					}

					foreach (var kvp in intersections)
						crossingEdges[kvp.Key].Add(new BoundaryCrossingEdge(edge, kvp.Value, false, isCornerCutter));
				}
				else
				{ // check for on border end points
					if (edge.start.isMapCorner || edge.end.isMapCorner)
					{
						Log("Very rare case of edge end point is mapCorner. May be safe to ignore?", LogType.Warning);
						continue;
					}

					if (edge.HasCornerOnBorder(out List<Corner> corners))
					{
						if (corners.Count != 1)
						{
							Log("SPECIAL CASE: Edge found with two border corners. This is a special case that must be addressed",
								LogType.Exception);
						}

						Corner borderCorner = corners[0];
						if (borderCorners.Contains(borderCorner))
							continue; // this corner will be reported multiple times. Best make sure it only gets added once.
						borderCorners.Add(borderCorner);
						MapSide mapSide = GetOnBorderMapSide(borderCorner);
						if (mapSide == MapSide.Inside)
						{
							Log("Corner reporting onBorder but is not.", LogType.Exception);
						}

						crossingEdges[mapSide].Add(new BoundaryCrossingEdge(edge, borderCorner.position, true));
					}
				}
			}

			return crossingEdges;
		}

		private void Log(string msg, LogType logType = LogType.Normal)
		{
			logMsgs.Add(logType.ToString() + ": " + msg);
			switch (logType)
			{
				case LogType.Normal:
					Debug.Log(msg);
					break;
				case LogType.Warning:
					Debug.LogWarning(msg);
					break;
				case LogType.Error:
					Debug.LogError(msg);
					break;
				case LogType.Exception:
					throw new Exception(msg);
			}
		}

		public class BoundaryCrossingEdge
		{
			public VEdge edge;
			public Vector2 intersectPosition;
			public bool isOnBorder;
			public bool isCornerCutter;

			public BoundaryCrossingEdge(VEdge edge, Vector2 intersect, bool isOnBorder = false, bool isCornerCutter = false)
			{
				this.edge = edge;
				intersectPosition = intersect;
				this.isOnBorder = isOnBorder;
				this.isCornerCutter = isCornerCutter;
			}
		}


		private bool DebugBreak(int countBeforeBreak = 2)
		{
			if (++breakCount >= countBeforeBreak)
			{
				Log("Manual Break initiated", LogType.Warning);
				return true;
			}

			return false;
		}

		[System.Obsolete]
		private Corner GetClosestMapCorner(Corner corner)
		{
			// get closest map corner
			float closestSqrDist = float.MaxValue;
			Corner mapCorner = null;
			foreach (var kvp in mapCorners)
			{
				float dist = (corner.position - kvp.Value.position).sqrMagnitude;
				if (dist < closestSqrDist)
				{
					closestSqrDist = dist;
					mapCorner = kvp.Value;
				}
			}

			return mapCorner;
		}

		[System.Obsolete]
		private Corner GetClosestMapCorner(Polygon currentPolygon)
		{
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

			return mapCorner;
		}

		[System.Obsolete]
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