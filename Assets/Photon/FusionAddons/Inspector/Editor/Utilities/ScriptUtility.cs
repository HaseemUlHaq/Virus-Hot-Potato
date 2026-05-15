namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Reflection;
	using UnityEditor;

	internal static class ScriptUtility
	{
		public static int GetLineNumber(MonoScript script, MethodInfo method)
		{
			string[] lines = script.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

			string keyword1 = method.Name;
			string keyword2 = "";
			string keyword3 = "";

			     if (method.IsPublic   == true) { keyword2 = "public";    }
			else if (method.IsPrivate  == true) { keyword2 = "private";   }
			else if (method.IsFamily   == true) { keyword2 = "protected"; }
			else if (method.IsAssembly == true) { keyword2 = "internal";  }

			if (method.IsStatic   == true)                       { keyword3 = "static";   }
			if (method.IsAbstract == true)                       { keyword3 = "abstract"; }
			if (method.IsVirtual  == true && !method.IsAbstract) { keyword3 = "virtual";  }

			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i];
				if (line.Contains(keyword1) == true && line.Contains(keyword2) == true && line.Contains(keyword3) == true)
					return i + 1;
			}

			return -1;
		}
	}
}
