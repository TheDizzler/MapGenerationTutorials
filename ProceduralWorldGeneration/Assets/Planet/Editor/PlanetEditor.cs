using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.Planets.Editors
{
	[CustomEditor(typeof(Planet))]
	public class PlanetEditor : Editor
	{
		Planet planet;
		Editor shapeEditor;
		Editor colorEditor;


		void OnEnable()
		{
			planet = (Planet)target;
		}

		public override void OnInspectorGUI()
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				base.OnInspectorGUI();
				if (check.changed)
					planet.GeneratePlanet();
			}

			if (GUILayout.Button("Generate"))
				planet.GeneratePlanet();

			DrawSettingsEditor(planet.shapeSettings, planet.OnShapeSettingsUpdated, ref planet.shapeSettingsFoldout, ref shapeEditor);
				DrawSettingsEditor(planet.colorSettings, planet.OnColorSettingsUpdated, ref planet.colorSettingsFoldout, ref colorEditor);
			
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
	}
}