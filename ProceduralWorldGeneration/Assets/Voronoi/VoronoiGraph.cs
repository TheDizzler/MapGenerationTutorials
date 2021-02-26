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

		/// <summary>
		/// Some random seeds produce invalid polygon that uses map corners. Ex:
		/// 637427950365396994
		/// 637427958541256155
		/// </summary>
		/// <param name="vGen"></param>
		/// <param name="dGraph"></param>
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

			invalidatedPolygons = new List<Polygon>();
			invalidatedEdges = new HashSet<VEdge>();

			polygons = new List<Polygon>();
			for (int i = 0; i < dGraph.centroids.Count; ++i) // first 4 centroids are map corners
			{
				Polygon poly = new Polygon(dGraph.centroids[i]);
				polygons.Add(poly);
			}

			if (polygons[0].corners == null || polygons[1].corners == null
				|| polygons[2].corners == null || polygons[3].corners == null)
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
				logMsgs.Add("Corner polygon did not make it passed initial generation");
				System.IO.File.WriteAllLines(logFilePath + "BorderCornerDeletedIssue_" + generator.randomSeed + ".txt", logMsgs);
				Log("Corner polygon did not make it passed initial generation", LogType.Exception, false);
			}
			VoronoiHelper.Associate(polygons[0], mapCorners[TopLeftCornerByte]);
			VoronoiHelper.Associate(polygons[1], mapCorners[TopRightCornerByte]);
			VoronoiHelper.Associate(polygons[2], mapCorners[BottomRightCornerByte]);
			VoronoiHelper.Associate(polygons[3], mapCorners[BottomLeftCornerByte]);

			if (VoronoiGenerator.MergeNearCorners)
				MergeNearCorners();

			if (generator.clampToMapBounds)
			{
				if (!ClampToMapBounds())
					return;

				foreach (Corner corner in uniqueCorners)
				{
					if (corner.isInvalidated)
					{
						if (!removeCorners.Contains(corner))
							removeCorners.Add(corner);
						continue;
					}

					if (corner.isOOB)
					{
						removeCorners.Add(corner);
					}
				}
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
						poly.Remove(edge);
					}
				}

				removingCorner.connectedEdges.Clear();
				RemoveCorner(removingCorner);
			}


			int culled = 0;
			foreach (var polygon in invalidatedPolygons)
			{
				debugPolygons.Add(polygon);
				polygons.Remove(polygon);
				++culled;
			}


			foreach (Polygon polygon in polygons)
			{
				polygon.CenterCentroid();
				polygon.SortEdges();
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
					MergeCorners(closeCorner, corner);
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
		/// Returns false if exception thrown.
		/// Double corner cutting case seed: 637421845385189985
		/// </summary>
		/// <returns></returns>
		private bool ClampToMapBounds()
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

			try
			{
				boundCrossingEdges = GetBoundCrossingEdges();
				// Sort edges clockwise starting from top left
				boundCrossingEdges[MapSide.Top].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2)
				{ return e1.intersectPosition.x < e2.intersectPosition.x ? -1 : 1; });
				boundCrossingEdges[MapSide.Right].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2)
				{ return e1.intersectPosition.y > e2.intersectPosition.y ? -1 : 1; });
				boundCrossingEdges[MapSide.Bottom].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2)
				{ return e1.intersectPosition.x > e2.intersectPosition.x ? -1 : 1; });
				boundCrossingEdges[MapSide.Left].Sort(delegate (BoundaryCrossingEdge e1, BoundaryCrossingEdge e2)
				{ return e1.intersectPosition.y < e2.intersectPosition.y ? -1 : 1; });

				if (generator.debugBorders)
				{
					if (generator.bottomBorder)
						Clamp(MapSide.Bottom);
					if (generator.leftBorder)
						Clamp(MapSide.Left);
					if (generator.topBorder)
						Clamp(MapSide.Top);
					if (generator.rightBorder)
						Clamp(MapSide.Right);

					return false;
				}
				else
				{
					Clamp(MapSide.Bottom);
					Clamp(MapSide.Left);
					Clamp(MapSide.Top);
					Clamp(MapSide.Right);
				}
			}
			catch (Exception ex)
			{
				System.IO.File.WriteAllLines(logFilePath + "BoundsClampIssue_" + generator.randomSeed + ".txt", logMsgs);
				Debug.LogException(ex);
				generator.newRandomSeed = false; // make sure we stay on this seed until the problem has been rectified
				generator.createRegions = false;
				return false;
			}

			return true;
		}

		private void Clamp(MapSide mapSide)
		{
			MapSide previousMapSide = mapSide == MapSide.Top ? MapSide.Left : (MapSide)((int)mapSide * .5f);
			MapSide nextMapSide = mapSide == MapSide.Left ? MapSide.Top : (MapSide)((int)mapSide * 2);
			Corner firstMapCorner = mapCorners[(byte)((byte)mapSide + (byte)previousMapSide)];
			Corner lastMapCorner = mapCorners[(byte)((byte)mapSide + (byte)nextMapSide)];
			Polygon firstCornerPolygon = firstMapCorner.polygons[0];
			;
			Polygon lastCornerPolygon = lastMapCorner.polygons[0];


			Corner currentCorner = firstMapCorner;
			Polygon currentPolygon = firstCornerPolygon;

			for (int i = 0; i < boundCrossingEdges[mapSide].Count; ++i)
			{
				BoundaryCrossingEdge currentBCE = boundCrossingEdges[mapSide][i];

				if (!currentBCE.isOnBorder)
				{
					BisectEdge(currentBCE.edge, currentBCE.intersectPosition, out Corner newCorner, out VEdge oobEDge);
					currentBCE.isOnBorder = true;
					currentBCE.borderCorner = newCorner;
					CreateBorderEdge(currentCorner, newCorner, currentPolygon);

					currentCorner = newCorner;
				}
				else
				{
					if (currentBCE.borderCorner == null)
						Log("IsOnBorder but no borderCorner", LogType.Exception);
					CreateBorderEdge(currentCorner, currentBCE.borderCorner, currentPolygon);

					currentCorner = currentBCE.borderCorner;
				}

				if (i == boundCrossingEdges[mapSide].Count - 1)
				{
					currentPolygon = lastCornerPolygon;
				}
				else
				{
					var nextBCE = boundCrossingEdges[mapSide][i + 1];
					List<Polygon> sharedPolygons = currentBCE.edge.GetSharedPolygons(nextBCE.edge);
					if (sharedPolygons.Count != 1)
					{
						Log("Unusual shared polygon count: " + sharedPolygons.Count
							+ "\nedge: " + currentBCE.edge.id + " Next edge: " + nextBCE.edge.id, LogType.Exception);
					}

					currentPolygon = sharedPolygons[0];
				}
			}

			CreateBorderEdge(currentCorner, lastMapCorner, currentPolygon);
		}


		private void CreateBorderEdge(Corner corner1, Corner corner2, Polygon polygon)
		{
			corner1.TryGetEdgeWith(corner2, out VEdge sharedEdge);
			VoronoiHelper.Associate(polygon, sharedEdge);
		}

		private void CreateBorderEdge(Corner corner1, Corner corner2)
		{
			List<Polygon> sharedPolygons = corner1.GetSharedPolygons(corner2);
			if (sharedPolygons.Count != 1)
			{
				debugCorners.Add(corner1);
				debugCorners.Add(corner2);
				foreach (var poly in sharedPolygons)
					debugPolygons.Add(poly);
				Log("Unusal shared polygon count between boundary corners. Count: " + sharedPolygons.Count
					+ "\nHopefully this is rare enough that we can just restart the map building process.", LogType.Exception);
			}

			CreateBorderEdge(corner1, corner2, sharedPolygons[0]);
		}


		private Dictionary<MapSide, List<BoundaryCrossingEdge>> GetBoundCrossingEdges()
		{
			List<Corner> foundBorderCorners = new List<Corner>();

			// List semi-oob edges with the intersection point
			Dictionary<MapSide, List<BoundaryCrossingEdge>> crossingEdges
				= new Dictionary<MapSide, List<BoundaryCrossingEdge>>();

			crossingEdges[MapSide.Left] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Right] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Bottom] = new List<BoundaryCrossingEdge>();
			crossingEdges[MapSide.Top] = new List<BoundaryCrossingEdge>();

			foreach (var edge in uniqueVEdges) // !!Need to get corners on border too!!
			{
				if (TryGetBoundsIntersections(edge, out Dictionary<MapSide, List<Vector2>> intersections, out List<Corner> borderCorners))
				{
					bool isCornerCutter = false;
					if (intersections.Count == 2)
					{
						isCornerCutter = true;
					}

					if (borderCorners.Count != 0)
					{
						if (isCornerCutter && TryGetCornerOOBofSameSideAs(borderCorners[0].position, edge, out Corner sameSideCorner, out MapSide mapSide))
						{ // skip this edge. It may give us invalid polygon results later.
							Log("Ignoring edge: " + edge.id + " corner: " + sameSideCorner.num);
							debugEdges.Add(edge);
							intersections.Remove(mapSide);
						}
						else
						{
							foreach (var corner in borderCorners)
							{
								MapSide side = GetOnBorderMapSide(corner);
								intersections.Remove(side);
								// if not corner cutter, Remove(side) will remove both intersections....which is maybe what we want?

								if (!foundBorderCorners.Contains(corner))
								{
									foundBorderCorners.Add(corner);
									crossingEdges[side].Add(new BoundaryCrossingEdge(edge, corner.position, true, isCornerCutter, corner));
								}
							}
						}
					}


					foreach (var kvp in intersections)
					{
						crossingEdges[kvp.Key].Add(new BoundaryCrossingEdge(edge, kvp.Value[0], false, isCornerCutter));
					}
				}
			}

			return crossingEdges;
		}

		public bool TryGetBoundsIntersections(VEdge edge, out Dictionary<MapSide, List<Vector2>> intersections, out List<Corner> borderCorners)
		{
			intersections = new Dictionary<MapSide, List<Vector2>>();

			if (edge.HasCornerOnBorder(out borderCorners))
			{
				MapSide mapSide;
				if (borderCorners.Count != 1)
				{
					Log("Ignoring edge with two on border corners: " + edge.id);
					return false;
				}

				if (!edge.GetOppositeSite(borderCorners[0]).isOOB)
				{
					Log("Ignoring partially inbounds edge: " + edge.id);
					return false;
				}

				mapSide = GetOnBorderMapSide(borderCorners[0]);
				intersections[mapSide] = new List<Vector2>();
				intersections[mapSide].Add(borderCorners[0].position);
			}

			foreach (MapSide mapSide in (MapSide[])Enum.GetValues(typeof(MapSide)))
			{
				if (mapSide == MapSide.Inside)
					continue;

				if (TryGetLineIntersection(borderEndPoints[mapSide].Item1, borderEndPoints[mapSide].Item2, edge.start.position, edge.end.position, out Vector2 intersection))
				{
					if (intersections.ContainsKey(mapSide)/* || intersections[mapSide] != null*/)
					{
						debugEdges.Add(edge);
						Log("This is an anomaly and should not happen", LogType.Exception);
					}
					else
						intersections.Add(mapSide, new List<Vector2>());
					intersections[mapSide].Add(intersection);
				}
			}

			return intersections.Count > 0;
		}

		private void MergeCorners(Corner deprecatedCorner, Corner mergedCorner, bool ignoreBorderCornerRules = false)
		{
			if (!ignoreBorderCornerRules)
			{
				if (!(deprecatedCorner.isOnBorder ^ mergedCorner.isOnBorder)) // if they are both on the border or both off
				{
					mergedCorner.position = (deprecatedCorner.position + mergedCorner.position) / 2; // get midpoint
					mergedCorner.isOnBorder = deprecatedCorner.isOnBorder;
				}
				else if (deprecatedCorner.isOnBorder) // if only old corner is on the border use its position
				{
					mergedCorner.position = deprecatedCorner.position;
					mergedCorner.isOnBorder = true;
				}
				// otherwise mergedCorner is on the border so leave as it is
			}

			for (int i = deprecatedCorner.connectedEdges.Count - 1; i >= 0; --i)
				deprecatedCorner.connectedEdges[i].ReplaceSite(deprecatedCorner, mergedCorner);

			deprecatedCorner.isInvalidated = true;

			removeCorners.Add(deprecatedCorner);
		}

		private void RemoveEdge(VEdge edge)
		{
			Corner p1 = edge.start;
			Corner p2 = edge.end;
			var polygons = edge.GetPolygons();
			foreach (var polygon in polygons)
			{
				polygon.Remove(edge);
			}

			RemoveEdgeFrom(p1, edge);
			RemoveEdgeFrom(p2, edge);

			uniqueVEdges.Remove(edge);
		}

		private void RemoveEdgeFrom(Corner corner, VEdge edge)
		{
			corner.connectedEdges.Remove(edge);
			if (corner.connectedEdges.Count == 1)
			{
				if (corner.isOOB && corner.connectedEdges[0].GetOppositeSite(corner).isOOB)
				{
					Log("Found a dangling corner and edge that are safe to remove");
					RemoveEdge(corner.connectedEdges[0]);
				}
				else
				{
					debugEdges.Add(edge);
					Log("Found a dangling corner", LogType.Exception);
				}
			}

			if (corner.connectedEdges.Count == 0)
			{
				corner.isInvalidated = true;
				removeCorners.Add(corner);
			}
		}


		/// <summary>
		/// <para>edge: becomes completely INBOUND edge.</para>
		/// <para>intersectPoint: where new corner is created.</para>
		/// <para>newEdge: new, compeletely OOB edge. Does NOT get added to shared polygons.</para>
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
			Corner oobCorner = null;
			if (edge.start.isOOB && edge.start.isOOB)
			{// this is a corner cutter - preserve edge that goes off the opposite border
			 // get "locked" x/y coordinate and mapSide
			 // find which edge endpoint is beyond that coordinate
				if (TryGetCornerOOBofSameSideAs(intersectPoint, edge, out Corner sameSideCorner, out MapSide mapSide))
					oobCorner = sameSideCorner;
				else
					Log("ohno", LogType.Exception);
			}
			else if (edge.start.isOOB)
			{
				oobCorner = edge.start;
			}
			else
			{
				oobCorner = edge.end;
			}

			edge.ReplaceSite(oobCorner, newCorner);
			newCorner.TryGetEdgeWith(oobCorner, out newEdge);
		}

		private void RemoveCorner(Corner corner)
		{
			foreach (var polygon in corner.polygons)
			{
				polygon.corners.Remove(corner);
				polygon.oobCorners.Remove(corner);
				if (polygon.corners.Count < 3)
				{
					polygon.Invalidate();
				}
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


		private void DebugBreak(int countBeforeBreak = 2)
		{
			if (++breakCount >= countBeforeBreak)
			{
				Log("Manual Break initiated", LogType.Exception);
			}
		}

		/// <summary>
		/// SilentLogging prevents normal and warnings from printing to console.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="logType"></param>
		/// <param name="silentLogging">If this msg should be saved to log file but don't need
		/// to spam the console, keep as true.</param>
		private void Log(string msg, LogType logType = LogType.Normal, bool silentLogging = true)
		{
			logMsgs.Add(logType.ToString() + ": " + msg);
			switch (logType)
			{
				case LogType.Normal:
					if (!silentLogging)
						Debug.Log(msg);
					break;
				case LogType.Warning:
					if (!silentLogging)
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
			public Corner borderCorner;

			public BoundaryCrossingEdge(VEdge edge, Vector2 intersect, bool isOnBorder = false, bool isCornerCutter = false, Corner borderCorner = null)
			{
				this.edge = edge;
				intersectPosition = intersect;
				this.isOnBorder = isOnBorder;
				this.isCornerCutter = isCornerCutter;
				this.borderCorner = borderCorner;
			}
		}
	}
}