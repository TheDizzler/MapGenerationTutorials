﻿using System;
using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	public class VEdge : Edge<Corner>
	{
		public static int count = 0;
		private static float minSegmentLengthToSubdivide = .5f;

		public List<Vector3> segments;

		private List<Polygon> polygons = new List<Polygon>();
		private DEdge pairedEdge;
		

		public VEdge(Corner p1, Corner p2) : base(p1, p2)
		{
			num = count++;
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}


		public void AddPolygon(Polygon polygon)
		{
			polygons.Add(polygon);
			if (polygons.Count > 2)
				throw new System.Exception("An edge may not have more than 2 polygons!");
		}

		public void Remove(Polygon polygon)
		{
			polygons.Remove(polygon);
			Debug.Log("Edge " + num + " has no polygons - deleting");
			VoronoiGraph.uniqueVEdges.Remove(this);
		}

		public List<Polygon> GetPolygons()
		{
			return polygons;
		}

		public bool Contains(Polygon polygon)
		{
			return polygons.Contains(polygon);
		}

		public int GetPolygonCount()
		{
			return polygons.Count;
		}

		/// <summary>
		/// https://www.redblobgames.com/maps/noisy-edges/
		/// </summary>
		public void CreateNoisyEdge(int subdivisions)
		{
			if (segments != null) // already noisified this edge
				return;

			if (polygons.Count != 2)
			{
				if (polygons.Count > 2 || polygons.Count == 0)
					throw new Exception("Invalid polygon count in edge " + num + ". Count: " + polygons.Count);
				//Debug.LogWarning("Edge not connected to 2 polygons. Cannot create noisy edge.");
				CreateSimpleBorder();
				return;
			}


			pairedEdge = polygons[0].centroid.GetConnectingEdge(polygons[1].centroid);
			if (pairedEdge == null)
				throw new Exception("Invalid delaunay edge");

			if (!VoronoiGenerator.TryGetLineIntersection(
				pairedEdge, this, out Vector2 intersectPoint, out float t1, out float t2)) // t1 is always negative. Is this normal?
			{
				Debug.LogWarning("SPECIAL CASE: voronoi and delaunay edges do not meet");
				CreateSimpleBorder();
				return;
			}

			if (subdivisions > 0)
			{
				segments = CreateSegments(
					pairedEdge.start.position, pairedEdge.end.position, start.position, end.position,
					subdivisions, VoronoiGenerator.GetNewT());
				segments.Insert(0, start.position);
			}
			else
			{
				segments = new List<Vector3>();
				segments.Add(start.position);
			}

			
			segments.Add(end.position);
		}

		private void CreateSimpleBorder()
		{
			segments = new List<Vector3>();
			segments.Add(start.position);
			segments.Add(end.position);
		}

		private static List<Vector3> CreateSegments(Vector3 control1, Vector3 control2, Vector3 line1, Vector3 line2, int subdivisions, float t)
		{
			List<Vector3> segmentPoints = new List<Vector3>();

			Vector3 midPoint = Vector3.Lerp(control1, control2, t);
			if (subdivisions > 1 && Vector3.Distance(line1, line2) > minSegmentLengthToSubdivide)
			{
				float t1 = VoronoiGenerator.GetNewT();
				float t2 = VoronoiGenerator.GetNewT();
				Vector3 edgeCenter1 = (line1 + control1) * .5f;
				Vector3 edgeCenter2 = (line1 + control2) * .5f;
				segmentPoints.AddRange(CreateSegments(edgeCenter1, edgeCenter2, line1, midPoint, subdivisions - 1, t1));

				segmentPoints.Add(midPoint);

				Vector3 edgeCenter3 = (line2 + control1) * .5f;
				Vector3 edgeCenter4 = (line2 + control2) * .5f;
				segmentPoints.AddRange(CreateSegments(edgeCenter3, edgeCenter4, midPoint, line2, subdivisions - 1, t2));
			}
			else
				segmentPoints.Add(midPoint);

			return segmentPoints;
		}

		/// <summary>
		/// Replaces oldSite with newSite in this edge, using newSites position and all other properties.
		/// Connects newSite with polygons this edge is border of but does not remove oldSite from polygons.
		/// </summary>
		/// <param name="oldSite"></param>
		/// <param name="newSite"></param>
		public void ReplaceSite(Corner oldSite, Corner newSite)
		{
			if (!Contains(oldSite))
				throw new System.Exception("This edge does not contain input oldSite");
			if (start == oldSite)
			{
				start.connectedEdges.Remove(this);
				start = newSite;
				start.connectedEdges.Add(this);
			}
			else
			{
				end.connectedEdges.Remove(this);
				end = newSite;
				end.connectedEdges.Add(this);
			}

			foreach (var polygon in polygons)
			{
				if (!polygon.corners.Contains(newSite))
					VoronoiHelper.Associate(polygon, newSite);
			}
		}

		public bool SharesCorner(VEdge lastEdge, out Corner sharedCorner)
		{
			sharedCorner = null;
			if (lastEdge.Contains(start))
				sharedCorner = start;
			else if (lastEdge.Contains(end))
				sharedCorner = end;

			return sharedCorner != null;
		}

		public bool HasCornerOnBorder(out List<Corner> borderCorners)
		{
			borderCorners = new List<Corner>();
			if (start.isOnBorder)
				borderCorners.Add(start);
			if (end.isOnBorder)
				borderCorners.Add(end);

			return borderCorners.Count > 0;
		}

		public List<Polygon> GetSharedPolygons(VEdge edge)
		{
			List<Polygon> shared = new List<Polygon>();
			foreach (Polygon poly in polygons)
			{
				if (edge.Contains(poly))
					shared.Add(poly);
			}
			return shared;
		}
	}

	public class DEdge : Edge<Centroid>
	{
		private static int count = 0;

		public DEdge(Centroid p1, Centroid p2) : base(p1, p2)
		{
			num = count++;
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}
	}

	/// <summary>
	/// A line that connects to sites.
	/// </summary>
	public abstract class Edge<TSite> where TSite : Site
	{
		public TSite start, end;
		public int num;


		public Edge(TSite p1, TSite p2)
		{
			start = p1;
			end = p2;
		}

		public bool Contains(TSite p2)
		{
			return start == p2 || end == p2;
		}

		public TSite GetOppositeSite(TSite site)
		{
			if (!Contains(site))
				throw new System.Exception("This edge does not contain input site");
			if (start == site)
				return end;
			return start;
		}
	}
}