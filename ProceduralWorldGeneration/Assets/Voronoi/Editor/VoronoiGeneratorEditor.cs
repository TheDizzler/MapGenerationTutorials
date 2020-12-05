using System;
using UnityEditor;
using UnityEngine;
using static AtomosZ.Voronoi.VoronoiGenerator;

namespace AtomosZ.Voronoi.Editors
{
	[CustomEditor(typeof(VoronoiGenerator))]
	public class VoronoiGeneratorEditor : Editor
	{
		private readonly Color cornerGreen = new Color(0, 1, 0, .25f);

		private GUIStyle style = new GUIStyle();
		private Vector2 textOffset = new Vector2(.25f, .20f);


		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			VoronoiGenerator gen = (VoronoiGenerator)target;
			if (GUILayout.Button("Generate Voronoi"))
			{
				EditorTools.EditorUtils.ClearLogConsole();
				gen.GenerateMap();
				EditorUtility.SetDirty(target);
			}

			if (GUILayout.Button("AddPoint"))
			{
				gen.AddPoint();
			}
		}

		void OnSceneGUI()
		{
			VoronoiGenerator gen = (VoronoiGenerator)target;

			if (gen.dGraph != null && gen.viewDelaunayTriangles)
			{
				Handles.color = Color.red;
				foreach (var edge in gen.dGraph.edges)
				{
					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}
			}

			if (gen.vGraph != null && gen.viewVoronoiPolygons)
			{
				if (gen.viewIntersectionDirections)
				{
					if (VoronoiGraph.boundCrossingEdges != null)
					{
						foreach (MapSide mapSide in (MapSide[])Enum.GetValues(typeof(MapSide)))
						{
							if (mapSide == MapSide.Inside)
								continue;
							Handles.color = new Color(1, .62f, .016f, 1);
							for (int i = 0; i < VoronoiGraph.boundCrossingEdges[mapSide].Count; ++i)
							{
								var edge = VoronoiGraph.boundCrossingEdges[mapSide][i];
								Vector2 nextPos;
								if (i == VoronoiGraph.boundCrossingEdges[mapSide].Count - 1)
									//nextPos = borderEndPoints[mapSide].Item2;
									continue;
								else
									nextPos = VoronoiGraph.boundCrossingEdges[mapSide][i + 1].intersectPosition;
								Handles.ArrowHandleCap(i, edge.intersectPosition, Quaternion.LookRotation(Vector3.right + new Vector3(0, .5f, 0)), Vector2.Distance(edge.intersectPosition, nextPos), EventType.Repaint);
								Handles.Label((edge.intersectPosition + nextPos) / 2, "" + i);
							}
						}
					}
				}

				style.normal.textColor = Color.black;
				if (gen.viewCorners)
				{
					foreach (var corner in VoronoiGraph.uniqueCorners)
					{
						if (corner.isInvalidated)
							Handles.color = new Color(.5f, .5f, .5f, .5f);
						else if (corner.isOOB)
							Handles.color = Color.red;
						else if (corner.isOnBorder)
							Handles.color = cornerGreen;
						else
							Handles.color = Color.white;

						Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .25f, EventType.Repaint);
						if (gen.viewCornerIDs)
							Handles.Label(corner.position + textOffset, "" + corner.num, style);
					}
				}

				int intersectCount = 0;
				style.normal.textColor = Color.yellow;
				foreach (var edge in VoronoiGraph.uniqueVEdges)
				{
					if (edge.start.isOnBorder && edge.end.isOnBorder)
						Handles.color = Color.green;
					else if (!edge.start.isOOB && !edge.end.isOOB)
					{
						Handles.color = Color.blue;
					}
					else if ((edge.start.isOOB ^ edge.end.isOOB) && !edge.start.isOnBorder && !edge.end.isOnBorder)
					{
						Handles.color = Color.yellow;
						style.normal.textColor = Color.yellow;
						if (gen.viewIntersections)
						{
							foreach (var intersection in VoronoiGenerator.FindMapBoundsIntersection(edge.start.position, edge.end.position))
							{
								Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .4f, EventType.Repaint);
								if (gen.viewIntersectionIDs)
									Handles.Label(intersection + textOffset, "" + (intersectCount++), style);
							}
						}
					}
					else
					{
						Handles.color = Color.red;
						if (gen.viewIntersections)
						{
							foreach (var intersection in VoronoiGenerator.FindMapBoundsIntersection(edge.start.position, edge.end.position))
							{ // this is a very rare occurence of an edge that starts on a border and intersects with a different map edge.
								Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .4f, EventType.Repaint);
								if (gen.viewIntersectionIDs)
									Handles.Label(intersection + textOffset, "" + (intersectCount++), style);
								Handles.color = Color.yellow;
							}
						}
					}

					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
					if (gen.viewEdgeIDs)
					{
						style.normal.textColor = Color.cyan;
						Handles.Label((edge.start.position + edge.end.position) * .5f + textOffset, "" + edge.num, style);
					}
				}

				

				Handles.color = Color.cyan;
				foreach (var polygon in VoronoiGenerator.debugPolygons)
				{
					foreach (var edge in polygon.voronoiEdges)
					{
						Handles.DrawDottedLine(edge.start.position, edge.end.position, 1);
					}

					foreach (var corner in polygon.corners)
					{
						Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .175f, EventType.Repaint);
					}

					int i = 0;
					style.normal.textColor = Color.cyan;
					foreach (var edge in polygon.voronoiEdges)
					{

						Handles.Label((edge.start.position + edge.end.position) / 2, "" + i, style);
						++i;
					}

					style.normal.textColor = Color.white;
					i = 0;
					foreach (var corner in polygon.corners)
					{
						Handles.Label(corner.position + textOffset, "" + i, style);
						++i;
					}
				}

				Handles.color = Color.magenta;
				foreach (var edge in VoronoiGenerator.debugEdges)
					Handles.DrawDottedLine(edge.start.position, edge.end.position, 1);

				foreach (var corner in VoronoiGenerator.debugCorners)
					Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .175f, EventType.Repaint);
			}
		}
	}
}