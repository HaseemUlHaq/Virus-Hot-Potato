namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class PrefabsInspector
	{
		private Vector2                _scrollPosition;
		private float                  _selectionTime;
		private List<PrefabInfo>       _selection = new List<PrefabInfo>();
		private UnityEngine.Object     _scriptSelection;
		private float                  _scriptSelectionTime;
		private Vector2                _scriptScrollPosition;
		private HashSet<ComponentInfo> _tempScripts = new HashSet<ComponentInfo>();
		private List<ComponentInfo>    _tempComponents = new List<ComponentInfo>();

		private static GUINameStat _objectStat;
		private static GUISortStat _componentsStat;
		private static GUIIconStat _isSpawnableStat;
		private static GUIIconStat _isMasterClientObjectStat;
		private static GUIIconStat _allowStateAuthorityOverrideStat;
		private static GUIIconStat _destroyWhenStateAuthorityLeavesStat;
		private static GUISortStat _interestModeStat;
		private static GUISortStat _stateSizeStat;

		private static GUIContent   _objectContent;
		private static GUIContent   _componentsContent;
		private static GUIContent   _componentsIconContent;
		private static GUIContent[] _interestModeContents;
		private static GUIContent   _stateSizeContent;
		private static GUIContent   _filterContent;
		private static GUIContent   _fieldsContent;
		private static GUIContent   _scriptContent;

		private static float _sortWidth;
		private static float _fieldsWidth1;
		private static float _fieldsWidth2;
		private static float _fieldsWidth3;

		public PrefabsInspector()
		{
			Texture2D prefabIcon = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
			Texture2D scriptIcon = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D filterIcon = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D fieldsIcon = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_objectStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Object", "Name of the object", (int)EPrefabSortMode.NameAscending, (int)EPrefabSortMode.NameDescending);

			_componentsStat                      = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of NetworkBehaviour components on the object", (int)EPrefabSortMode.ComponentsAscending, (int)EPrefabSortMode.ComponentsDescending);
			_isSpawnableStat                     = new GUIIconStat(GUIStyles.Icon, "✿", "✔", $"Is Spawnable", headerHeight);
			_isMasterClientObjectStat            = new GUIIconStat(GUIStyles.Icon, "❖", "✔", $"Is Master Client Object (Shared Mode)", headerHeight);
			_allowStateAuthorityOverrideStat     = new GUIIconStat(GUIStyles.Icon, "✪", "✔", $"Allow State Authority Override (Shared Mode)", headerHeight);
			_destroyWhenStateAuthorityLeavesStat = new GUIIconStat(GUIStyles.Icon, "☢", "✔", $"Destroy When State Authority Leaves (Shared Mode)", headerHeight);
			_interestModeStat                    = new GUISortStat(GUIStyles.RightBoldLabel, "Interest", "Object Interest Mode", (int)EPrefabSortMode.InterestModeAscending, (int)EPrefabSortMode.InterestModeDescending);
			_stateSizeStat                       = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state of the object in Bytes", (int)EPrefabSortMode.StateSizeAscending, (int)EPrefabSortMode.StateSizeDescending, stateSizeWidth);

			_objectContent         = new GUIContent(prefabIcon);
			_componentsContent     = new GUIContent("", "# of NetworkBehaviour components on the object");
			_componentsIconContent = new GUIContent(scriptIcon, "# of NetworkBehaviour components on the object");
			_interestModeContents  = new GUIContent[] { new GUIContent("Area"), new GUIContent("Global"), new GUIContent("Explicit") };
			_stateSizeContent      = new GUIContent("", "Size of the networked state of the object in Bytes");
			_filterContent         = new GUIContent(filterIcon, "Filter by component type");
			_fieldsContent         = new GUIContent(fieldsIcon, "Visible fields");
			_scriptContent         = new GUIContent(scriptIcon);

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_componentsStat.Width   += _sortWidth;
			_interestModeStat.Width += _sortWidth;
			_stateSizeStat.Width    += _sortWidth;

			_fieldsWidth1 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Destroy When State Authority Leaves")).x;
			_fieldsWidth2 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Min. Field Fill")).x;
			_fieldsWidth3 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Min. Field Fill")).x;
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			EPrefabField visibleFields    = db.Prefabs.VisibleFields | EPrefabField.Object;
			EPrefabField toggleableFields = ~EPrefabField.Object;

			db.Prefabs.VisibleFields = visibleFields;

			DrawPrefabsGUI(window, ref region, db, visibleFields, toggleableFields);

			region.yMin += Mathf.Clamp(region.height, 0.0f, 2.0f);

			DrawSelectionGUI(window, ref region, db);
		}

		private void DrawPrefabsGUI(Rect window, ref Rect region, FusionInspectorDB db, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			Prefabs prefabs = db.Prefabs;
			if (prefabs.All.Count <= 0)
				return;

			int minSelection = int.MaxValue;
			int maxSelection = int.MinValue;

			_selection.Clear();
			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				PrefabInfo prefab = prefabs.Filtered[i];
				if (prefab.IsSelected == true)
				{
					_selection.Add(prefab);

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

			bool showComponents                      = visibleFields.Has(EPrefabField.Components);
			bool showIsSpawnable                     = visibleFields.Has(EPrefabField.IsSpawnable);
			bool showIsMasterClientObject            = visibleFields.Has(EPrefabField.IsMasterClientObject);
			bool showAllowStateAuthorityOverride     = visibleFields.Has(EPrefabField.AllowStateAuthorityOverride);
			bool showDestroyWhenStateAuthorityLeaves = visibleFields.Has(EPrefabField.DestroyWhenStateAuthorityLeaves);
			bool showInterestMode                    = visibleFields.Has(EPrefabField.InterestMode);
			bool showStateSize                       = visibleFields.Has(EPrefabField.StateSize);

			GUISeparators.Clear();

			content.xMin += FusionInspector.LeftWindowOffset;
			content.xMax -= FusionInspector.RightWindowOffset;

			_objectStat.Width = content.width;

			if (showComponents                      == true) { _objectStat.Width -= _componentsStat.Width;                      }
			if (showIsSpawnable                     == true) { _objectStat.Width -= _isSpawnableStat.Width;                     } else { prefabs.IsSpawnableFilter                     = default; }
			if (showIsMasterClientObject            == true) { _objectStat.Width -= _isMasterClientObjectStat.Width;            } else { prefabs.IsMasterClientObjectFilter            = default; }
			if (showAllowStateAuthorityOverride     == true) { _objectStat.Width -= _allowStateAuthorityOverrideStat.Width;     } else { prefabs.AllowStateAuthorityOverrideFilter     = default; }
			if (showDestroyWhenStateAuthorityLeaves == true) { _objectStat.Width -= _destroyWhenStateAuthorityLeavesStat.Width; } else { prefabs.DestroyWhenStateAuthorityLeavesFilter = default; }
			if (showInterestMode                    == true) { _objectStat.Width -= _interestModeStat.Width;                    }
			if (showStateSize                       == true) { _objectStat.Width -= _stateSizeStat.Width;                       }

			Rect headerRect = content;
			headerRect.width  = _objectStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(prefabs.ComponentFilterInclude.Count + prefabs.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				prefabs.ComponentFilterFoldout = !prefabs.ComponentFilterFoldout;
				prefabs.VisibleFieldsFoldout = false;
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
			prefabs.ObjectNameFilter = EditorGUI.TextField(nameFilterRect, prefabs.ObjectNameFilter, GUIStyles.SearchField);

			Rect nameRect = headerRect;
			nameRect.xMax = nameFilterRect.xMin;
			nameRect.xMin += _objectStat.IsSorted((int)prefabs.SortMode) ? 0.0f : _sortWidth;
			if (GUI.Button(nameRect, _objectStat.GetContent(prefabs.All.Count, prefabs.Filtered.Count, _selection.Count, (int)prefabs.SortMode), _objectStat.Style) == true)
			{
				ToggleSortMode(ref prefabs.SortMode, EPrefabSortMode.NameAscending, EPrefabSortMode.NameDescending);
			}

			if (showComponents                      == true) { DrawHeader(_componentsStat, _componentsIconContent, ref headerRect, ref prefabs.SortMode, EPrefabSortMode.ComponentsDescending, EPrefabSortMode.ComponentsAscending); }
			if (showIsSpawnable                     == true) { DrawHeader(_isSpawnableStat,                        ref headerRect, ref prefabs.IsSpawnableFilter);                     }
			if (showIsMasterClientObject            == true) { DrawHeader(_isMasterClientObjectStat,               ref headerRect, ref prefabs.IsMasterClientObjectFilter);            }
			if (showAllowStateAuthorityOverride     == true) { DrawHeader(_allowStateAuthorityOverrideStat,        ref headerRect, ref prefabs.AllowStateAuthorityOverrideFilter);     }
			if (showDestroyWhenStateAuthorityLeaves == true) { DrawHeader(_destroyWhenStateAuthorityLeavesStat,    ref headerRect, ref prefabs.DestroyWhenStateAuthorityLeavesFilter); }
			if (showInterestMode                    == true) { DrawHeader(_interestModeStat, null,                 ref headerRect, ref prefabs.SortMode, EPrefabSortMode.InterestModeAscending, EPrefabSortMode.InterestModeDescending); }
			if (showStateSize                       == true) { DrawHeader(_stateSizeStat, null,                    ref headerRect, ref prefabs.SortMode, EPrefabSortMode.StateSizeDescending,   EPrefabSortMode.StateSizeAscending);     }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				prefabs.VisibleFieldsFoldout = !prefabs.VisibleFieldsFoldout;
				prefabs.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			content.yMin += headerRect.height;

			DrawFieldsGUI(ref content, prefabs, visibleFields, toggleableFields);
			DrawComponentFilterGUI(ref content, prefabs, db.Components);

			GUISeparators.AddY(content.y);

			Rect scrollRect     = new Rect(content.x, content.y, content.width + FusionInspector.RightWindowOffset, content.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * prefabs.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   minScrollY    = _scrollPosition.y;
			float   maxScrollY    = _scrollPosition.y + content.height;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition = Event.current.mousePosition;

			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
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

				PrefabInfo prefab = prefabs.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _objectStat.Width;

				if (prefab.IsSelected == true)
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

				GUIColor.Set(prefab.IsActive == true ? FusionInspector.ActiveTextColor : FusionInspector.InactiveTextColor);
				_objectContent.text = prefab.Name;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
				{
					if (_selection.Count == 1 && _selection.Contains(prefab) == true && Time.unscaledTime < (_selectionTime + FusionInspector.SelectionDelay))
					{
						_selectionTime = default;

						Selection.SetActiveObjectWithContext(prefab.GameObject, prefab.GameObject);
					}
					else
					{
						if (Event.current.shift == true)
						{
							minSelection = Mathf.Min(minSelection, i);
							maxSelection = Mathf.Max(maxSelection, i);

							for (int j = minSelection; j <= maxSelection; ++j)
							{
								prefabs.Filtered[j].IsSelected = true;
							}

							EditorGUIUtility.PingObject(prefab.GameObject);
						}
						else if (Event.current.control == true)
						{
							prefab.IsSelected = !prefab.IsSelected;
							if (prefab.IsSelected == true)
							{
								EditorGUIUtility.PingObject(prefab.GameObject);
							}
						}
						else
						{
							for (int j = 0; j < _selection.Count; ++j)
							{
								_selection[j].IsSelected = false;
							}

							if (_selection.Count != 1 || _selection.Contains(prefab) == false)
							{
								prefab.IsSelected = true;
								EditorGUIUtility.PingObject(prefab.GameObject);
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

					if (prefab.ComponentCount.Value > 0)
					{
						_componentsContent.text = prefab.ComponentCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _componentsContent, GUIStyles.RightLabel) == true)
						{
							if (prefab.Components.Length > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int componentIndex = 0; componentIndex < prefab.Components.Length; ++componentIndex)
								{
									ComponentInfo component = prefab.Components[componentIndex];
									menu.AddItem(new GUIContent($"ID:[{prefab.NetworkObject.NetworkedBehaviours[componentIndex].GetInstanceID()}] {component.TypeName}"), false, (script) =>
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

				if (showIsSpawnable                     == true) { FusionInspector.DrawIcon(_isSpawnableStat,                     ref drawRect, prefab.IsSpawnable);                     }
				if (showIsMasterClientObject            == true) { FusionInspector.DrawIcon(_isMasterClientObjectStat,            ref drawRect, prefab.IsMasterClientObject);            }
				if (showAllowStateAuthorityOverride     == true) { FusionInspector.DrawIcon(_allowStateAuthorityOverrideStat,     ref drawRect, prefab.AllowStateAuthorityOverride);     }
				if (showDestroyWhenStateAuthorityLeaves == true) { FusionInspector.DrawIcon(_destroyWhenStateAuthorityLeavesStat, ref drawRect, prefab.DestroyWhenStateAuthorityLeaves); }

				if (showInterestMode == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _interestModeStat.Width;

					EditorGUI.LabelField(drawRect, _interestModeContents[(int)prefab.InterestMode], GUIStyles.RightLabel);
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					EditorGUI.LabelField(drawRect, prefab.StateSize.GetLabel(), GUIStyles.RightLabel);
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

		private void DrawFieldsGUI(ref Rect sectionRect, Prefabs prefabs, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			if (prefabs.VisibleFieldsFoldout == false)
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
			configRect.xMax = configRect.xMin + _fieldsWidth1 * widthMultiplier;

			configRect.y += yOffset;

			EditorGUI.LabelField(configRect, "Configuration", EditorStyles.boldLabel);
			configRect.y += configRect.height;
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Components (NB)",                     EPrefabField.Components);
			DrawToggle(ref configRect, "Is Spawnable",                        EPrefabField.IsSpawnable);
			DrawToggle(ref configRect, "Is Master Client Object",             EPrefabField.IsMasterClientObject);
			DrawToggle(ref configRect, "Allow State Authority Override",      EPrefabField.AllowStateAuthorityOverride);
			DrawToggle(ref configRect, "Destroy When State Authority Leaves", EPrefabField.DestroyWhenStateAuthorityLeaves);
			DrawToggle(ref configRect, "Object Interest Mode",                EPrefabField.InterestMode);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = configRect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth2 * widthMultiplier;

			otherRect.y += yOffset;

			EditorGUI.LabelField(otherRect, "Other", EditorStyles.boldLabel);
			otherRect.y += otherRect.height;
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "State Size", EPrefabField.StateSize);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, otherRect.yMin);

			return;

			void DrawToggle(ref Rect rect, string label, EPrefabField field)
			{
				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = prefabs.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(rect, label, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					prefabs.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, Prefabs prefabs, Components components)
		{
			prefabs.ComponentFilterInclude.Clear();
			prefabs.ComponentFilterExclude.Clear();

			if (prefabs.ComponentFilterFoldout == false)
			{
				for (int i = 0, count = components.All.Count; i < count; ++i)
				{
					ComponentInfo component = components.All[i];

					if (component.Prefabs.FilterMode == EFilterMode.Include)
					{
						prefabs.ComponentFilterInclude.Add(component.Type);
					}
					else if (component.Prefabs.FilterMode == EFilterMode.Exclude)
					{
						prefabs.ComponentFilterExclude.Add(component.Type);
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

				ComponentInfo.ObjectContext context = component.Prefabs;

				if (EditorGUI.ToggleLeft(componentRect, context.ObjectFilter.GetLabel(component.TypeName, context.AllObjectCount.Value, context.FilteredObjectCount.Value), false, context.FilteredObjectCount.Value > 0 ? GUIStyles.ActiveToggle : GUIStyles.InactiveToggle) == true)
				{
					context.FilterMode = (EFilterMode)(((int)context.FilterMode + 1) % FusionInspector.FILTER_MODES);
				}

				if (context.FilterMode == EFilterMode.Include)
				{
					prefabs.ComponentFilterInclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (context.FilterMode == EFilterMode.Exclude)
				{
					prefabs.ComponentFilterExclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private void DrawSelectionGUI(Rect window, ref Rect region, FusionInspectorDB db)
		{
			Prefabs prefabs = db.Prefabs;
			if (prefabs.All.Count <= 0)
				return;

			_tempScripts.Clear();
			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				PrefabInfo prefabInfo = prefabs.Filtered[i];
				if (prefabInfo.IsSelected == true)
				{
					for (int j = 0, countJ = prefabInfo.Components.Length; j < countJ; ++j)
					{
						_tempScripts.Add(prefabInfo.Components[j]);
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

			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * _tempScripts.Count);
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
			GUI.Label(typeRect, "State", _stateSizeStat.Style);

			FusionInspector.RestoreLinePadding(ref headerRect);

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			content.yMin += headerRect.height + 0.5f;
			content.yMax -= 0.5f;

			_scriptScrollPosition = GUI.BeginScrollView(content, _scriptScrollPosition, scrollViewRect);

			float minScrollY = _scriptScrollPosition.y;
			float maxScrollY = _scriptScrollPosition.y + content.height;
			Rect  drawRect   = new Rect(FusionInspector.LeftWindowOffset, 0.0f, _objectStat.Width - widthOffset, FusionInspector.LineHeight);

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

				GUI.Label(drawRect, component.StateSize.GetLabel(), GUIStyles.RightLabel);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, content.width, region.height), headerRect.yMin, headerRect.height, headerRect.yMax, region.height - headerRect.height);
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref EPrefabSortMode sortMode, EPrefabSortMode primarySortMode, EPrefabSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (EPrefabSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref EPrefabSortMode sortMode, EPrefabSortMode primaryMode, EPrefabSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
