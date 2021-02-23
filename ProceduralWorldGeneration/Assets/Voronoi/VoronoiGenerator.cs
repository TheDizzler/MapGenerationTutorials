using System;
using System.Collections.Generic;
using AtomosZ.Voronoi.Regions;
using UnityEngine;
using Random = System.Random;


namespace AtomosZ.Voronoi
{
	public class VoronoiGenerator : MonoBehaviour
	{
		public const byte TopRightCornerByte = 3;
		public const byte BottomRightCornerByte = 6;
		public const byte TopLeftCornerByte = 9;
		public const byte BottomLeftCornerByte = 12;

		public enum MapSide
		{
			Inside = 0, // 0000
			Top = 1,    // 0001
			Right = 2,  // 0010
			Bottom = 4, // 0100
			Left = 8    // 1000
		};

		public static VoronoiGenerator instance;
		public Random rng;
		public static float minSqrDistBetweenCorners;
		public static float minDistCornerAndBorder;
		public static bool MergeNearCorners;

		public static Dictionary<MapSide, Tuple<Vector2, Vector2>> borderEndPoints;
		public static Vector2 topLeft;
		public static Vector2 topRight;
		public static Vector2 bottomRight;
		public static Vector2 bottomLeft;

		public static List<VEdge> debugEdges;
		public static List<Corner> debugCorners;
		public static List<Polygon> debugPolygons;


		private static Rect mapBounds;
		private static float mapBoundsTolerance = .00001f;

		public bool newRandomSeed = true;
		public string randomSeed = "Seed";

		[Range(1, 1024)]
		public int mapWidth = 512;
		[Range(1, 1024)]
		public int mapHeight = 512;
		[Range(0, 256)]
		public int regionAmount = 50;
		[Range(.0f, 15.00f)]
		public float minSqrDistBtwnSites = .5f;
		[Range(.0f, 5.00f)]
		public float minDistBtwnSiteAndBorder = .5f;
		[Range(.05f, 5f)]
		public float minDistBtwnCornerAndBorder = .25f;
		[Range(0f, 1f)]
		public float minEdgeLengthToMerge = .05f;

		public bool mapDebugFoldout;
		public bool viewDelaunayCircles = false;
		public bool viewDelaunayTriangles = true;
		public bool viewCenteroids = true;
		public bool viewVoronoiPolygons = true;
		public bool viewCorners = false;
		public bool viewCornerIDs = false;
		public bool viewEdgeIDs = false;
		public bool viewIntersections = false;
		public bool viewIntersectionIDs = false;
		public bool viewIntersectionDirections = false;
		public bool debugBorders = false;
		public bool topBorder = true;
		public bool rightBorder = true;
		public bool bottomBorder = true;
		public bool leftBorder = true;

		public DelaunayGraph dGraph;
		public VoronoiGraph vGraph;
		public GameObject regionPrefab;
		public Transform regionHolder;
		public List<Region> regions;

		/// <summary>
		/// This causes issues that I'd rather not deal with.
		/// </summary>
		public bool mergeNearCorners = false;
		public bool clampToMapBounds = true;
		public bool createRegions = true;

		[Range(2, 256)]
		public int resolution = 10;
		public VoronoiNoiseSettings noiseSettings;
		public VoronoiHeightMapSettings heightMapSettings;

		public bool bordersEnabled = true;
		[Range(0, 7)]
		public int subdivisions = 1;
		[Range(0, 1)]
		public float amplitude;

		public VoronoiTextureData textureData;
		public Material regionMaterial;
		public MeshRenderer noisePreviewMeshRenderer;

		// Noisy edge test variables
		public bool debugNoisyLine;
		public Vector3 startNoisy = Vector3.zero;
		public Vector3 endNoisy = new Vector3(10, 0);
		public Vector3 startControl = new Vector3(5, 5);
		public Vector3 endControl = new Vector3(5, -5);
		[HideInInspector]
		public bool wasReset;

		[HideInInspector]
		public LineRenderer lr;

		private HeightMap heightMap;
		public bool borderSettingsFoldout;


		void Start()
		{
			GenerateMap();
		}

		public void OnShapeSettingsUpdated()
		{
			GenerateMap();
		}

		public void OnColorSettingsUpdated()
		{
			GenerateMap();
		}

		public void ToggleBorders(bool borders)
		{
			bordersEnabled = borders;
			if (regions != null)
			{
				foreach (var region in regions)
					region.ToggleBorder(bordersEnabled);
			}
		}

		public void ClearMap()
		{
			VEdge.count = 0;
			Polygon.count = 0;
			Region.count = 0;
			debugEdges = new List<VEdge>();
			debugCorners = new List<Corner>();
			debugPolygons = new List<Polygon>();
			if (regions != null)
				foreach (var region in regions)
					DestroyImmediate(region.gameObject);

			regions = null;
			dGraph = null;
			vGraph = null;
		}

		private void CreateRNG()
		{
			if (newRandomSeed)
				randomSeed = System.DateTime.Now.Ticks.ToString();
			instance = this;
			rng = new Random(randomSeed.GetHashCode());
		}

		public void GenerateMap()
		{
			instance = this;
			DestroyImmediate(lr);
			ClearMap();

			CreateRNG();

			MergeNearCorners = mergeNearCorners;

			minSqrDistBetweenCorners = minSqrDistBtwnSites;
			minDistCornerAndBorder = minDistBtwnCornerAndBorder;



			mapBounds = new Rect(0, 0, mapWidth, mapHeight);

			topLeft = new Vector2(mapBounds.xMin, mapBounds.yMax);
			topRight = new Vector2(mapBounds.xMax, mapBounds.yMax);
			bottomRight = new Vector2(mapBounds.xMax, mapBounds.yMin);
			bottomLeft = new Vector2(mapBounds.xMin, mapBounds.yMin);

			borderEndPoints = new Dictionary<MapSide, Tuple<Vector2, Vector2>>();
			borderEndPoints.Add(MapSide.Top, new Tuple<Vector2, Vector2>(topLeft, topRight));
			borderEndPoints.Add(MapSide.Right, new Tuple<Vector2, Vector2>(topRight, bottomRight));
			borderEndPoints.Add(MapSide.Bottom, new Tuple<Vector2, Vector2>(bottomRight, bottomLeft));
			borderEndPoints.Add(MapSide.Left, new Tuple<Vector2, Vector2>(bottomLeft, topLeft));

			List<Vector2> sites = new List<Vector2>();
			// create corner sites

			sites.Add(topLeft);
			sites.Add(topRight);
			sites.Add(bottomRight);
			sites.Add(bottomLeft);

			int retryAttempts = 0;
			for (int i = 0; i < regionAmount; i++)
			{
				Vector2 site = new Vector2(
					(float)(minDistBtwnSiteAndBorder + rng.NextDouble() * (mapHeight - minDistBtwnSiteAndBorder * 2)),
					(float)(minDistBtwnSiteAndBorder + rng.NextDouble() * (mapWidth - minDistBtwnSiteAndBorder * 2)));
				if (IsTooNear(sites, site))
				{   // try again

					++retryAttempts;
					if (retryAttempts > 25)
					{
						Debug.Log("Unable to place new site within exceptable distance parameters."
							+ "\nAborting creating new sites. Created " + i + " out of " + regionAmount);
						break;
					}

					--i;
				}
				else
					sites.Add(site);
			}

			try
			{
				dGraph = new DelaunayGraph(sites);
				vGraph = new VoronoiGraph(this, dGraph);
				CreateRegions();
			}
			catch (System.Exception ex)
			{
				newRandomSeed = false; // make sure we stay on this seed until the problem has been rectified
				Debug.LogException(ex);
			}
		}


		public static float GetNewT(float midT)
		{
			float rnd = (float)instance.rng.NextDouble();
			return Mathf.Lerp(midT, rnd, instance.amplitude);
		}

		public LineRenderer GenerateNoisyLineDebug()
		{
			ClearMap();
			CreateRNG();
			wasReset = true;
			if (lr == null)
				lr = gameObject.AddComponent<LineRenderer>();
			lr.startColor = Color.black;
			lr.endColor = Color.black;
			lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
			lr.widthMultiplier = .12f;
			lr.numCapVertices = 20;
			lr.positionCount = (int)Mathf.Pow(2, subdivisions) + 1;

			return lr;
		}


		/// <summary>
		/// NOT TESTED!
		/// </summary>
		public void AddPoint()
		{
			if (dGraph == null)
			{
				Debug.LogWarning("No graph to add points to");
				return;
			}

			Vector2 site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			while (IsTooNear(dGraph.centroids, site))
			{
				site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			}

			dGraph.AddSite(site);
			Debug.Log(dGraph.triangles.Count + " triangles");
		}


		public static bool IsInMapBounds(Vector2 position)
		{
			if (mapBounds.Contains(position))
				return true; // no argument that it is within bounds

			// check if close enough
			if (position.x + mapBoundsTolerance > mapBounds.xMin
				&& position.y + mapBoundsTolerance > mapBounds.yMin
				&& position.x - mapBoundsTolerance < mapBounds.xMax
				&& position.y - mapBoundsTolerance < mapBounds.yMax)
				return true;

			return false;
		}

		/// <summary>
		/// Finds corner case where a polygon is in a corner but doesn't contain that corner.
		/// INCOMPLETE.
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="cuttingCorners"></param>
		/// <param name="cornerCut"></param>
		/// <returns></returns>
		[System.Obsolete]
		public static bool TryGetCornerIntersections(Polygon polygon, out VEdge cornerCuttingEdge, out byte cornerCut)
		{
			cornerCuttingEdge = null;
			cornerCut = 0;

			foreach (var corner in polygon.corners)
			{
				// if a connected edge thats in this polygon intersects border and a connected edge NOT in this polygon intersectes a DIFFERENT border
				if (IsInMapBounds(corner.position))
					continue; // ignore corners oob

				List<VEdge> encasingEdges = new List<VEdge>();
				List<MapSide> mapSidesCrossed = new List<MapSide>();
				foreach (var edge in corner.connectedEdges)
				{
					if (!TryGetMapSideCross(edge.start.position, edge.end.position, out MapSide side))
						continue; // edge does not cross map border

					encasingEdges.Add(edge);
					mapSidesCrossed.Add(side);
				}

				if (encasingEdges.Count >= 2)
				{
					MapSide side = mapSidesCrossed[0];
					bool allSameSide = true;
					for (int i = 1; i < mapSidesCrossed.Count; ++i)
						if (mapSidesCrossed[i] != side)
							allSameSide = false;
					if (allSameSide)
						continue; // uninteresting corner

					// at this point we should have identified a corner that has atleast two edges that encase a map corner
					// determine which edges are the corner enclosers
					// by neccesity, corner encasing edges are only connected to one polygon
					// which is not helpful if checking in the polygon creation phase
					Debug.Log("Found corner!");
				}
			}


			return false;
		}

		[System.Obsolete]
		public static Corner GetClosestCornerToMapCorner(Polygon currentPolygon, out Corner mapCorner)
		{
			// find closest mapCorner to polygon. Realistically, if all corners don't share the same 
			// closest mapCorner as the centroid of the polygon then the map should be too small to be valid/useful.
			float closestSqrDist = float.MaxValue;
			mapCorner = null;
			foreach (var kvp in VoronoiGraph.mapCorners)
			{
				float dist = (currentPolygon.centroid.position - kvp.Value.position).sqrMagnitude;
				if (dist < closestSqrDist)
				{
					closestSqrDist = dist;
					mapCorner = kvp.Value;
				}
			}

			Corner closestCorner = null;
			closestSqrDist = float.MaxValue;
			foreach (var corner in currentPolygon.corners)
			{
				float dist = (corner.position - mapCorner.position).sqrMagnitude;
				if (dist < closestSqrDist)
				{
					closestSqrDist = dist;
					closestCorner = corner;
				}
			}

			return closestCorner;
		}

		[System.Obsolete]
		public static bool IsOnBorder(Corner corner)
		{
			return Mathf.Approximately(corner.position.x, mapBounds.xMin)
				|| Mathf.Approximately(corner.position.x, mapBounds.xMax)
				|| Mathf.Approximately(corner.position.y, mapBounds.yMin)
				|| Mathf.Approximately(corner.position.y, mapBounds.yMax);
		}

		/// <summary>
		/// Returns MapSide.Inside if corner is not on border.
		/// </summary>
		/// <param name="corner"></param>
		/// <returns></returns>
		public static MapSide GetOnBorderMapSide(Corner corner)
		{
			return GetOnBorderMapSide(corner.position);
		}

		/// <summary>
		/// Returns MapSide.Inside if corner is not on border.
		/// </summary>
		/// <param name="borderCoord"></param>
		/// <returns></returns>
		public static MapSide GetOnBorderMapSide(Vector2 borderCoord)
		{
			if (Mathf.Approximately(borderCoord.x, mapBounds.xMin))
				return MapSide.Left;
			if (Mathf.Approximately(borderCoord.x, mapBounds.xMax))
				return MapSide.Right;
			if (Mathf.Approximately(borderCoord.y, mapBounds.yMin))
				return MapSide.Bottom;
			if (Mathf.Approximately(borderCoord.y, mapBounds.yMax))
				return MapSide.Top;
			return MapSide.Inside;
		}

		public static bool TryGetCornerOOBofSameSideAs(Vector2 borderCoord, VEdge edge, out Corner sameSideCorner, out MapSide mapSide)
		{
			mapSide = GetOnBorderMapSide(borderCoord);
			float lockedCoord = 0;
			switch (mapSide)
			{
				case MapSide.Left:

					lockedCoord = topLeft.x;
					if (edge.start.position.y > lockedCoord)
						sameSideCorner = edge.start;
					else
						sameSideCorner = edge.end;
					return true;
				case MapSide.Right:
					lockedCoord = topRight.x;
					if (edge.start.position.x > lockedCoord)
						sameSideCorner = edge.start;
					else
						sameSideCorner = edge.end;
					return true;
				case MapSide.Bottom:
					lockedCoord = bottomRight.y;
					if (edge.start.position.y < lockedCoord)
						sameSideCorner = edge.start;
					else
						sameSideCorner = edge.end;
					return true;
				case MapSide.Top:
					lockedCoord = topLeft.y;
					if (edge.start.position.x < lockedCoord)
						sameSideCorner = edge.start;
					else
						sameSideCorner = edge.end;
					return true;
			}

			sameSideCorner = null;
			return false;
		}

		public static bool GetCoordinateOfMapSide(Vector2 coordinateOnBorder, out MapSide mapSide, out float lockedCoord)
		{
			if (Mathf.Approximately(coordinateOnBorder.x, mapBounds.xMin))
			{
				mapSide = MapSide.Left;
				lockedCoord = topLeft.x;
				return true;
			}

			if (Mathf.Approximately(coordinateOnBorder.x, mapBounds.xMax))
			{
				mapSide = MapSide.Right;
				lockedCoord = topRight.x;
				return true;
			}

			if (Mathf.Approximately(coordinateOnBorder.y, mapBounds.yMin))
			{
				mapSide = MapSide.Bottom;
				lockedCoord = bottomRight.y;
				return true;
			}

			if (Mathf.Approximately(coordinateOnBorder.y, mapBounds.yMax))
			{
				mapSide = MapSide.Top;
				lockedCoord = topLeft.y;
				return true;
			}

			mapSide = MapSide.Inside;
			lockedCoord = 0;
			return false;
		}

		/// <summary>
		/// Gets closest map corner to edge IF it crosses a map border.
		/// If not, returns false.
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="cornerByte"></param>
		/// <returns></returns>
		public static bool TryGetClosestCorner(VEdge edge, out byte cornerByte)
		{
			Vector2 lineStart = edge.start.position;
			Vector2 lineEnd = edge.end.position;
			cornerByte = TopRightCornerByte;

			if (!TryGetFirstMapBoundsIntersection(lineStart, lineEnd, out Vector2 intersectPoint))
			{
				return false;
			}

			float minDistance = Vector2.Distance(intersectPoint, topRight);

			float toTopLeft = Vector2.Distance(intersectPoint, topLeft);
			if (toTopLeft < minDistance)
			{
				minDistance = toTopLeft;
				cornerByte = TopLeftCornerByte;
			}
			float toBottomLeft = Vector2.Distance(intersectPoint, bottomLeft);
			if (toBottomLeft < minDistance)
			{
				minDistance = toBottomLeft;
				cornerByte = BottomLeftCornerByte;
			}
			float toBottomRight = Vector2.Distance(intersectPoint, bottomRight);
			if (toBottomRight < minDistance)
			{
				minDistance = toBottomRight;
				cornerByte = BottomRightCornerByte;
			}

			return true;
		}

		public static bool TryGetMapSideCross(Vector2 lineStart, Vector2 lineEnd, out MapSide mapSide)
		{
			mapSide = MapSide.Inside;
			if (LineIntersectsMapSide(lineStart, lineEnd, MapSide.Top))
			{
				mapSide = MapSide.Top;
				return true;
			}

			if (LineIntersectsMapSide(lineStart, lineEnd, MapSide.Right))
			{
				mapSide = MapSide.Right;
				return true;
			}
			if (LineIntersectsMapSide(lineStart, lineEnd, MapSide.Left))
			{
				mapSide = MapSide.Left;
				return true;
			}
			if (LineIntersectsMapSide(lineStart, lineEnd, MapSide.Bottom))
			{
				mapSide = MapSide.Bottom;
				return true;
			}

			return false;
		}

		public static bool LineIntersectsMapSide(Vector2 lineStart, Vector2 lineEnd, MapSide mapSide)
		{
			switch (mapSide)
			{
				case MapSide.Top:
					return TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out Vector2 top);
				case MapSide.Right:
					return TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out Vector2 right);
				case MapSide.Bottom:
					return TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out Vector2 bottom);
				case MapSide.Left:
					return TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out Vector2 left);
			}

			return false;
		}

		public static bool TryGetBoundsIntersection(Polygon polygon, out Dictionary<VEdge, Vector2> intersections)
		{
			intersections = new Dictionary<VEdge, Vector2>();
			foreach (var edge in polygon.GetVoronoiEdges())
			{
				if (TryGetFirstMapBoundsIntersection(edge.start.position, edge.end.position, out Vector2 intersectPoint))
				{
					intersections.Add(edge, intersectPoint);
				}
			}

			return intersections.Count > 0;
		}

		/// <summary>
		/// If any exist, returns a list of edge-bool pairs of oob edges and 
		/// whether it's completely oob (true) or partially oob (false).
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static bool TryGetOOBEdges(Polygon polygon, out List<VEdge> completelyOOB, out List<VEdge> partialOOB)
		{
			completelyOOB = new List<VEdge>();
			partialOOB = new List<VEdge>();

			foreach (var edge in polygon.GetVoronoiEdges())
			{
				bool startOOB = !IsInMapBounds(edge.start.position);
				bool endOOB = !IsInMapBounds(edge.end.position);
				if (startOOB && endOOB)
					completelyOOB.Add(edge);
				else if (startOOB || endOOB)
					partialOOB.Add(edge);
			}

			return completelyOOB.Count > 0 || partialOOB.Count > 0;
		}

		public static List<Vector3> FindMapBoundsIntersection(Vector3 lineStart, Vector3 lineEnd)
		{
			List<Vector3> intersections = new List<Vector3>();
			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out Vector2 leftSide))
				intersections.Add(leftSide);
			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out Vector2 topSide))
				intersections.Add(topSide);
			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out Vector2 rightSide))
				intersections.Add(rightSide);
			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out Vector2 bottomSide))
				intersections.Add(bottomSide);
			return intersections;
		}


		public static bool TryGetFirstMapBoundsIntersection(Vector2 lineStart, Vector2 lineEnd, out Vector2 intersectPoint)
		{
			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out intersectPoint))
				return true;
			return false;
		}


		public static bool TryGetBoundsIntersection(Vector2 lineStart, Vector2 lineEnd, out Dictionary<MapSide, Vector2> intersections, out byte corner)
		{
			intersections = new Dictionary<MapSide, Vector2>();
			corner = (int)MapSide.Inside;

			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out Vector2 top))
			{
				intersections.Add(MapSide.Top, top);
				corner |= (int)MapSide.Top;
			}

			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out Vector2 bottom))
			{
				intersections.Add(MapSide.Bottom, bottom);
				corner |= (int)MapSide.Bottom;
			}

			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out Vector2 left))
			{
				intersections.Add(MapSide.Left, left);
				corner |= (int)MapSide.Left;
			}

			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out Vector2 right))
			{
				intersections.Add(MapSide.Right, right);
				corner |= (int)MapSide.Right;
			}

			return intersections.Count > 0;
		}


		/// <summary>
		/// Algo modified from http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="p4"></param>
		/// <param name="intersectPoint"></param>
		/// <returns></returns>
		public static bool TryGetLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectPoint)
		{
			return TryGetLineIntersections(p1, p2, p3, p4,
				out intersectPoint, out float t1, out float t2);
		}

		public static bool TryGetLineIntersection(Edge<Centroid> delaunay, Edge<Corner> voronoi, out Vector2 intersectPoint, out float t1, out float t2)
		{
			return TryGetLineIntersections(delaunay.start.position, delaunay.end.position,
				voronoi.start.position, voronoi.end.position, out intersectPoint, out t1, out t2);
		}


		public static bool TryGetLineIntersections(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectPoint, out float t1, out float t2)
		{
			float dx12 = p1.x - p2.x;
			float dy12 = p1.y - p2.y;
			float dx34 = p4.x - p3.x;
			float dy34 = p4.y - p3.y;

			float denominator = (dy12 * dx34 - dx12 * dy34);
			t1 = ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34) / denominator;

			if (float.IsInfinity(t1))
			{
				t2 = float.PositiveInfinity;
				// The lines are parallel (or close enough to it).
				intersectPoint = Vector2.positiveInfinity;
				return false;
			}

			t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;


			if ((t2 > mapBoundsTolerance) && (t2 < 1 - mapBoundsTolerance))
			{
				// Find the point of intersection.
				intersectPoint = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1); // this is intersect point on line 1
				return true;
			}

			// if (t1 > 0) && (t1 < 1)))
			//{ // this is intersect point on line 2.
			//	// The result is the same Vector as line 1 but the value of t2 may be useful in the future.
			//	intersectPoint = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2); 
			//	return true;
			//}

			// segments do not intersect
			intersectPoint = Vector2.positiveInfinity;
			return false;
		}


		private void CreateRegions()
		{
			if (!createRegions || debugBorders || !clampToMapBounds)
				return;
			regions = new List<Region>();

			noiseSettings.seed = randomSeed;
			HeightMap heightMap = VoronoiHeightMapGenerator.GenerateHeightMap(
				noiseSettings.mapResolution * mapWidth, noiseSettings.mapResolution * mapHeight, heightMapSettings, noiseSettings, Vector2.zero);
			DrawNoiseTexture(VoronoiTextureGenerator.TextureFromHeightMap(heightMap));

			
			foreach (var polygon in vGraph.polygons)
			{
				GameObject regionGO = Instantiate(regionPrefab, regionHolder);
				Region region = regionGO.GetComponent<Region>();
				regions.Add(region);
				region.CreateRegion(polygon);
				region.ToggleBorder(bordersEnabled);
			}

			GenerateTexture();
			
			// set average height of region
			foreach (var region in regions)
			{
				region.SetRegionHeight(noiseSettings.mapResolution, heightMap.values, regionMaterial);
			}


		public void GenerateTexture()
		{
			textureData.ApplyToMaterial(regionMaterial);
			textureData.UpdateMeshHeights(regionMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
			regionMaterial.SetFloat("minHeight", heightMapSettings.minHeight);
			regionMaterial.SetFloat("maxHeight", heightMapSettings.maxHeight);
		}

		public void DrawNoiseTexture(Texture2D texture)
		{
			noisePreviewMeshRenderer.sharedMaterial.mainTexture = texture;
			noisePreviewMeshRenderer.transform.localScale = new Vector3(texture.width, texture.height, 10) / 10;

			noisePreviewMeshRenderer.gameObject.SetActive(true);
		}

		private bool IsTooNear(List<Centroid> sites, Vector2 site)
		{
			for (int i = 0; i < sites.Count; ++i)
			{
				Vector2 check = sites[i].position;
				if (check == site)
					continue;
				if ((check - site).sqrMagnitude < minSqrDistBtwnSites)
					return true;
			}

			return false;
		}

		private bool IsTooNear(List<Vector2> sites, Vector2 site)
		{
			for (int i = 0; i < sites.Count; ++i)
			{
				Vector2 check = sites[i];
				if (check == site)
					continue;
				if ((check - site).sqrMagnitude < minSqrDistBtwnSites)
					return true;
			}

			return false;
		}

		private void CheckDistanceAndEliminate(List<Vector2> centroids, float minSqrDistanceBetweenPoints)
		{
			List<Vector2> remove = new List<Vector2>();
			int amtChecked = 0;
			for (int i = 0; i < centroids.Count - 1; ++i)
			{
				Vector2 check = centroids[i];
				for (int j = i + 1; j < centroids.Count; ++j)
				{
					if ((check - centroids[j]).sqrMagnitude < minSqrDistanceBetweenPoints)
					{
						remove.Add(centroids[j]);
					}
					++amtChecked;
				}
			}

			Debug.Log("removing " + remove.Count + " centroids.");
			foreach (var rem in remove)
				centroids.Remove(rem);
		}


		private void OnDrawGizmos()
		{
			if (dGraph != null)
			{
				if (viewDelaunayTriangles)
				{
					foreach (var triangle in dGraph.triangles)
					{
						if (triangle.isInvalidated)
							continue;
						Gizmos.color = Color.green;
						Gizmos.DrawLine(triangle.p1.position, triangle.p2.position);
						Gizmos.DrawLine(triangle.p2.position, triangle.p3.position);
						Gizmos.DrawLine(triangle.p3.position, triangle.p1.position);

						if (viewDelaunayCircles)
						{
							Gizmos.color = Color.blue;
							Gizmos.DrawWireSphere(triangle.realCenter, triangle.radius);
						}
					}
				}
				else if (viewDelaunayCircles)
				{
					foreach (var triangle in dGraph.triangles)
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawWireSphere(triangle.realCenter, triangle.radius);
					}
				}

				if (viewCenteroids)
				{
					Gizmos.color = Color.white;
					foreach (var centroid in dGraph.centroids)
						Gizmos.DrawSphere(centroid.position, .25f);
				}
			}

			if (vGraph != null && viewVoronoiPolygons)
			{

			}

			// draw map bounds
			Gizmos.color = Color.black;
			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(topRight, bottomRight);
			Gizmos.DrawLine(bottomRight, bottomLeft);
			Gizmos.DrawLine(bottomLeft, topLeft);

		}
	}
}