namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Reflection;
	using UnityEngine;
	using UnityEditor;

	internal static class TypeUtility
	{
		public static string GetTypeName(Type type)
		{
			string typeName = type.Name;
			if (type.IsGenericType == false)
				return typeName;

			int backtickIndex = typeName.IndexOf('`');
			if (backtickIndex >= 0)
			{
				typeName = typeName.Substring(0, backtickIndex);
			}

			string[] argumentTypes = new string[type.GenericTypeArguments.Length];
			for (int i = 0; i < argumentTypes.Length; ++i)
			{
				argumentTypes[i] = GetTypeName(type.GenericTypeArguments[i]);
			}

			return $"{typeName}<{string.Join(",", argumentTypes)}>";
		}


		public static int GetExecutionOrder(Type type, MonoScript script)
		{
			int executionOrder = script != null ? MonoImporter.GetExecutionOrder(script) : default;
			if (executionOrder == default)
			{
				DefaultExecutionOrder attribute = type.GetCustomAttribute<DefaultExecutionOrder>();
				executionOrder = attribute != null ? attribute.order : default;
			}

			return executionOrder;
		}
	}
}
