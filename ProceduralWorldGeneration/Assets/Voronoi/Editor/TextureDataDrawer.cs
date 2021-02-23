using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AtomosZ.Voronoi.EditorTools
{
	[CustomPropertyDrawer(typeof(VoronoiTextureData))]
	public class TextureDataDrawer : PropertyDrawer
	{
		private ReorderableList colorHeightList;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// The 6 comes from extra spacing between the fields (2px each)
			if (colorHeightList == null)
				return base.GetPropertyHeight(property, label);
			return (EditorGUIUtility.singleLineHeight + 10) * colorHeightList.count;
		}


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (colorHeightList == null)
			{
				colorHeightList = BuildColorHeightList(property);
			}

			colorHeightList.DoList(position);
			EditorGUI.EndProperty();
		}


		private ReorderableList BuildColorHeightList(SerializedProperty property)
		{
			var colorHeightMaps = property.FindPropertyRelative("colorHeightMaps");
			ReorderableList list = new ReorderableList(property.serializedObject, colorHeightMaps, true, true, true, true);

			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Color Height Map");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{ // an alternative to this would be to make another custom property drawer to encapsulate these
				var colorHeightMap = colorHeightMaps.GetArrayElementAtIndex(index);
				Rect colorRect = rect;
				colorRect.width *= .5f;
				Rect baseStartRect = colorRect;
				baseStartRect.x += colorRect.width;
				EditorGUIUtility.labelWidth = 50;
				colorHeightMap.FindPropertyRelative("baseColor").colorValue 
					= EditorGUI.ColorField(colorRect, new GUIContent("Color"),
					colorHeightMap.FindPropertyRelative("baseColor").colorValue, true, true, false);
				colorHeightMap.FindPropertyRelative("baseStartHeight").floatValue
					= EditorGUI.Slider(baseStartRect, new GUIContent("Start Height"),
					colorHeightMap.FindPropertyRelative("baseStartHeight").floatValue, 0, 1);
			};

			return list;
		}
	}
}