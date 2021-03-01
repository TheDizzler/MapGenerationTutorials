using System;
using AtomosZ.Voronoi.Regions;
using UnityEditor;
using UnityEngine;
using static AtomosZ.Voronoi.Regions.BiomeSettings;

namespace AtomosZ.Voronoi.EditorTools
{
	[CustomEditor(typeof(BiomeSettings))]
	public class BiomeEditor : Editor
	{
		private GUIStyle zoneBG;
		private Texture2D[] colorTextures = new Texture2D[2];


		private void OnEnable()
		{
			zoneBG = new GUIStyle();

			colorTextures[0] = MakeBGTexture(Color.red);
			colorTextures[1] = MakeBGTexture(Color.blue);
		}


		public override void OnInspectorGUI()
		{
			BiomeSettings biomes = (BiomeSettings)target;
			EditorStyles.label.wordWrap = true;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("riverSubdivisions"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("riverAmplitude"));

			if (biomes.biomeColors.Count != Enum.GetNames(typeof(BiomeType)).Length)
				for (int i = biomes.biomeColors.Count; i < Enum.GetNames(typeof(BiomeType)).Length; ++i)
					biomes.biomeColors.Add(Color.black);

			for (int i = 0; i < Enum.GetNames(typeof(BiomeType)).Length; ++i)
			{
				BiomeType biomeType = (BiomeType)i;
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField(biomeType.ToString());
					biomes.biomeColors[i] = EditorGUILayout.ColorField(biomes.biomeColors[i], GUILayout.Width(120));
				}
				EditorGUILayout.EndHorizontal();
			}

			biomes.isElevationFoldout = EditorGUILayout.Foldout(biomes.isElevationFoldout, "Elevation Start Heights", true);
			if (biomes.isElevationFoldout)
			{
				for (int i = 0; i < biomes.elevationStartHeights.Length; ++i)
				{
					EditorGUI.indentLevel = 1;
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField(((ElevationZone)i).ToString(), GUILayout.Width(60));
						biomes.elevationStartHeights[i] = EditorGUILayout.DelayedFloatField(biomes.elevationStartHeights[i]);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUI.indentLevel = 0;
				}
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("biomeDictionary"));

			serializedObject.ApplyModifiedProperties();
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