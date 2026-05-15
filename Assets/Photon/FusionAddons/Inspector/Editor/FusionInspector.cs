namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Reflection;
	using Unity.Profiling;
	using UnityEngine;
	using UnityEditor;

	public sealed partial class FusionInspector : EditorWindow
	{
		public const int FILTER_MODES       = 3;
		public const int STATE_SIZE_UNKNOWN = -999;

		public static readonly float   LeftWindowOffset  = 8.0f;
		public static readonly float   RightWindowOffset = 16.0f;
		public static readonly float   LineHeight        = EditorGUIUtility.singleLineHeight + 1.0f;
		public static readonly float   LinePadding       = 1.0f;
		public static readonly float   FieldPadding      = 40.0f;
		public static readonly float   HeaderHeight      = LineHeight + 3.0f;
		public static readonly float   FooterHeight      = LineHeight + 3.0f;
		public static readonly Color   HeaderColor       = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		public static readonly Color   HoverColor        = new Color(0.25f, 0.25f, 0.25f, 1.0f);
		public static readonly Color   SelectionColor    = new Color(0.24f, 0.377f, 0.569f, 1.0f);
		public static readonly Color   BackgroundColor   = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		public static readonly Color   NormalTextColor   = new Color(0.92f, 0.92f, 0.92f, 1.0f);
		public static readonly Color   ActiveTextColor   = Color.white;
		public static readonly Color   InactiveTextColor = Color.grey;
		public static readonly float   SeparatorSize     = 1.5f;
		public static readonly Color   SeparatorColor    = new Color(0.175f, 0.175f, 0.175f, 1.0f);
		public static readonly Color[] FilterColors      = new Color[] { Color.white, Color.green, Color.red };
		public static readonly float   SelectionSize     = 0.333f;
		public static readonly float   SelectionDelay    = 0.35f;

		private FusionInspectorDB     _db;
		private GUIContent[]          _sections;
		private ComponentsInspector   _componentsInspector;
		private SceneObjectsInspector _sceneObjectsInspector;
		private PrefabsInspector      _prefabsInspector;
		private Assembly              _assembly;
		private string                _version;

		private static GUIIntStat     _rttStat;
		private static GUIIntStat     _tickRateStat;
		private static GUITrafficStat _trafficInStat;
		private static GUITrafficStat _trafficOutStat;
		private static GUIIntStat     _playerCountStat;
		private static GUITimeStat    _sessionTimeStat;
		private static GUITimeStat    _captureTimeStat;
		private static GUIStat[]      _gameModeStats;
		private static ProfilerMarker _profilerMarker = new ProfilerMarker($"{nameof(FusionInspector)}.DrawGUI");

		[MenuItem("Tools/Fusion/Inspector")]
		public static void Create()
		{
#if FUSION_INSPECTOR_DOCKED
			CreateWindow<FusionInspector>("Fusion Inspector");
#else
			FusionInspector inspector = EditorWindow.GetWindow<FusionInspector>(true, "Fusion Inspector");
			inspector.ShowUtility();
#endif
		}

		public void OnEnable()
		{
			minSize = new Vector2(256.0f, 128.0f);
		}

		internal static void ApplyLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin += padding;
			rect.yMax -= padding;
		}

		internal static void RestoreLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin -= padding;
			rect.yMax += padding;
		}

		internal static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			GUIColor.Set(FilterColors[(int)filter]);
			if (GUI.Button(rect, stat.Content, stat.Style) == true)
			{
				filter = (EFilterMode)(((int)filter + 1) % FILTER_MODES);
			}
			GUIColor.Reset();
		}

		internal static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref int sortMode, int primarySortMode, int secondarySortMode)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			if (GUI.Button(rect, stat.GetContent(sortMode), stat.Style) == true)
			{
				sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
			}

			if (icon != null)
			{
				ApplyLinePadding(ref rect, 2.0f);

				if (GUI.Button(rect, icon, stat.Style) == true)
				{
					sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
				}

				RestoreLinePadding(ref rect, 2.0f);
			}
		}

		internal static void DrawIcon(GUIIconStat stat, ref Rect rect, bool isOn)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			if (isOn == true)
			{
				EditorGUI.LabelField(rect, stat.IconContent, GUIStyles.Icon);
			}
		}

		private void OnGUI()
		{
			if (Event.current.type == EventType.Layout)
				return;

			Initialize();

			Rect drawRect = new Rect(default, position.size);

			TextureUtility.DrawTexture(drawRect, BackgroundColor);

			float buttonHeight = EditorStyles.toolbarButton.fixedHeight;
			float buttonWidth  = drawRect.width / _sections.Length;

			for (int i = 0, count = _sections.Length; i < count; ++i)
			{
				if (GUI.Toggle(new Rect(drawRect.x + buttonWidth * i, drawRect.y, buttonWidth, buttonHeight), _db.CurrentSection == i, _sections[i], EditorStyles.toolbarButton) == true)
				{
					if (_db.CurrentSection != i)
					{
						_db.SetCurrentSection(i);
					}
				}
			}

			_db.Update();

			_profilerMarker.Begin();

			drawRect.yMin += buttonHeight;
			drawRect.yMax -= FooterHeight;

			if (_db.CurrentSection == 0)
			{
				if (_sceneObjectsInspector == null)
				{
					_sceneObjectsInspector = new SceneObjectsInspector();
				}

				_sceneObjectsInspector.DrawGUI(position, drawRect, _db);
			}
			else if (_db.CurrentSection == 1)
			{
				if (_prefabsInspector == null)
				{
					_prefabsInspector = new PrefabsInspector();
				}

				_prefabsInspector.DrawGUI(position, drawRect, _db);
			}
			else if (_db.CurrentSection == 2)
			{
				if (_componentsInspector == null)
				{
					_componentsInspector = new ComponentsInspector();
				}

				_componentsInspector.DrawGUI(position, drawRect, _db);
			}

			drawRect.y += drawRect.height;
			drawRect.height = FooterHeight;

			DrawFooterGUI(drawRect, Application.isPlaying == true && _db.CurrentRunner != null && _db.CurrentRunner.IsRunning == true);

			_profilerMarker.End();

			Repaint();
		}

		private void DrawFooterGUI(Rect rect, bool hasRunningSession)
		{
			const float spacing = 16.0f;

			ApplyLinePadding(ref rect);

			Rect footerRect = rect;
			footerRect.xMin += 8.0f;
			footerRect.xMax -= 8.0f;

			//----------------------------------------------------------------

			Rect versionRect = footerRect;
			versionRect.width = 112.0f + spacing;
			GUI.Label(versionRect, _version);
			footerRect.xMin += versionRect.width;

			//----------------------------------------------------------------

			if (hasRunningSession == true)
			{
				if (_db.CurrentRunner.GameMode != default)
				{
					GUIStat gameModeStat = _gameModeStats[(int)_db.CurrentRunner.GameMode];
					Rect gameModeRect = footerRect;
					gameModeRect.width = gameModeStat.Width + spacing;
					GUI.Label(gameModeRect, gameModeStat.Content, gameModeStat.Style);
					footerRect.xMin += gameModeRect.width;
				}

				//----------------------------------------------------------------

				try
				{
					Rect tickRateRect = footerRect;
					tickRateRect.width = _tickRateStat.Width + spacing;
					GUI.Label(tickRateRect, _tickRateStat.GetContent(_db.CurrentRunner.TickRate));
					footerRect.xMin += tickRateRect.width;
				}
				catch {}

				//----------------------------------------------------------------

				if (_db.PlayerCount > 0)
				{
					Rect playerCountRect = footerRect;
					playerCountRect.width = _playerCountStat.Width + spacing;
					GUI.Label(playerCountRect, _playerCountStat.GetContent(_db.PlayerCount));
					footerRect.xMin += playerCountRect.width;
				}

				//----------------------------------------------------------------

				Rect sessionTimeRect = footerRect;
				sessionTimeRect.width = _sessionTimeStat.Width + spacing;
				GUI.Label(sessionTimeRect, _sessionTimeStat.GetContent(_db.SessionTime));
				footerRect.xMin += sessionTimeRect.width;

				//----------------------------------------------------------------

				Rect captureTimeRect = footerRect;
				captureTimeRect.width = _captureTimeStat.Width + spacing;
				GUI.Label(captureTimeRect, _captureTimeStat.GetContent(_db.CaptureTime));
				footerRect.xMin += captureTimeRect.width;

				//----------------------------------------------------------------

				if (_db.SmoothRTTMs > 0)
				{
					Rect rttRect = footerRect;
					rttRect.width = _rttStat.Width + spacing;
					GUI.Label(rttRect, _rttStat.GetContent(_db.SmoothRTTMs));
					footerRect.xMin += rttRect.width;
				}

				//----------------------------------------------------------------

				if (_db.InBandwidth > 0.0f)
				{
					Rect trafficInRect = footerRect;
					trafficInRect.width = _trafficInStat.Width + spacing;
					GUI.Label(trafficInRect, _trafficInStat.GetContent(_db.InBandwidth));
					footerRect.xMin += trafficInRect.width;
				}

				//----------------------------------------------------------------

				if (_db.OutBandwidth > 0.0f)
				{
					Rect trafficOutRect = footerRect;
					trafficOutRect.width = _trafficOutStat.Width + spacing;
					GUI.Label(trafficOutRect, _trafficOutStat.GetContent(_db.OutBandwidth));
					footerRect.xMin += trafficOutRect.width;
				}

				//----------------------------------------------------------------

				Rect resetRect = new Rect(footerRect.xMax - 64.0f, footerRect.y, 64.0f, footerRect.height);
				if (GUI.Button(resetRect, "Reset") == true)
				{
					_db.ResetSession(true);
				}
			}

			RestoreLinePadding(ref rect);

			TextureUtility.DrawTexture(new Rect(0.0f, rect.yMin - SeparatorSize * 0.5f, position.width, SeparatorSize), SeparatorColor);
		}

		private void Initialize()
		{
			if (_db == null)
			{
				_db = ScriptableObject.CreateInstance<FusionInspectorDB>();
				_db.hideFlags = HideFlags.HideAndDontSave;

				UnityEngine.Object.DontDestroyOnLoad(_db);
			}

			if (_sections == null)
			{
				_sections = new GUIContent[]
				{
					new GUIContent(" Scene Objects", EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D),
					new GUIContent(" Prefabs",       EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D),
					new GUIContent(" Components",    EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D),
				};
			}

			if (_gameModeStats == null)
			{
				GUIStat empty = new GUIStat(GUIStyles.LeftLabel, "", "");

				_gameModeStats = new GUIStat[] { empty, empty, empty, empty, empty, empty, empty, empty };
				_gameModeStats[(int)GameMode.Single]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Single)}", "");
				_gameModeStats[(int)GameMode.Shared]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Shared)}", "");
				_gameModeStats[(int)GameMode.Server]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Server)}", "");
				_gameModeStats[(int)GameMode.Host]             = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Host)}", "");
				_gameModeStats[(int)GameMode.Client]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Client)}", "");
				_gameModeStats[(int)GameMode.AutoHostOrClient] = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.AutoHostOrClient)}", "");

				_rttStat         = new GUIIntStat(GUIStyles.LeftLabel, "RTT: {0}ms", 888, "Round Trip Time");
				_tickRateStat    = new GUIIntStat(GUIStyles.LeftLabel, "{0} Hz", 88, "Tick Rate");
				_trafficInStat   = new GUITrafficStat(GUIStyles.LeftLabel, "In: {0}", 888888888, "Traffic In");
				_trafficOutStat  = new GUITrafficStat(GUIStyles.LeftLabel, "Out: {0}", 888888888, "Traffic Out");
				_playerCountStat = new GUIIntStat(GUIStyles.LeftLabel, "Players: {0}", 8, "Active Player Count");
				_sessionTimeStat = new GUITimeStat(GUIStyles.LeftLabel, "Session: {0}", TimeSpan.FromHours(8888), "Session Time");
				_captureTimeStat = new GUITimeStat(GUIStyles.LeftLabel, "Capture: {0}", TimeSpan.FromHours(8888), "Capture Time");
			}

			Assembly assembly = typeof(NetworkObject).Assembly;
			if (_assembly != assembly)
			{
				_assembly = assembly;

				string assemblyFileVersion   = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
				string assemblyConfiguration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

				if (string.IsNullOrEmpty(assemblyFileVersion)   == true) { assemblyFileVersion   = "0.0.0"; }
				if (string.IsNullOrEmpty(assemblyConfiguration) == true) { assemblyConfiguration = "";      }

				assemblyConfiguration = assemblyConfiguration.Replace("Unity ", "");

				_version = $"{assemblyConfiguration} {assemblyFileVersion}";
			}
		}
	}
}
