using System;
using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	public class VEdge : Edge<Corner>
	{
		public static int count = 0;

		public List<Vector3> segments;
		/// <summary>
		/// Have heights been calculated?
		/// </summary>
		public bool isHeightsSet = false;

		private List<Polygon> polygons = new List<Polygon>();
		private DEdge pairedEdge;
		/// <summary>
		/// A constraint to prevent sharp, ugly jaggies.
		/// </summary>
		private float maxSegmentDistanceFromOrigin;
		internal bool isRiver;

		public VEdge(Corner p1, Corner p2) : base(p1, p2)
		{
			id = count++;
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
			Debug.Log("Edge " + id + " has no polygons - deleting");
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
		/// Based off of https://www.redblobgames.com/maps/noisy-edges/
		/// </summary>
		public void CreateNoisyEdge(int subdivisions)
		{
			if (segments != null) // already noisified this edge
				return;

			if (polygons.Count != 2)
			{ // probably on the map edge
				if (polygons.Count > 2 || polygons.Count == 0)
					throw new Exception("Invalid polygon count in edge " + id + ". Count: " + polygons.Count);
				CreateSimpleBorder();
				return;
			}

			segments = new List<Vector3>();

			pairedEdge = polygons[0].centroid.GetConnectingEdge(polygons[1].centroid);
			if (pairedEdge == null)
				throw new Exception("Invalid delaunay edge");

			Vector3 control1 = pairedEdge.start.position;
			Vector3 control2 = pairedEdge.end.position;
			if (!VoronoiGenerator.TryGetLineIntersections(
				control1, control2, start.position, end.position,
				out Vector2 intersectPoint, out float tMid, out float t2))
			{
				Debug.LogWarning("SPECIAL CASE: voronoi and delaunay edges do not meet");
				CreateSimpleBorder();
				return;
			}

			segments.Add(start.position);
			segments.AddRange(
				CreateSegments(start.position, end.position,
				control1, control2, subdivisions, -tMid));
			segments.Add(end.position);
		}

		private List<Vector3> CreateSegments(
			Vector3 lineStart, Vector3 lineEnd,
			Vector3 control1, Vector3 control2, int subdivisions, float tMid)
		{
			List<Vector3> newSegments = new List<Vector3>();
			if (subdivisions > 0)
			{
				float lineDist = Vector3.Distance(lineStart, lineEnd);
				float controlDist = Vector3.Distance(control1, control2);
				if (lineDist < controlDist)
				{ // clamp the control points to stop ugly extreme lines
					float diff = (controlDist - lineDist) * .5f;

					Vector3 newControl1 = Vector3.MoveTowards(control1, control2, diff);
					Vector3 newControl2 = Vector3.MoveTowards(control2, control1, diff);
					control1 = newControl1;
					control2 = newControl2;
				}

				Vector3 edgeCenter1 = (lineStart + control1) * .5f;
				Vector3 edgeCenter2 = (lineStart + control2) * .5f;

				Vector3 midPoint = Vector3.Lerp(control1, control2,
					VoronoiGenerator.GetNewT(tMid, isRiver));
				newSegments.AddRange(
					CreateSegments(lineStart, midPoint, edgeCenter1, edgeCenter2, subdivisions - 1, tMid));

				newSegments.Add(midPoint);

				Vector3 edgeCenter3 = (lineEnd + control1) * .5f;
				Vector3 edgeCenter4 = (lineEnd + control2) * .5f;

				newSegments.AddRange(
					CreateSegments(midPoint, lineEnd, edgeCenter3, edgeCenter4, subdivisions - 1, tMid));
			}

			return newSegments;
		}


		/// <summary>
		/// Reverses start and end point as well as segments if they exist.
		/// </summary>
		public void ReverseEndPoints()
		{
			if (segments != null)
				segments.Reverse();
			var temp = start;
			start = end;
			end = temp;
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
			if (lastEdge == this)
				throw new System.Exception("Trying to find shared corner with self");
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

		private void CreateSimpleBorder()
		{
			segments = new List<Vector3>();
			segments.Add(start.position);
			segments.Add(end.position);
		}
	}

	public class DEdge : Edge<Centroid>
	{
		private static int count = 0;

		public DEdge(Centroid p1, Centroid p2) : base(p1, p2)
		{
			id = count++;
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
		public int id;


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