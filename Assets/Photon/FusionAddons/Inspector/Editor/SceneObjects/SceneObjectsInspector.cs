namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class SceneObjectsInspector
	{
		private Vector2                _scrollPosition;
		private float                  _selectionTime;
		private List<SceneObjectInfo>  _selection = new List<SceneObjectInfo>();
		private UnityEngine.Object     _scriptSelection;
		private float                  _scriptSelectionTime;
		private Vector2                _scriptScrollPosition;
		private MemoryLabel            _totalSelectionChanges = new MemoryLabel(true);
		private HashSet<ComponentInfo> _tempScripts = new HashSet<ComponentInfo>();
		private List<ComponentInfo>    _tempComponents = new List<ComponentInfo>();
		private string[]               _tempRunnerNames;

		private static GUINameStat _objectStat;
		private static GUISortStat _componentsStat;
		private static GUIIconStat _isMasterClientObjectStat;
		private static GUIIconStat _allowStateAuthorityOverrideStat;
		private static GUIIconStat _destroyWhenStateAuthorityLeavesStat;
		private static GUISortStat _interestModeStat;

		private static GUIIconStat _isInSimulationStat;
		private static GUIIconStat _hasStateAuthorityStat;
		private static GUIIconStat _hasInputAuthorityStat;
		private static GUISortStat _networkIdStat;
		private static GUISortStat _stateAuthorityStat;
		private static GUISortStat _inputAuthorityStat;
		private static GUISortStat _totalStateChangesStat;
		private static GUISortStat _averageStateChangesStat;
		private static GUISortStat _stateSizeStat;
		private static GUISortStat _distanceStat;

		private static GUIContent   _objectContent;
		private static GUIContent   _componentsContent;
		private static GUIContent   _componentsIconContent;
		private static GUIContent[] _interestModeContents;
		private static GUIContent   _localStateAuthorityContent;
		private static GUIContent   _localInputAuthorityContent;
		private static GUIContent   _stateSizeContent;
		private static GUIContent   _filterContent;
		private static GUIContent   _fieldsContent;
		private static GUIContent   _scriptContent;

		private static float _sortWidth;
		private static float _fieldsWidth1;
		private static float _fieldsWidth2;
		private static float _fieldsWidth3;

		public SceneObjectsInspector()
		{
			Texture2D gameObjectIcon = EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
			Texture2D scriptIcon     = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D filterIcon     = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D fieldsIcon     = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_objectStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Object", "Name of the object", (int)ESceneObjectSortMode.NameAscending, (int)ESceneObjectSortMode.NameDescending);

			_componentsStat                      = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of NetworkBehaviour components on the object", (int)ESceneObjectSortMode.ComponentsAscending, (int)ESceneObjectSortMode.ComponentsDescending);
			_isMasterClientObjectStat            = new GUIIconStat(GUIStyles.Icon, "❖", "✔", $"Is Master Client Object (Shared Mode)", headerHeight);
			_allowStateAuthorityOverrideStat     = new GUIIconStat(GUIStyles.Icon, "✪", "✔", $"Allow State Authority Override (Shared Mode)", headerHeight);
			_destroyWhenStateAuthorityLeavesStat = new GUIIconStat(GUIStyles.Icon, "☢", "✔", $"Destroy When State Authority Leaves (Shared Mode)", headerHeight);
			_interestModeStat                    = new GUISortStat(GUIStyles.RightBoldLabel, "Interest", "Object Interest Mode", (int)ESceneObjectSortMode.InterestModeAscending, (int)ESceneObjectSortMode.InterestModeDescending);

			_isInSimulationStat      = new GUIIconStat(GUIStyles.Icon, "☀", "✔", $"Is In Simulation", headerHeight);
			_hasStateAuthorityStat   = new GUIIconStat(GUIStyles.Icon, "◈", "✔", $"Has State Authority", headerHeight);
			_hasInputAuthorityStat   = new GUIIconStat(GUIStyles.Icon, "⦿", "✔", $"Has Input Authority", headerHeight);
			_networkIdStat           = new GUISortStat(GUIStyles.RightBoldLabel, "Network Id", "NetworkId of the network object", (int)ESceneObjectSortMode.NetworkIdAscending, (int)ESceneObjectSortMode.NetworkIdDescending);
			_stateAuthorityStat      = new GUISortStat(GUIStyles.RightBoldLabel, "S-Authority", "State authority of the network object (PlayerRef)", (int)ESceneObjectSortMode.StateAuthorityAscending, (int)ESceneObjectSortMode.StateAuthorityDescending);
			_inputAuthorityStat      = new GUISortStat(GUIStyles.RightBoldLabel, "I-Authority", "Input authority of the network object (PlayerRef)", (int)ESceneObjectSortMode.InputAuthorityAscending, (int)ESceneObjectSortMode.InputAuthorityDescending);
			_totalStateChangesStat   = new GUISortStat(GUIStyles.RightBoldLabel, "Σ Changes", "Total changes in networked state in Bytes", (int)ESceneObjectSortMode.TotalStateChangesAscending, (int)ESceneObjectSortMode.TotalStateChangesDescending);
			_averageStateChangesStat = new GUISortStat(GUIStyles.RightBoldLabel, "Ø Changes", "Average changes in networked state in Bytes per second", (int)ESceneObjectSortMode.AverageStateChangesAscending, (int)ESceneObjectSortMode.AverageStateChangesDescending);
			_stateSizeStat           = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state of the object in Bytes", (int)ESceneObjectSortMode.StateSizeAscending, (int)ESceneObjectSortMode.StateSizeDescending, stateSizeWidth);
			_distanceStat            = new GUISortStat(GUIStyles.RightBoldLabel, "Distance", "Distance from Player Object / Camera", (int)ESceneObjectSortMode.DistanceAscending, (int)ESceneObjectSortMode.DistanceDescending);

			_objectContent              = new GUIContent(gameObjectIcon);
			_componentsContent          = new GUIContent("", "# of NetworkBehaviour components on the object");
			_componentsIconContent      = new GUIContent(scriptIcon, "# of NetworkBehaviour components on the object");
			_interestModeContents       = new GUIContent[] { new GUIContent("Area"), new GUIContent("Global"), new GUIContent("Explicit") };
			_localStateAuthorityContent = new GUIContent(" •", "State authority of this object is the local player");
			_localInputAuthorityContent = new GUIContent(" •", "Input authority of this object is the local player");
			_stateSizeContent           = new GUIContent("", "Size of the networked state of the object in Bytes");
			_filterContent              = new GUIContent(filterIcon, "Filter by component type");
			_fieldsContent              = new GUIContent(fieldsIcon, "Visible fields");
			_scriptContent              = new GUIContent(scriptIcon);

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_componentsStat.Width          += _sortWidth;
			_interestModeStat.Width        += _sortWidth;
			_networkIdStat.Width           += _sortWidth;
			_stateAuthorityStat.Width      += _sortWidth;
			_inputAuthorityStat.Width      += _sortWidth;
			_totalStateChangesStat.Width   += _sortWidth;
			_averageStateChangesStat.Width += _sortWidth;
			_stateSizeStat.Width           += _sortWidth;
			_distanceStat.Width            += _sortWidth;

			_fieldsWidth1 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Destroy When State Authority Leaves")).x;
			_fieldsWidth2 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Has State Authority")).x;
			_fieldsWidth3 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Min. Field Fill")).x;
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			DrawRunnersGUI(ref region, db);

			GameMode          gameMode         = db.CurrentRunner != null ? db.CurrentRunner.GameMode : default;
			ESceneObjectField visibleFields    = db.SceneObjects.VisibleFields | ESceneObjectField.Object;
			ESceneObjectField toggleableFields = ~ESceneObjectField.Object;

			bool showRuntimeInfo = gameMode != default;
			if (showRuntimeInfo == false)
			{
				visibleFields &= ~ESceneObjectField.IsInSimulation;
				visibleFields &= ~ESceneObjectField.HasStateAuthority;
				visibleFields &= ~ESceneObjectField.HasInputAuthority;
				visibleFields &= ~ESceneObjectField.NetworkId;
				visibleFields &= ~ESceneObjectField.StateAuthority;
				visibleFields &= ~ESceneObjectField.InputAuthority;
				visibleFields &= ~ESceneObjectField.TotalStateChanges;
				visibleFields &= ~ESceneObjectField.AverageStateChanges;

				toggleableFields &= ~ESceneObjectField.IsInSimulation;
				toggleableFields &= ~ESceneObjectField.HasStateAuthority;
				toggleableFields &= ~ESceneObjectField.HasInputAuthority;
				toggleableFields &= ~ESceneObjectField.NetworkId;
				toggleableFields &= ~ESceneObjectField.StateAuthority;
				toggleableFields &= ~ESceneObjectField.InputAuthority;
				toggleableFields &= ~ESceneObjectField.TotalStateChanges;
				toggleableFields &= ~ESceneObjectField.AverageStateChanges;
			}

			bool showSharedModeInfo = gameMode == default || gameMode == GameMode.Shared;
			if (showSharedModeInfo == false)
			{
				visibleFields &= ~ESceneObjectField.IsMasterClientObject;
				visibleFields &= ~ESceneObjectField.AllowStateAuthorityOverride;
				visibleFields &= ~ESceneObjectField.DestroyWhenStateAuthorityLeaves;

				toggleableFields &= ~ESceneObjectField.IsMasterClientObject;
				toggleableFields &= ~ESceneObjectField.AllowStateAuthorityOverride;
				toggleableFields &= ~ESceneObjectField.DestroyWhenStateAuthorityLeaves;
			}

			DrawSceneObjectsGUI(window, ref region, db, visibleFields, toggleableFields);

			region.yMin += Mathf.Clamp(region.height, 0.0f, 2.0f);

			DrawSelectionGUI(window, ref region, db);
		}

		private void DrawRunnersGUI(ref Rect region, FusionInspectorDB db)
		{
			if (db.Runners.Count < 2)
				return;

			int currentRunnerIndex = db.Runners.IndexOf(db.CurrentRunner);

			if (_tempRunnerNames == null || _tempRunnerNames.Length != db.Runners.Count)
			{
				_tempRunnerNames = new string[db.Runners.Count];
			}

			for (int i = 0; i < db.Runners.Count; ++i)
			{
				_tempRunnerNames[i] = $"ID:[{db.Runners[i].GetInstanceID()}] {db.Runners[i].name}";
			}

			region.yMin += FusionInspector.LinePadding;

			Rect popupRect = region;
			popupRect.xMin += FusionInspector.LinePadding;
			popupRect.xMax -= FusionInspector.LinePadding;
			popupRect.height = FusionInspector.LineHeight;

			int pendingRunnerIndex = EditorGUI.Popup(popupRect, currentRunnerIndex, _tempRunnerNames);
			if (pendingRunnerIndex != currentRunnerIndex)
			{
				db.PendingRunner = db.Runners[pendingRunnerIndex];
			}

			region.yMin += popupRect.height;
			region.yMin += FusionInspector.LinePadding;
		}

		private void DrawSceneObjectsGUI(Rect window, ref Rect region, FusionInspectorDB db, ESceneObjectField visibleFields, ESceneObjectField toggleableFields)
		{
			SceneObjects sceneObjects = db.SceneObjects;
			if (sceneObjects.All.Count <= 0)
				return;

			PlayerRef localPlayer  = db.LocalPlayer;
			int       minSelection = int.MaxValue;
			int       maxSelection = int.MinValue;

			_selection.Clear();
			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];
				if (sceneObject.IsSelected == true)
				{
					_selection.Add(sceneObject);

					minSelection = Mathf.Min(minSelection, i);
					maxSelection = Mathf.Max(maxSelection, i);
				}
			}

			Rect content = region;

			float selectionRegionHeight = Mathf.Clamp(region.height - FusionInspector.HeaderHeight - FusionInspector.LineHeight, 0.0f, region.height * FusionInspector.SelectionSize);
			if (selectionRegionHeight > 0.0f && _selection.Count > 0)
			{
				region.yMin = region.yMax - selectionRegionHeight;
				content.height -= selectionRegionHeight;
			}

			bool showComponents                      = visibleFields.Has(ESceneObjectField.Components);
			bool showIsMasterClientObject            = visibleFields.Has(ESceneObjectField.IsMasterClientObject);
			bool showAllowStateAuthorityOverride     = visibleFields.Has(ESceneObjectField.AllowStateAuthorityOverride);
			bool showDestroyWhenStateAuthorityLeaves = visibleFields.Has(ESceneObjectField.DestroyWhenStateAuthorityLeaves);
			bool showInterestMode                    = visibleFields.Has(ESceneObjectField.InterestMode);

			bool showIsInSimulation                  = visibleFields.Has(ESceneObjectField.IsInSimulation);
			bool showHasStateAuthority               = visibleFields.Has(ESceneObjectField.HasStateAuthority);
			bool showHasInputAuthority               = visibleFields.Has(ESceneObjectField.HasInputAuthority);
			bool showNetworkId                       = visibleFields.Has(ESceneObjectField.NetworkId);
			bool showStateAuthority                  = visibleFields.Has(ESceneObjectField.StateAuthority);
			bool showInputAuthority                  = visibleFields.Has(ESceneObjectField.InputAuthority);
			bool showTotalStateChanges               = visibleFields.Has(ESceneObjectField.TotalStateChanges);
			bool showAverageStateChanges             = visibleFields.Has(ESceneObjectField.AverageStateChanges);
			bool showStateSize                       = visibleFields.Has(ESceneObjectField.StateSize);
			bool showDistance                        = visibleFields.Has(ESceneObjectField.Distance);

			GUISeparators.Clear();

			content.xMin += FusionInspector.LeftWindowOffset;
			content.xMax -= FusionInspector.RightWindowOffset;

			_objectStat.Width = content.width;

			if (showComponents                      == true) { _objectStat.Width -= _componentsStat.Width;                      }
			if (showIsMasterClientObject            == true) { _objectStat.Width -= _isMasterClientObjectStat.Width;            } else { sceneObjects.IsMasterClientObjectFilter            = default; }
			if (showAllowStateAuthorityOverride     == true) { _objectStat.Width -= _allowStateAuthorityOverrideStat.Width;     } else { sceneObjects.AllowStateAuthorityOverrideFilter     = default; }
			if (showDestroyWhenStateAuthorityLeaves == true) { _objectStat.Width -= _destroyWhenStateAuthorityLeavesStat.Width; } else { sceneObjects.DestroyWhenStateAuthorityLeavesFilter = default; }
			if (showInterestMode                    == true) { _objectStat.Width -= _interestModeStat.Width;                    }

			if (showIsInSimulation                  == true) { _objectStat.Width -= _isInSimulationStat.Width;                  } else { sceneObjects.IsInSimulationFilter                  = default; }
			if (showHasStateAuthority               == true) { _objectStat.Width -= _hasStateAuthorityStat.Width;               } else { sceneObjects.HasStateAuthorityFilter               = default; }
			if (showHasInputAuthority               == true) { _objectStat.Width -= _hasInputAuthorityStat.Width;               } else { sceneObjects.HasInputAuthorityFilter               = default; }
			if (showNetworkId                       == true) { _objectStat.Width -= _networkIdStat.Width;                       }
			if (showStateAuthority                  == true) { _objectStat.Width -= _stateAuthorityStat.Width;                  }
			if (showInputAuthority                  == true) { _objectStat.Width -= _inputAuthorityStat.Width;                  }
			if (showTotalStateChanges               == true) { _objectStat.Width -= _totalStateChangesStat.Width;               }
			if (showAverageStateChanges             == true) { _objectStat.Width -= _averageStateChangesStat.Width;             }
			if (showStateSize                       == true) { _objectStat.Width -= _stateSizeStat.Width;                       }
			if (showDistance                        == true) { _objectStat.Width -= _distanceStat.Width;                        }

			Rect headerRect = content;
			headerRect.width  = _objectStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(sceneObjects.ComponentFilterInclude.Count + sceneObjects.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				sceneObjects.ComponentFilterFoldout = !sceneObjects.ComponentFilterFoldout;
				sceneObjects.VisibleFieldsFoldout = false;
			}
			GUIColor.Reset();

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax = componentFilterRect.xMin;
			nameFilterRect.xMin = nameFilterRect.xMax - headerRect.width * 0.4f;
			nameFilterRect.y += 0.5f;
			if (nameFilterRect.height > EditorGUIUtility.singleLineHeight)
			{
				nameFilterRect.y += (nameFilterRect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
				nameFilterRect.height = EditorGUIUtility.singleLineHeight;
			}
			sceneObjects.ObjectNameFilter = EditorGUI.TextField(nameFilterRect, sceneObjects.ObjectNameFilter, GUIStyles.SearchField);

			Rect nameRect = headerRect;
			nameRect.xMax = nameFilterRect.xMin;
			nameRect.xMin += _objectStat.IsSorted((int)sceneObjects.SortMode) ? 0.0f : _sortWidth;
			if (GUI.Button(nameRect, _objectStat.GetContent(sceneObjects.All.Count, sceneObjects.Filtered.Count, _selection.Count, (int)sceneObjects.SortMode), _objectStat.Style) == true)
			{
				ToggleSortMode(ref sceneObjects.SortMode, ESceneObjectSortMode.NameAscending, ESceneObjectSortMode.NameDescending);
			}

			if (showComponents                      == true) { DrawHeader(_componentsStat, _componentsIconContent, ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.ComponentsDescending, ESceneObjectSortMode.ComponentsAscending); }
			if (showIsMasterClientObject            == true) { DrawHeader(_isMasterClientObjectStat,               ref headerRect, ref sceneObjects.IsMasterClientObjectFilter);            }
			if (showAllowStateAuthorityOverride     == true) { DrawHeader(_allowStateAuthorityOverrideStat,        ref headerRect, ref sceneObjects.AllowStateAuthorityOverrideFilter);     }
			if (showDestroyWhenStateAuthorityLeaves == true) { DrawHeader(_destroyWhenStateAuthorityLeavesStat,    ref headerRect, ref sceneObjects.DestroyWhenStateAuthorityLeavesFilter); }
			if (showInterestMode                    == true) { DrawHeader(_interestModeStat, null,                 ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.InterestModeAscending, ESceneObjectSortMode.InterestModeDescending); }

			if (showIsInSimulation                  == true) { DrawHeader(_isInSimulationStat,                     ref headerRect, ref sceneObjects.IsInSimulationFilter);    }
			if (showHasStateAuthority               == true) { DrawHeader(_hasStateAuthorityStat,                  ref headerRect, ref sceneObjects.HasStateAuthorityFilter); }
			if (showHasInputAuthority               == true) { DrawHeader(_hasInputAuthorityStat,                  ref headerRect, ref sceneObjects.HasInputAuthorityFilter); }
			if (showNetworkId                       == true) { DrawHeader(_networkIdStat, null,                    ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.NetworkIdAscending,            ESceneObjectSortMode.NetworkIdDescending);          }
			if (showStateAuthority                  == true) { DrawHeader(_stateAuthorityStat, null,               ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.StateAuthorityAscending,       ESceneObjectSortMode.StateAuthorityDescending);     }
			if (showInputAuthority                  == true) { DrawHeader(_inputAuthorityStat, null,               ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.InputAuthorityAscending,       ESceneObjectSortMode.InputAuthorityDescending);     }
			if (showTotalStateChanges               == true) { DrawHeader(_totalStateChangesStat, null,            ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.TotalStateChangesDescending,   ESceneObjectSortMode.TotalStateChangesAscending);   }
			if (showAverageStateChanges             == true) { DrawHeader(_averageStateChangesStat, null,          ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.AverageStateChangesDescending, ESceneObjectSortMode.AverageStateChangesAscending); }
			if (showStateSize                       == true) { DrawHeader(_stateSizeStat, null,                    ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.StateSizeDescending,           ESceneObjectSortMode.StateSizeAscending);           }
			if (showDistance                        == true) { DrawHeader(_distanceStat, null,                     ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.DistanceAscending,             ESceneObjectSortMode.DistanceDescending);           }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				sceneObjects.VisibleFieldsFoldout = !sceneObjects.VisibleFieldsFoldout;
				sceneObjects.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			content.yMin += headerRect.height;

			DrawFieldsGUI(ref content, sceneObjects, visibleFields, toggleableFields);
			DrawComponentFilterGUI(ref content, sceneObjects, db.Components);

			GUISeparators.AddY(content.y);

			Rect scrollRect     = new Rect(content.x, content.y, content.width + FusionInspector.RightWindowOffset, content.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * sceneObjects.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   minScrollY    = _scrollPosition.y;
			float   maxScrollY    = _scrollPosition.y + content.height;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition = Event.current.mousePosition;

			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}
				else if (drawRect.y > maxScrollY)
				{
					break;
				}

				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _objectStat.Width;

				if (sceneObject.IsSelected == true)
				{
					Rect selectionRect = drawRect;
					selectionRect.xMin = 0.0f;
					selectionRect.xMax = window.width;
					selectionRect.yMin = Mathf.Max(minScrollY, selectionRect.yMin);
					selectionRect.yMax = Mathf.Min(selectionRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(selectionRect, FusionInspector.SelectionColor);

					selectionRect.width = scrollRect.x;
					selectionRect.y += scrollRect.y - _scrollPosition.y;
					GUIHighlights.Add(selectionRect, FusionInspector.SelectionColor);
				}
				else if (mousePosition.x >= content.xMin && mousePosition.x <= content.xMax && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
				{
					Rect hoverRect = drawRect;
					hoverRect.xMin = 0.0f;
					hoverRect.xMax = window.width;
					hoverRect.yMin = Mathf.Max(minScrollY, hoverRect.yMin + 0.25f);
					hoverRect.yMax = Mathf.Min(hoverRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);

					hoverRect.width = scrollRect.x;
					hoverRect.y += scrollRect.y - _scrollPosition.y;
					GUIHighlights.Add(hoverRect, FusionInspector.HoverColor);
				}

				FusionInspector.ApplyLinePadding(ref drawRect);

				GUIColor.Set(sceneObject.IsActive == true ? FusionInspector.ActiveTextColor : FusionInspector.InactiveTextColor);
				_objectContent.text = sceneObject.Name;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
				{
					if (_selection.Count == 1 && _selection.Contains(sceneObject) == true && Time.unscaledTime < (_selectionTime + FusionInspector.SelectionDelay))
					{
						_selectionTime = default;

						Selection.SetActiveObjectWithContext(sceneObject.GameObject, sceneObject.GameObject);
					}
					else
					{
						db.Components.ClearSelectedStateChanges();

						if (Event.current.shift == true)
						{
							minSelection = Mathf.Min(minSelection, i);
							maxSelection = Mathf.Max(maxSelection, i);

							for (int j = minSelection; j <= maxSelection; ++j)
							{
								sceneObjects.Filtered[j].IsSelected = true;
							}

							EditorGUIUtility.PingObject(sceneObject.GameObject);
						}
						else if (Event.current.control == true)
						{
							sceneObject.IsSelected = !sceneObject.IsSelected;
							if (sceneObject.IsSelected == true)
							{
								EditorGUIUtility.PingObject(sceneObject.GameObject);
							}
						}
						else
						{
							for (int j = 0; j < _selection.Count; ++j)
							{
								_selection[j].IsSelected = false;
							}

							if (_selection.Count != 1 || _selection.Contains(sceneObject) == false)
							{
								sceneObject.IsSelected = true;
								EditorGUIUtility.PingObject(sceneObject.GameObject);
							}
						}

						_selectionTime = Time.unscaledTime;
					}
				}
				drawRect.height -= 1.0f; // GUI.Button dead zone fix
				GUIColor.Reset();

				if (showComponents == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _componentsStat.Width;

					if (sceneObject.ComponentCount.Value > 0)
					{
						_componentsContent.text = sceneObject.ComponentCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _componentsContent, GUIStyles.RightLabel) == true)
						{
							if (sceneObject.Components.Length > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int componentIndex = 0; componentIndex < sceneObject.Components.Length; ++componentIndex)
								{
									ComponentInfo component = sceneObject.Components[componentIndex];
									menu.AddItem(new GUIContent($"ID:[{sceneObject.NetworkObject.NetworkedBehaviours[componentIndex].GetInstanceID()}] {component.TypeName}"), false, (script) =>
									{
										MonoScript monoScript = script as MonoScript;
										if (monoScript != null)
										{
											AssetDatabase.OpenAsset(monoScript);
										}
									}, component.Script);
								}
								menu.ShowAsContext();
							}
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showIsMasterClientObject            == true) { FusionInspector.DrawIcon(_isMasterClientObjectStat,            ref drawRect, sceneObject.IsMasterClientObject);            }
				if (showAllowStateAuthorityOverride     == true) { FusionInspector.DrawIcon(_allowStateAuthorityOverrideStat,     ref drawRect, sceneObject.AllowStateAuthorityOverride);     }
				if (showDestroyWhenStateAuthorityLeaves == true) { FusionInspector.DrawIcon(_destroyWhenStateAuthorityLeavesStat, ref drawRect, sceneObject.DestroyWhenStateAuthorityLeaves); }

				if (showInterestMode == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _interestModeStat.Width;

					EditorGUI.LabelField(drawRect, _interestModeContents[(int)sceneObject.InterestMode], GUIStyles.RightLabel);
				}

				if (showIsInSimulation    == true) { FusionInspector.DrawIcon(_isInSimulationStat,    ref drawRect, sceneObject.IsInSimulation);    }
				if (showHasStateAuthority == true) { FusionInspector.DrawIcon(_hasStateAuthorityStat, ref drawRect, sceneObject.HasStateAuthority); }
				if (showHasInputAuthority == true) { FusionInspector.DrawIcon(_hasInputAuthorityStat, ref drawRect, sceneObject.HasInputAuthority); }

				if (showNetworkId == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _networkIdStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.NetworkId.GetLabel(), GUIStyles.RightLabel);
				}

				if (showStateAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateAuthorityStat.Width;

					if (sceneObject.StateAuthority.Value != PlayerRef.None)
					{
						EditorGUI.LabelField(drawRect, sceneObject.StateAuthority.GetLabel(), GUIStyles.RightLabel);
						if (sceneObject.StateAuthority.Value == localPlayer)
						{
							GUIColor.Set(Color.green);
							EditorGUI.LabelField(drawRect, _localStateAuthorityContent, GUIStyles.LeftBoldLabel);
							GUIColor.Reset();
						}
					}
				}

				if (showInputAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _inputAuthorityStat.Width;

					if (sceneObject.InputAuthority.Value != PlayerRef.None)
					{
						EditorGUI.LabelField(drawRect, sceneObject.InputAuthority.GetLabel(), GUIStyles.RightLabel);
						if (sceneObject.InputAuthority.Value == localPlayer)
						{
							GUIColor.Set(Color.green);
							EditorGUI.LabelField(drawRect, _localInputAuthorityContent, GUIStyles.LeftBoldLabel);
							GUIColor.Reset();
						}
					}
				}

				if (showTotalStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _totalStateChangesStat.Width;

					if (sceneObject.TotalStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, sceneObject.TotalStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showAverageStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _averageStateChangesStat.Width;

					if (sceneObject.AverageStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, sceneObject.AverageStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				if (showDistance == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _distanceStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.Distance.GetLabel(), GUIStyles.RightLabel);
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight);

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight);
		}

		private void DrawFieldsGUI(ref Rect sectionRect, SceneObjects sceneObjects, ESceneObjectField visibleFields, ESceneObjectField toggleableFields)
		{
			if (sceneObjects.VisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2 + _fieldsWidth3;
			float widthMultiplier = innerRect.width / totalWidth;

			//----------------------------------------------------------------

			Rect configRect = innerRect;
			configRect.xMin = innerRect.xMin;
			configRect.xMax = innerRect.xMin + _fieldsWidth1 * widthMultiplier;

			configRect.y += yOffset;

			EditorGUI.LabelField(configRect, "Configuration", EditorStyles.boldLabel);
			configRect.y += configRect.height;
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Components (NB)",                     ESceneObjectField.Components);
			DrawToggle(ref configRect, "Is Master Client Object",             ESceneObjectField.IsMasterClientObject);
			DrawToggle(ref configRect, "Allow State Authority Override",      ESceneObjectField.AllowStateAuthorityOverride);
			DrawToggle(ref configRect, "Destroy When State Authority Leaves", ESceneObjectField.DestroyWhenStateAuthorityLeaves);
			DrawToggle(ref configRect, "Object Interest Mode",                ESceneObjectField.InterestMode);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect runtimeRect = innerRect;
			runtimeRect.xMin = configRect.xMax + xOffset;
			runtimeRect.xMax = runtimeRect.xMin + _fieldsWidth2 * widthMultiplier;

			runtimeRect.y += yOffset;

			EditorGUI.LabelField(runtimeRect, "Runtime", EditorStyles.boldLabel);
			runtimeRect.y += runtimeRect.height;
			runtimeRect.y += yOffset;

			DrawToggle(ref runtimeRect, "Is In Simulation",    ESceneObjectField.IsInSimulation);
			DrawToggle(ref runtimeRect, "Has State Authority", ESceneObjectField.HasStateAuthority);
			DrawToggle(ref runtimeRect, "Has Input Authority", ESceneObjectField.HasInputAuthority);
			DrawToggle(ref runtimeRect, "Network Id",          ESceneObjectField.NetworkId);
			DrawToggle(ref runtimeRect, "State Authority",     ESceneObjectField.StateAuthority);
			DrawToggle(ref runtimeRect, "Input Authority",     ESceneObjectField.InputAuthority);
			DrawToggle(ref runtimeRect, "Σ State Changes",     ESceneObjectField.TotalStateChanges);
			DrawToggle(ref runtimeRect, "Ø State Changes",     ESceneObjectField.AverageStateChanges);

			runtimeRect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = runtimeRect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth3 * widthMultiplier;

			otherRect.y += yOffset;

			EditorGUI.LabelField(otherRect, "Other", EditorStyles.boldLabel);
			otherRect.y += otherRect.height;
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "State Size", ESceneObjectField.StateSize);
			DrawToggle(ref otherRect, "Distance",   ESceneObjectField.Distance);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, runtimeRect.yMin, otherRect.yMin);

			return;

			void DrawToggle(ref Rect rect, string label, ESceneObjectField field)
			{
				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = sceneObjects.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(rect, label, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					sceneObjects.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, SceneObjects sceneObjects, Components components)
		{
			sceneObjects.ComponentFilterInclude.Clear();
			sceneObjects.ComponentFilterExclude.Clear();

			if (sceneObjects.ComponentFilterFoldout == false)
			{
				for (int i = 0, count = components.All.Count; i < count; ++i)
				{
					ComponentInfo component = components.All[i];

					if (component.SceneObjects.FilterMode == EFilterMode.Include)
					{
						sceneObjects.ComponentFilterInclude.Add(component.Type);
					}
					else if (component.SceneObjects.FilterMode == EFilterMode.Exclude)
					{
						sceneObjects.ComponentFilterExclude.Add(component.Type);
					}
				}

				return;
			}

			_tempComponents.Clear();
			_tempComponents.AddRange(components.All);

			if (components.SortMode != EComponentSortMode.TypeNameAscending)
			{
				Components.Sort(_tempComponents, EComponentSortMode.TypeNameAscending);
			}

			float rowWidth = Mathf.Clamp(sectionRect.width, 160.0f, 224.0f);
			int   columns  = Mathf.Max(1, Mathf.FloorToInt(sectionRect.width / rowWidth));
			int   rows     = _tempComponents.Count / columns;

			if (_tempComponents.Count > (rows * columns))
			{
				++rows;
			}

			float xOffset    = 3.0f;
			float yOffset    = 6.0f;
			float lineHeight = EditorGUIUtility.singleLineHeight;

			Rect filterRect = sectionRect;
			filterRect.height = rows * lineHeight + yOffset * 2;
			sectionRect.yMin += filterRect.height;

			filterRect.xMin += xOffset;
			filterRect.xMax -= xOffset;
			filterRect.yMin += yOffset;
			filterRect.yMax -= yOffset;

			for (int i = 0, count = _tempComponents.Count; i < count; ++i)
			{
				int column = i / rows;
				int row    = i - rows * column;

				ComponentInfo component = _tempComponents[i];

				Rect componentRect = filterRect;
				componentRect.width /= columns;
				componentRect.height = lineHeight;
				componentRect.x += componentRect.width * column;
				componentRect.y += componentRect.height * row;

				ComponentInfo.ObjectContext context = component.SceneObjects;

				if (EditorGUI.ToggleLeft(componentRect, context.ObjectFilter.GetLabel(component.TypeName, context.AllObjectCount.Value, context.FilteredObjectCount.Value), false, context.FilteredObjectCount.Value > 0 ? GUIStyles.ActiveToggle : GUIStyles.InactiveToggle) == true)
				{
					context.FilterMode = (EFilterMode)(((int)context.FilterMode + 1) % FusionInspector.FILTER_MODES);
				}

				if (context.FilterMode == EFilterMode.Include)
				{
					sceneObjects.ComponentFilterInclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (context.FilterMode == EFilterMode.Exclude)
				{
					sceneObjects.ComponentFilterExclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private void DrawSelectionGUI(Rect window, ref Rect region, FusionInspectorDB db)
		{
			SceneObjects sceneObjects = db.SceneObjects;
			if (sceneObjects.All.Count <= 0)
				return;

			_tempScripts.Clear();
			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];
				if (sceneObject.IsSelected == true)
				{
					for (int j = 0, countJ = sceneObject.Components.Length; j < countJ; ++j)
					{
						_tempScripts.Add(sceneObject.Components[j]);
					}
				}
			}

			if (_tempScripts.Count <= 0)
				return;

			Rect content = region;
			content.xMax = content.xMin + FusionInspector.LeftWindowOffset + _objectStat.Width;

			GUISeparators.Clear();
			GUISeparators.AddX(content.xMax);

			float widthOffset = 0.0f;
			int   scrollViewItems = _tempScripts.Count;

			bool isInPlayMode = Application.isPlaying;
			if (isInPlayMode == true)
			{
				++scrollViewItems;
			}

			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * scrollViewItems);
			if (scrollViewRect.height > (content.height - FusionInspector.HeaderHeight - 1.0f))
			{
				widthOffset = FusionInspector.RightWindowOffset;
			}

			scrollViewRect.width -= widthOffset;

			Rect headerRect = content;
			headerRect.xMin += FusionInspector.LeftWindowOffset;
			headerRect.width  = _objectStat.Width - widthOffset;
			headerRect.height = FusionInspector.HeaderHeight;

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, content.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect typeRect = headerRect;
			typeRect.xMin += _sortWidth;
			GUI.Label(typeRect, "Type", _objectStat.Style);

			if (isInPlayMode == true)
			{
				GUI.Label(typeRect, "Σ Changes", _totalStateChangesStat.Style);
			}
			else
			{
				GUI.Label(typeRect, "State", _stateSizeStat.Style);
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			content.yMin += headerRect.height + 0.5f;
			content.yMax -= 0.5f;

			_scriptScrollPosition = GUI.BeginScrollView(content, _scriptScrollPosition, scrollViewRect);

			float minScrollY = _scriptScrollPosition.y;
			float maxScrollY = _scriptScrollPosition.y + content.height;
			Rect  drawRect   = new Rect(FusionInspector.LeftWindowOffset, 0.0f, _objectStat.Width - widthOffset, FusionInspector.LineHeight);

			if (isInPlayMode == true)
			{
				_totalSelectionChanges.Value = 0;

				foreach (ComponentInfo component in _tempScripts)
				{
					_totalSelectionChanges.Value += component.SelectedStateChanges.Value;
				}

				GUI.Label(new Rect(drawRect.x + 18.0f, drawRect.y, drawRect.width, drawRect.height), "Total", GUIStyles.ObjectField);
				GUI.Label(drawRect, _totalSelectionChanges.GetLabel(), GUIStyles.RightLabel);
				drawRect.y += drawRect.height;
			}

			foreach (ComponentInfo component in _tempScripts)
			{
				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}
				else if (drawRect.y > maxScrollY)
				{
					break;
				}

				_scriptContent.text = component.TypeName;
				if (GUI.Button(drawRect, _scriptContent, GUIStyles.ObjectField) == true && component.Script != null)
				{
					if (_scriptSelection == component.Script && Time.unscaledTime < (_scriptSelectionTime + FusionInspector.SelectionDelay))
					{
						AssetDatabase.OpenAsset(component.Script);
						_scriptSelection     = default;
						_scriptSelectionTime = default;
					}
					else
					{
						EditorGUIUtility.PingObject(component.Script);
						_scriptSelection     = component.Script;
						_scriptSelectionTime = Time.unscaledTime;
					}
				}

				if (isInPlayMode == true)
				{
					GUI.Label(drawRect, component.SelectedStateChanges.GetLabel(), GUIStyles.RightLabel);
				}
				else
				{
					GUI.Label(drawRect, component.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, content.width, region.height), headerRect.yMin, headerRect.height, headerRect.yMax, region.height - headerRect.height);
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref ESceneObjectSortMode sortMode, ESceneObjectSortMode primarySortMode, ESceneObjectSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (ESceneObjectSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref ESceneObjectSortMode sortMode, ESceneObjectSortMode primaryMode, ESceneObjectSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
