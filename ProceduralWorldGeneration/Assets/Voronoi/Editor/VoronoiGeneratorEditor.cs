using AtomosZ.Voronoi;
using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.Voronoi.Editors
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
				gen.GenerateMap();
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
				int halfOOB = 0, OOB = 0;

				foreach (var edge in VoronoiGraph.uniqueVEdges)
				{
					if (VoronoiGenerator.mapBounds.Contains(edge.start.position)
						&& VoronoiGenerator.mapBounds.Contains(edge.end.position))
					{
						Handles.color = Color.blue;
					}
					else if (VoronoiGenerator.mapBounds.Contains(edge.start.position)
						|| VoronoiGenerator.mapBounds.Contains(edge.end.position))
					{
						Handles.color = Color.yellow;
						foreach (var intersection in gen.FindMapBoundsIntersection(edge.start.position, edge.end.position))
						{
							Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .5f, EventType.Repaint);
						}

						++halfOOB;
					}
					else
					{
						++OOB;
						Handles.color = Color.red;
						foreach (var intersection in gen.FindMapBoundsIntersection(edge.start.position, edge.end.position))
						{
							Handles.CylinderHandleCap(0, intersection, Quaternion.identity, .5f, EventType.Repaint);
						}
					}

					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}

				//Debug.Log("halfOOB: " + halfOOB + " OOB: " + OOB);

				foreach (var corner in VoronoiGraph.uniqueCorners)
				{
					if (VoronoiGenerator.mapBounds.Contains(corner.position))
						Handles.color = Color.white;
					else
						Handles.color = Color.red;
					Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .25f, EventType.Repaint);
				}
			}
		}
	}

	[CustomEditor(typeof(VoronoiLibGenerator))]
	public class VoronoiLibGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			VoronoiLibGenerator gen = (VoronoiLibGenerator)target;
			if (GUILayout.Button("Generate Voronoi") || DrawDefaultInspector())
			{
				gen.GeneratePoints();
			}
		}
	}
}