namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Text;

	internal static class ReflectionUtility
	{
		public static MethodInfo[] GetRPCs(Type type)
		{
			List<MethodInfo> rpcs = new List<MethodInfo>();

			foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				if (method.GetCustomAttribute<RpcAttribute>() != null)
				{
					rpcs.Add(method);
				}
			}

			return rpcs.ToArray();
		}

		public static bool HasMethodOverride(Type type, string methodName)
		{
			MethodInfo method = type.GetMethod(methodName);
			return method != null && method.DeclaringType == type;
		}

		public static bool HasInterfaceImplementation(Type type, Type interfaceType)
		{
			foreach (Type implementedInterface in type.GetInterfaces())
			{
				if (implementedInterface == interfaceType)
					return true;
			}

			return false;
		}

		public static string GetMethodDeclaration(MethodInfo method)
		{
			StringBuilder sb = new StringBuilder();

			     if (method.IsPublic   == true) { sb.Append("public ");    }
			else if (method.IsPrivate  == true) { sb.Append("private ");   }
			else if (method.IsFamily   == true) { sb.Append("protected "); }
			else if (method.IsAssembly == true) { sb.Append("internal ");  }

			if (method.IsStatic   == true)                       { sb.Append("static ");   }
			if (method.IsAbstract == true)                       { sb.Append("abstract "); }
			if (method.IsVirtual  == true && !method.IsAbstract) { sb.Append("virtual ");  }

			string returnType = TypeUtility.GetTypeName(method.ReturnType);
			if (returnType == "Void")
			{
				returnType = "void";
			}
			sb.Append(returnType + " ");

			sb.Append(method.Name);

			sb.Append("(");

			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; ++i)
			{
				ParameterInfo parameterInfo = parameters[i];

				sb.Append($"{TypeUtility.GetTypeName(parameterInfo.ParameterType)} {parameterInfo.Name}");

				if (i < parameters.Length - 1)
				{
					sb.Append(", ");
				}
			}

			sb.Append(");");

			return sb.ToString();
		}
	}
}
