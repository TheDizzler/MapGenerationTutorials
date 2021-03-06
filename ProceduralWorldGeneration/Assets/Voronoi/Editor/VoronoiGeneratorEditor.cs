﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AtomosZ.Voronoi.VoronoiGenerator;
using Object = UnityEngine.Object;

namespace AtomosZ.Voronoi.EditorTools
{
	[CustomEditor(typeof(VoronoiGenerator))]
	public class VoronoiGeneratorEditor : Editor
	{
		private readonly Color cornerGreen = new Color(0, 1, 0, .25f);

		private GUIStyle style = new GUIStyle();
		private Vector3 textOffset = new Vector3(.25f, .20f, -.15f);
		private Editor shapeEditor;
		private Editor colorEditor;
		public bool colorSettingsFoldout;
		public bool shapeSettingsFoldout;
		private bool mapGenerationSettingsFoldout = true;


		public override void OnInspectorGUI()
		{
			VoronoiGenerator gen = (VoronoiGenerator)target;
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				mapGenerationSettingsFoldout = EditorGUILayout.Foldout(mapGenerationSettingsFoldout, new GUIContent("Map Generation Settings"));
				if (mapGenerationSettingsFoldout)
				{
					EditorGUI.indentLevel = 1;
					gen.newRandomSeed = EditorGUILayout.Toggle(new GUIContent("New RandomSeed"), gen.newRandomSeed);
					gen.randomSeed = EditorGUILayout.DelayedTextField(new GUIContent("RandomSeed"), gen.randomSeed);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("mapWidth"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("mapHeight"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("regionAmount"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSqrDistBtwnSites"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistBtwnSiteAndBorder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistBtwnCornerAndBorder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minEdgeLengthToMerge"));
					GUI.enabled = false;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("mergeNearCorners"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("clampToMapBounds"));
					GUI.enabled = true;
					EditorGUI.indentLevel = 0;
				}

				gen.borderSettingsFoldout = EditorGUILayout.Foldout(gen.borderSettingsFoldout, new GUIContent("Border Settings"));
				if (gen.borderSettingsFoldout)
				{
					EditorGUI.indentLevel = 1;
					bool borders = EditorGUILayout.Toggle(new GUIContent("Show Borders"), gen.bordersEnabled);
					if (borders != gen.bordersEnabled)
						gen.ToggleBorders(borders);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("subdivisions"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("amplitude"));
					EditorGUI.indentLevel = 0;
				}

				EditorGUILayout.PropertyField(serializedObject.FindProperty("regionMaterial"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("sideMaterial"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("regionPrefab"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("regionHolder"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("noisePreviewMeshRenderer"));

				if (check.changed || GUILayout.Button("Generate Voronoi"))
				{
					AtomosZ.EditorTools.EditorUtils.ClearLogConsole();
					gen.GenerateMap();
					EditorUtility.SetDirty(target);
				}

				if (GUILayout.Button("Clear Map"))
					gen.ClearMap();
			}

			using (var check = new EditorGUI.ChangeCheckScope())
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("noiseSettings"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("heightMapSettings"));
				if (check.changed)
				{
					gen.GenerateTexture();
				}
			}

			gen.mapDebugFoldout = EditorGUILayout.Foldout(gen.mapDebugFoldout, new GUIContent("Map Debug Settings"));
			if (gen.mapDebugFoldout)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewDelaunayCircles"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewDelaunayTriangles"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewCenteroids"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewVoronoiPolygons"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewCorners"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewCornerIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewEdgeIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewIntersections"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewIntersectionIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewIntersectionDirections"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("viewRegionIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("debugSlopeDirections"));
				gen.debugBorders = EditorGUILayout.Toggle(new GUIContent("Debug Borders"), gen.debugBorders);
				if (gen.debugBorders)
				{
					EditorGUI.indentLevel = 2;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("topBorder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("rightBorder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("bottomBorder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("leftBorder"));
					EditorGUI.indentLevel = 1;
				}

				gen.debugNoisyLine = EditorGUILayout.Foldout(gen.debugNoisyLine, new GUIContent("Noisy Line Debug"));
				if (gen.debugNoisyLine)
				{
					EditorGUI.indentLevel = 2;

					EditorGUILayout.PropertyField(serializedObject.FindProperty("debugSubdivisions"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("debugAmplitude"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("startNoisy"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("endNoisy"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("startControl"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("endControl"));

					if (GUILayout.Button("Generate Noisy Line"))
					{
						AtomosZ.EditorTools.EditorUtils.ClearLogConsole();
						gen.GenerateNoisyLineDebug();
						EditorUtility.SetDirty(target);
					}
					EditorGUI.indentLevel = 1;
				}

				EditorGUI.indentLevel = 0;
			}

			serializedObject.ApplyModifiedProperties();
		}


		private void DrawSettingsEditor(Object settings, System.Action OnSettingsUpdated, ref bool foldOut, ref Editor editor)
		{
			if (settings == null)
				return;

			foldOut = EditorGUILayout.InspectorTitlebar(foldOut, settings);
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				if (foldOut)
				{
					CreateCachedEditor(settings, null, ref editor);
					editor.OnInspectorGUI();

					if (check.changed)
					{
						OnSettingsUpdated();
					}
				}
			}
		}


		/// <summary>
		/// Debug quad for generating noisy edge.
		/// </summary>
		public class Quad
		{
			public List<Vector3> points = new List<Vector3>();
			private Vector3 control1, control2;
			private Vector3 line1, line2;
			private Vector3 midPoint;
			private List<Quad> quads = new List<Quad>();
			private float tMid;


			public Quad(
				Vector3 control1, Vector3 control2, Vector3 line1, Vector3 line2,
				int subdivisions, float tMid, float amplitude, System.Random rng)
			{
				this.control1 = control1;
				this.control2 = control2;
				this.line1 = line1;
				this.line2 = line2;
				this.tMid = tMid;

				float lineDist = Vector3.Distance(line1, line2);
				float controlDist = Vector3.Distance(control1, control2);
				if (lineDist < controlDist)
				{
					float diff = (controlDist - lineDist) * .5f;

					Vector3 newControl1 = Vector3.MoveTowards(control1, control2, diff);
					Vector3 newControl2 = Vector3.MoveTowards(control2, control1, diff);
					control1 = newControl1;
					control2 = newControl2;
				}

				if (subdivisions > 0)
				{
					midPoint = Vector3.Lerp(control1, control2,
						Mathf.Lerp(tMid, (float)rng.NextDouble(), amplitude));
					CreateSubQuads(subdivisions, amplitude, rng);
				}
			}

			private void CreateSubQuads(int subdivisions, float amplitude, System.Random rng)
			{
				Vector3 edgeCenter1 = (line1 + control1) * .5f;
				Vector3 edgeCenter2 = (line1 + control2) * .5f;

				quads.Add(new Quad(edgeCenter1, edgeCenter2, line1, midPoint,
					subdivisions - 1, tMid, amplitude, rng));

				Vector3 edgeCenter3 = (line2 + control1) * .5f;
				Vector3 edgeCenter4 = (line2 + control2) * .5f;

				quads.Add(new Quad(edgeCenter3, edgeCenter4, midPoint, line2,
					subdivisions - 1, tMid, amplitude, rng));

				points.AddRange(quads[0].points);
				points.Add(midPoint);
				points.AddRange(quads[1].points);
			}


			public void Draw()
			{
				Handles.color = Color.grey;
				Handles.DrawDottedLine(line1, control1, 2);
				Handles.DrawDottedLine(line1, control2, 2);
				Handles.color = new Color(.5f, .5f, .25f, 1);
				Handles.DrawDottedLine(line2, control1, 2);
				Handles.DrawDottedLine(line2, control2, 2);

				foreach (Quad quad in quads)
					quad.Draw();
			}
		}

		private Quad quad;


		void OnSceneGUI()
		{
			VoronoiGenerator gen = (VoronoiGenerator)target;

			if (gen.lr != null)
			{
				if (quad == null || gen.wasReset)
				{
					if (VoronoiGenerator.instance == null)
						gen.GenerateNoisyLineDebug();
					var lr = gen.lr;
					VoronoiGenerator.TryGetLineIntersections(
						gen.startControl, gen.endControl, gen.startNoisy, gen.endNoisy,
						out Vector2 intersectPoint, out float t1, out float t2);
					Debug.Log("t1: " + t1 + " t2: " + t2);

					quad = new Quad(gen.startControl, gen.endControl, gen.startNoisy, gen.endNoisy,
						gen.debugSubdivisions, -t1, gen.debugAmplitude, new System.Random());
					List<Vector3> positions = new List<Vector3>();
					positions.Add(gen.startNoisy);
					positions.AddRange(quad.points);
					positions.Add(gen.endNoisy);
					lr.SetPositions(positions.ToArray());

					gen.wasReset = false;
				}
				else
					quad.Draw();
			}

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
								Handles.ArrowHandleCap(i, edge.intersectPosition,
									Quaternion.LookRotation(Vector3.right + new Vector3(0, .5f, 0)),
									Vector2.Distance(edge.intersectPosition, nextPos), EventType.Repaint);
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
						Handles.Label((edge.start.position + edge.end.position) * .5f + textOffset, "" + edge.id, style);
					}
				}



				Handles.color = Color.cyan;
				foreach (var polygon in VoronoiGenerator.debugPolygons)
				{
					foreach (var edge in polygon.GetVoronoiEdges())
					{
						Handles.DrawDottedLine(edge.start.position, edge.end.position, 1);
					}

					foreach (var corner in polygon.corners)
					{
						Handles.SphereHandleCap(0, corner.position, Quaternion.identity, .175f, EventType.Repaint);
					}

					int i = 0;
					style.normal.textColor = Color.cyan;
					foreach (var edge in polygon.GetVoronoiEdges())
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

			if (gen.regions != null && gen.viewRegionIDs)
			{
				foreach (var region in gen.regions)
				{
					if (region == null || region.polygon == null)
						break;
					Handles.Label(region.polygon.centroid.position, "" + region.id, style);
				}
			}

			if (VoronoiGraph.uniqueCorners != null && gen.debugSlopeDirections)
			{
				foreach (Corner corner in VoronoiGraph.uniqueCorners)
				{
					if (corner.downSlope != null)
					{
						Vector3 cornerPos = Quaternion.AngleAxis(90, Vector3.right) * corner.position;
						Vector3 targetPos = Quaternion.AngleAxis(90, Vector3.right) * corner.downSlope.position;

						Handles.ArrowHandleCap(0, cornerPos,
							Quaternion.LookRotation(targetPos - cornerPos, Vector3.up), 1, EventType.Repaint);
					}
				}
			}
		}
	}
}