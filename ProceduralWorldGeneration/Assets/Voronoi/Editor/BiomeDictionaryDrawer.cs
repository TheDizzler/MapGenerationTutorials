using System;
using AtomosZ.Voronoi.Regions;
using UnityEditor;
using UnityEngine;
using static AtomosZ.Voronoi.Regions.BiomeSettings;

namespace AtomosZ.Voronoi.EditorTools
{
	[CustomPropertyDrawer(typeof(BiomeDictionary))]
	public class BiomeDictionaryDrawer : PropertyDrawer
	{
		private const int columnWidth = 115;

		GUIStyle moistureStyle;
		GUIStyle elevationStyle;
		GUIStyle biomeLabelStyle;
		GUIStyle zoneStyle;
		private BiomeSettings settings;

		public BiomeDictionaryDrawer() : base()
		{
			moistureStyle = new GUIStyle(EditorStyles.helpBox);
			moistureStyle.alignment = TextAnchor.MiddleCenter;
			//moistureStyle.fixedHeight = EditorGUIUtility.singleLineHeight;
			moistureStyle.normal.background = MakeBGTexture(Color.yellow);

			elevationStyle = new GUIStyle(EditorStyles.helpBox);
			elevationStyle.alignment = TextAnchor.MiddleCenter;
			elevationStyle.fixedHeight = EditorGUIUtility.singleLineHeight * 2;
			elevationStyle.normal.background = MakeBGTexture(Color.grey);

			biomeLabelStyle = new GUIStyle(EditorStyles.miniLabel);
			biomeLabelStyle.alignment = TextAnchor.MiddleCenter;
			//elevationStyle.fixedHeight = EditorGUIUtility.singleLineHeight;
			//biomeLabelStyle.normal.background = MakeBGTexture(Color.grey);

			zoneStyle = new GUIStyle();

		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
				return EditorGUIUtility.singleLineHeight * 1f;
			return EditorGUIUtility.singleLineHeight * (Enum.GetNames(typeof(ElevationZone)).Length * 2 + 2);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect rect = position;
			rect.height = EditorGUIUtility.singleLineHeight;
			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, "Biomes", true, EditorStyles.foldoutHeader);

			if (property.isExpanded)
			{
				if (settings == null)
					settings = ((BiomeSettings)property.serializedObject.targetObject);

				GUI.backgroundColor = Color.red;
				rect.y += EditorGUIUtility.singleLineHeight;
				float startX = rect.x;
				//rect.x = 0;
				rect.width = columnWidth;
				EditorGUI.LabelField(rect, "Zones");

				foreach (var moistureZone in Enum.GetNames(typeof(MoistureZone)))
				{
					rect.x += columnWidth;
					EditorGUI.LabelField(rect, moistureZone, moistureStyle);
				}

				rect.x = startX;

				var values = Enum.GetValues(typeof(ElevationZone));

				for (int i = 0; i < values.Length; ++i)
				{
					rect.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.LabelField(rect, values.GetValue(i).ToString(), elevationStyle);

					var elevation = BiomeDictionary.biomeLookup[i];
					for (int j = 0; j < elevation.Length; ++j)
					{
						rect.x += columnWidth;
						EditorGUI.ColorField(rect, settings.biomeColors[elevation[j]]);
						rect.y += EditorGUIUtility.singleLineHeight;
						EditorGUI.LabelField(rect, ((BiomeType)elevation[j]).ToString(), biomeLabelStyle);
						rect.y -= EditorGUIUtility.singleLineHeight;
					}

					rect.x = startX;
					rect.y += EditorGUIUtility.singleLineHeight;
				}
			}

		}


		private Texture2D MakeBGTexture(Color col)
		{
			Color[] pix = new Color[1];
			pix[0] = col;
			Texture2D result = new Texture2D(1, 1);
			result.SetPixels(pix);
			result.Apply();

			return result;
		}
	}
}