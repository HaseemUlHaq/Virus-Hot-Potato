namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UIElements;
	using UnityEditor;
	using UnityEditor.Overlays;
	using UnityEditor.Toolbars;

	[Overlay(typeof(SceneView), "Fusion Inspector")]
	public class FusionInspectorToolbar : ToolbarOverlay
	{
		FusionInspectorToolbar() : base(OpenInspectorButton.ID)
		{
		}
	}

	[EditorToolbarElement(ID, typeof(SceneView))]
	public class OpenInspectorButton : EditorToolbarButton
	{
		public const string ID = nameof(FusionInspectorToolbar) + "." + nameof(OpenInspectorButton);

		public OpenInspectorButton()
		{
			icon    = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Photon/FusionAddons/Inspector/Editor/EditorResources/PhotonCircle.png");
			tooltip = "Open Fusion Inspector.";

			clicked -= OnClick;
			clicked += OnClick;
		}

		private void OnClick()
		{
			FusionInspector.Create();
		}
	}

	[EditorToolbarElement(ID, typeof(SceneView))]
	public class RTTLabel : TextElement
	{
		public const string ID = nameof(FusionInspectorToolbar) + "." + nameof(RTTLabel);

		public RTTLabel()
		{
			style.marginLeft = 6;
			style.marginRight = 6;
			style.unityTextAlign = TextAnchor.MiddleLeft;

			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;
		}

		~RTTLabel()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			if (Application.isPlaying == false)
			{
				text = "";
				return;
			}

			List<NetworkRunner>.Enumerator runnersEnumerator = NetworkRunner.GetInstancesEnumerator();
			while (runnersEnumerator.MoveNext() == true)
			{
				NetworkRunner runner = runnersEnumerator.Current;
				if (runner != null && runner.IsRunning == true)
				{
					int rttMs = (int)(runner.GetPlayerRtt(runner.LocalPlayer) * 1000.0);
					text = $"{rttMs}ms";

					if (rttMs < 50)
					{
						style.color = Color.green;
					}
					else if (rttMs < 100)
					{
						style.color = Color.yellow;
					}
					else
					{
						style.color = Color.red;
					}
				}
			}
		}
	}
}
