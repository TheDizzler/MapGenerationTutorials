using UnityEditor;
using UnityEngine;

namespace AtomosZ.Voronoi.Editors
{
	[CustomEditor(typeof(VoronoiGenerator))]
	public class VoronoiGeneratorEditor : Editor
	{
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
				//if (VoronoiGraph.boundCrossingEdges != null)
				//{
				//	Handles.color = new Color(1, .62f, .016f, 1);
				//	for (int i = 0; i < VoronoiGraph.boundCrossingEdges[VoronoiGenerator.MapSide.Top].Count; ++i)
				//	{
				//		var edge = VoronoiGraph.boundCrossingEdges[VoronoiGenerator.MapSide.Top][i];
				//		Vector2 nextPos;
				//		if (i == VoronoiGraph.boundCrossingEdges[VoronoiGenerator.MapSide.Top].Count - 1)
				//			nextPos = VoronoiGenerator.topRight;
				//		else
				//			nextPos = VoronoiGraph.boundCrossingEdges[VoronoiGenerator.MapSide.Top][i + 1].intersectPosition;
				//		Handles.ArrowHandleCap(i, edge.intersectPosition, Quaternion.LookRotation(Vector3.right), Vector2.Distance(edge.intersectPosition, nextPos), EventType.Repaint);
				//		Handles.Label((edge.intersectPosition + nextPos) / 2, "" + i);
				//	}
				//}

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
						foreach (var intersection in VoronoiGenerator.FindMapBoundsIntersection(edge.start.position, edge.end.position))
						{
							Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .5f, EventType.Repaint);
						}
					}
					else
					{
						Handles.color = Color.red;
						foreach (var intersection in VoronoiGenerator.FindMapBoundsIntersection(edge.start.position, edge.end.position))
						{
							Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .5f, EventType.Repaint);
						}
					}

					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}


				foreach (var corner in VoronoiGraph.uniqueCorners)
				{
					if (corner.isInvalidated)
						Handles.color = new Color(.5f, .5f, .5f, .5f);
					else if (corner.isOOB)
						Handles.color = Color.red;
					else if (corner.isOnBorder)
						Handles.color = Color.green;
					else
						Handles.color = Color.white;

					Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .25f, EventType.Repaint);
				}


				GUIStyle style = new GUIStyle();
				Vector2 textOffset = new Vector2(.25f, .20f);
				Handles.color = Color.cyan;
				foreach (var polygon in VoronoiGenerator.debugPolygons)
				{
					
					foreach (var edge in polygon.voronoiEdges)
					{
						Handles.DrawDottedLine(edge.start.position, edge.end.position, 1);
					}

					
					foreach (var corner in polygon.corners)
					{
						Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .375f, EventType.Repaint);
					}

					style.normal.textColor = Color.cyan;
					int i = 0;
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
					Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .375f, EventType.Repaint);

			}
		}
	}
}