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
					if (corner.isOOB)
						Handles.color = Color.red;
					else if (corner.isOnBorder)
						Handles.color = Color.green;
					else
						Handles.color = Color.white;

					Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .25f, EventType.Repaint);
				}
			}
		}
	}
}