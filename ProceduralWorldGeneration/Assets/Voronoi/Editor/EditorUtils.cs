using System;
using System.Reflection;
using UnityEditor;

namespace AtomosZ.EditorTools
{
	public class EditorUtils
	{
		static MethodInfo _clearConsoleMethod;
		static MethodInfo clearConsoleMethod
		{
			get
			{
				if (_clearConsoleMethod == null)
				{
					Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
					Type logEntries = assembly.GetType("UnityEditor.LogEntries");
					_clearConsoleMethod = logEntries.GetMethod("Clear");
				}
				return _clearConsoleMethod;
			}
		}

		public static void ClearLogConsole()
		{
			clearConsoleMethod.Invoke(new object(), null);
		}
	}
}