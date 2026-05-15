namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class ComponentsInspector
	{
		private Vector2               _scrollPosition;
		private UnityEngine.Object    _selectionObject;
		private float                 _selectionTime;
		private List<PrefabInfo>      _tempPrefabs = new List<PrefabInfo>();
		private List<SceneObjectInfo> _tempSceneObjects = new List<SceneObjectInfo>();

		private static GUINameStat _componentStat;

		private static GUIIconStat _hasSpawnedStat;
		private static GUIIconStat _hasDespawnedStat;
		private static GUIIconStat _hasFixedUpdateNetworkStat;
		private static GUIIconStat _hasRenderStat;

		private static GUIIconStat _hasAfterSpawnedStat;
		private static GUIIconStat _hasAfterHostMigrationStat;
		private static GUIIconStat _hasStateAuthorityChangedStat;
		private static GUIIconStat _hasInputAuthorityGainedStat;
		private static GUIIconStat _hasInputAuthorityLostStat;
		private static GUIIconStat _hasSimulationEnterStat;
		private static GUIIconStat _hasSimulationExitStat;
		private static GUIIconStat _hasInterestEnterStat;
		private static GUIIconStat _hasInterestExitStat;

		private static GUIIconStat _hasBeforeUpdateStat;
		private static GUIIconStat _hasBeforeCopyPreviousStateStat;
		private static GUIIconStat _hasBeforeClientPredictionResetStat;
		private static GUIIconStat _hasAfterClientPredictionResetStat;
		private static GUIIconStat _hasBeforeSimulationStat;
		private static GUIIconStat _hasBeforeAllTicksStat;
		private static GUIIconStat _hasBeforeTickStat;
		private static GUIIconStat _hasAfterTickStat;
		private static GUIIconStat _hasAfterAllTicksStat;
		private static GUIIconStat _hasAfterRenderStat;
		private static GUIIconStat _hasAfterUpdateStat;

		private static GUISortStat _sceneObjectsStat;
		private static GUISortStat _prefabsStat;
		private static GUISortStat _rpcsStat;
		private static GUISortStat _stateSizeStat;
		private static GUISortStat _executionOrderStat;
		private static GUISortStat _totalStateChangesStat;
		private static GUISortStat _averageStateChangesStat;

		private static GUIContent _sceneObjectsContent;
		private static GUIContent _sceneObjectsIconContent;
		private static GUIContent _prefabsContent;
		private static GUIContent _prefabsIconContent;
		private static GUIContent _rpcsContent;
		private static GUIContent _stateSizeContent;
		private static GUIContent _executionOrderContent;
		private static GUIContent _scriptContent;
		private static GUIContent _fieldsContent;

		private static float _sortWidth;
		private static float _fieldsWidth1;
		private static float _fieldsWidth2;
		private static float _fieldsWidth3;
		private static float _fieldsWidth4;

		public ComponentsInspector()
		{
			Texture2D gameObjectIcon = EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
			Texture2D prefabIcon     = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
			Texture2D scriptIcon     = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D fieldsIcon     = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_componentStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Type", "Name of the component", (int)EComponentSortMode.TypeHierarchyAscending, (int)EComponentSortMode.TypeHierarchyDescending);

			_hasSpawnedStat                     = new GUIIconStat(GUIStyles.Icon, "Ⓢ", "✔", $"Overrides {nameof(NetworkBehaviour.Spawned)}()", headerHeight);
			_hasDespawnedStat                   = new GUIIconStat(GUIStyles.Icon, "Ⓓ", "✔", $"Overrides {nameof(NetworkBehaviour.Despawned)}()", headerHeight);
			_hasFixedUpdateNetworkStat          = new GUIIconStat(GUIStyles.Icon, "Ⓕ", "✔", $"Overrides {nameof(NetworkBehaviour.FixedUpdateNetwork)}()", headerHeight);
			_hasRenderStat                      = new GUIIconStat(GUIStyles.Icon, "Ⓡ", "✔", $"Overrides {nameof(NetworkBehaviour.Render)}()", headerHeight);

			_hasAfterSpawnedStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterSpawned)}", headerHeight);
			_hasAfterHostMigrationStat          = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterHostMigration)}", headerHeight);
			_hasStateAuthorityChangedStat       = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IStateAuthorityChanged)}", headerHeight);
			_hasInputAuthorityGainedStat        = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInputAuthorityGained)}", headerHeight);
			_hasInputAuthorityLostStat          = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInputAuthorityLost)}", headerHeight);
			_hasSimulationEnterStat             = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(ISimulationEnter)}", headerHeight);
			_hasSimulationExitStat              = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(ISimulationExit)}", headerHeight);
			_hasInterestEnterStat               = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInterestEnter)}", headerHeight);
			_hasInterestExitStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInterestExit)}", headerHeight);

			_hasBeforeUpdateStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeUpdate)}", headerHeight);
			_hasBeforeCopyPreviousStateStat     = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeCopyPreviousState)}", headerHeight);
			_hasBeforeClientPredictionResetStat = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeClientPredictionReset)}", headerHeight);
			_hasAfterClientPredictionResetStat  = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterClientPredictionReset)}", headerHeight);
			_hasBeforeSimulationStat            = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeSimulation)}", headerHeight);
			_hasBeforeAllTicksStat              = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeAllTicks)}", headerHeight);
			_hasBeforeTickStat                  = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeTick)}", headerHeight);
			_hasAfterTickStat                   = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterTick)}", headerHeight);
			_hasAfterAllTicksStat               = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterAllTicks)}", headerHeight);
			_hasAfterRenderStat                 = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterRender)}", headerHeight);
			_hasAfterUpdateStat                 = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterUpdate)}", headerHeight);

			_sceneObjectsStat        = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of Scene Objects with the component", (int)EComponentSortMode.SceneObjectsAscending, (int)EComponentSortMode.SceneObjectsDescending);
			_prefabsStat             = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of Prefabs with the component", (int)EComponentSortMode.PrefabsAscending, (int)EComponentSortMode.PrefabsDescending);
			_rpcsStat                = new GUISortStat(GUIStyles.RightBoldLabel, "RPCs", "# of RPCs declared in the component", (int)EComponentSortMode.RPCsAscending, (int)EComponentSortMode.RPCsDescending);
			_stateSizeStat           = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state in Bytes", (int)EComponentSortMode.StateSizeAscending, (int)EComponentSortMode.StateSizeDescending, stateSizeWidth);
			_executionOrderStat      = new GUISortStat(GUIStyles.RightBoldLabel, "Execution", "Script execution order", (int)EComponentSortMode.ExecutionOrderAscending, (int)EComponentSortMode.ExecutionOrderDescending);
			_totalStateChangesStat   = new GUISortStat(GUIStyles.RightBoldLabel, "Σ Changes", "Total changes in networked state in Bytes", (int)EComponentSortMode.TotalStateChangesAscending, (int)EComponentSortMode.TotalStateChangesDescending);
			_averageStateChangesStat = new GUISortStat(GUIStyles.RightBoldLabel, "Ø Changes", "Average changes in networked state in Bytes per second", (int)EComponentSortMode.AverageStateChangesAscending, (int)EComponentSortMode.AverageStateChangesDescending);

			_sceneObjectsContent     = new GUIContent("", "# of Scene Objects with this component");
			_sceneObjectsIconContent = new GUIContent(gameObjectIcon, "# of Scene Objects with this component");
			_prefabsContent          = new GUIContent("", "# of Prefabs with this component");
			_prefabsIconContent      = new GUIContent(prefabIcon, "# of Prefabs with this component");
			_rpcsContent             = new GUIContent("", "# of RPCs declared in this component");
			_stateSizeContent        = new GUIContent("", "Size of the networked state in Bytes");
			_executionOrderContent   = new GUIContent("", "Script execution order");
			_scriptContent           = new GUIContent(scriptIcon);
			_fieldsContent           = new GUIContent(fieldsIcon, "Visible fields");

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_sceneObjectsStat.Width        += _sortWidth;
			_prefabsStat.Width             += _sortWidth;
			_rpcsStat.Width                += _sortWidth;
			_stateSizeStat.Width           += _sortWidth;
			_executionOrderStat.Width      += _sortWidth;
			_totalStateChangesStat.Width   += _sortWidth;
			_averageStateChangesStat.Width += _sortWidth;

			_fieldsWidth1 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("FixedUpdateNetwork()")).x;
			_fieldsWidth2 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("IStateAuthorityChanged")).x;
			_fieldsWidth3 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("IBeforeClientPredictionReset")).x;
			_fieldsWidth4 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Ø State Changes")).x;
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			GameMode        gameMode         = db.CurrentRunner != null ? db.CurrentRunner.GameMode : default;
			EComponentField visibleFields    = db.Components.VisibleFields | EComponentField.Type;
			EComponentField toggleableFields = ~EComponentField.Type;

			bool showRuntimeInfo = gameMode != default;
			if (showRuntimeInfo == false)
			{
				visibleFields &= ~EComponentField.TotalStateChanges;
				visibleFields &= ~EComponentField.AverageStateChanges;

				toggleableFields &= ~EComponentField.TotalStateChanges;
				toggleableFields &= ~EComponentField.AverageStateChanges;
			}

			DrawComponentsGUI(window, region, db, visibleFields, toggleableFields);
		}

		private void DrawComponentsGUI(Rect window, Rect region, FusionInspectorDB db, EComponentField visibleFields, EComponentField toggleableFields)
		{
			Components components = db.Components;

			bool showHasSpawned                     = visibleFields.Has(EComponentField.HasSpawned);
			bool showHasDespawned                   = visibleFields.Has(EComponentField.HasDespawned);
			bool showHasFixedUpdateNetwork          = visibleFields.Has(EComponentField.HasFixedUpdateNetwork);
			bool showHasRender                      = visibleFields.Has(EComponentField.HasRender);

			bool showHasAfterSpawned                = visibleFields.Has(EComponentField.HasAfterSpawned);
			bool showHasAfterHostMigration          = visibleFields.Has(EComponentField.HasAfterHostMigration);
			bool showStateAuthorityChanged          = visibleFields.Has(EComponentField.HasStateAuthorityChanged);
			bool showInputAuthorityGained           = visibleFields.Has(EComponentField.HasInputAuthorityGained);
			bool showInputAuthorityLost             = visibleFields.Has(EComponentField.HasInputAuthorityLost);
			bool showHasSimulationEnter             = visibleFields.Has(EComponentField.HasSimulationEnter);
			bool showHasSimulationExit              = visibleFields.Has(EComponentField.HasSimulationExit);
			bool showHasInterestEnter               = visibleFields.Has(EComponentField.HasInterestEnter);
			bool showHasInterestExit                = visibleFields.Has(EComponentField.HasInterestExit);

			bool showHasBeforeUpdate                = visibleFields.Has(EComponentField.HasBeforeUpdate);
			bool showHasBeforeCopyPreviousState     = visibleFields.Has(EComponentField.HasBeforeCopyPreviousState);
			bool showHasBeforeClientPredictionReset = visibleFields.Has(EComponentField.HasBeforeClientPredictionReset);
			bool showHasAfterClientPredictionReset  = visibleFields.Has(EComponentField.HasAfterClientPredictionReset);
			bool showHasBeforeSimulation            = visibleFields.Has(EComponentField.HasBeforeSimulation);
			bool showHasBeforeAllTicks              = visibleFields.Has(EComponentField.HasBeforeAllTicks);
			bool showHasBeforeTick                  = visibleFields.Has(EComponentField.HasBeforeTick);
			bool showHasAfterTick                   = visibleFields.Has(EComponentField.HasAfterTick);
			bool showHasAfterAllTicks               = visibleFields.Has(EComponentField.HasAfterAllTicks);
			bool showHasAfterRender                 = visibleFields.Has(EComponentField.HasAfterRender);
			bool showHasAfterUpdate                 = visibleFields.Has(EComponentField.HasAfterUpdate);

			bool showSceneObjects                   = visibleFields.Has(EComponentField.SceneObjects);
			bool showPrefabs                        = visibleFields.Has(EComponentField.Prefabs);
			bool showRPCs                           = visibleFields.Has(EComponentField.RPCs);
			bool showStateSize                      = visibleFields.Has(EComponentField.StateSize);
			bool showExecutionOrder                 = visibleFields.Has(EComponentField.ExecutionOrder);
			bool showTotalStateChanges              = visibleFields.Has(EComponentField.TotalStateChanges);
			bool showAverageStateChanges            = visibleFields.Has(EComponentField.AverageStateChanges);

			GUISeparators.Clear();

			region.xMin += FusionInspector.LeftWindowOffset;
			region.xMax -= FusionInspector.RightWindowOffset;

			_componentStat.Width = region.width;

			if (showHasSpawned                     == true) { _componentStat.Width -= _hasSpawnedStat.Width;                     } else { components.HasSpawnedFilter                     = default; }
			if (showHasDespawned                   == true) { _componentStat.Width -= _hasDespawnedStat.Width;                   } else { components.HasDespawnedFilter                   = default; }
			if (showHasFixedUpdateNetwork          == true) { _componentStat.Width -= _hasFixedUpdateNetworkStat.Width;          } else { components.HasFixedUpdateNetworkFilter          = default; }
			if (showHasRender                      == true) { _componentStat.Width -= _hasRenderStat.Width;                      } else { components.HasRenderFilter                      = default; }

			if (showHasAfterSpawned                == true) { _componentStat.Width -= _hasAfterSpawnedStat.Width;                } else { components.HasAfterSpawnedFilter                = default; }
			if (showHasAfterHostMigration          == true) { _componentStat.Width -= _hasAfterHostMigrationStat.Width;          } else { components.HasAfterHostMigrationFilter          = default; }
			if (showStateAuthorityChanged          == true) { _componentStat.Width -= _hasStateAuthorityChangedStat.Width;       } else { components.HasStateAuthorityChangedFilter       = default; }
			if (showInputAuthorityGained           == true) { _componentStat.Width -= _hasInputAuthorityGainedStat.Width;        } else { components.HasInputAuthorityGainedFilter        = default; }
			if (showInputAuthorityLost             == true) { _componentStat.Width -= _hasInputAuthorityLostStat.Width;          } else { components.HasInputAuthorityLostFilter          = default; }
			if (showHasSimulationEnter             == true) { _componentStat.Width -= _hasSimulationEnterStat.Width;             } else { components.HasSimulationEnterFilter             = default; }
			if (showHasSimulationExit              == true) { _componentStat.Width -= _hasSimulationExitStat.Width;              } else { components.HasSimulationExitFilter              = default; }
			if (showHasInterestEnter               == true) { _componentStat.Width -= _hasInterestEnterStat.Width;               } else { components.HasInterestEnterFilter               = default; }
			if (showHasInterestExit                == true) { _componentStat.Width -= _hasInterestExitStat.Width;                } else { components.HasInterestExitFilter                = default; }

			if (showHasBeforeUpdate                == true) { _componentStat.Width -= _hasBeforeUpdateStat.Width;                } else { components.HasBeforeUpdateFilter                = default; }
			if (showHasBeforeCopyPreviousState     == true) { _componentStat.Width -= _hasBeforeCopyPreviousStateStat.Width;     } else { components.HasBeforeCopyPreviousStateFilter     = default; }
			if (showHasBeforeClientPredictionReset == true) { _componentStat.Width -= _hasBeforeClientPredictionResetStat.Width; } else { components.HasBeforeClientPredictionResetFilter = default; }
			if (showHasAfterClientPredictionReset  == true) { _componentStat.Width -= _hasAfterClientPredictionResetStat.Width;  } else { components.HasAfterClientPredictionResetFilter  = default; }
			if (showHasBeforeSimulation            == true) { _componentStat.Width -= _hasBeforeSimulationStat.Width;            } else { components.HasBeforeSimulationFilter            = default; }
			if (showHasBeforeAllTicks              == true) { _componentStat.Width -= _hasBeforeAllTicksStat.Width;              } else { components.HasBeforeAllTicksFilter              = default; }
			if (showHasBeforeTick                  == true) { _componentStat.Width -= _hasBeforeTickStat.Width;                  } else { components.HasBeforeTickFilter                  = default; }
			if (showHasAfterTick                   == true) { _componentStat.Width -= _hasAfterTickStat.Width;                   } else { components.HasAfterTickFilter                   = default; }
			if (showHasAfterAllTicks               == true) { _componentStat.Width -= _hasAfterAllTicksStat.Width;               } else { components.HasAfterAllTicksFilter               = default; }
			if (showHasAfterRender                 == true) { _componentStat.Width -= _hasAfterRenderStat.Width;                 } else { components.HasAfterRenderFilter                 = default; }
			if (showHasAfterUpdate                 == true) { _componentStat.Width -= _hasAfterUpdateStat.Width;                 } else { components.HasAfterUpdateFilter                 = default; }

			if (showSceneObjects                   == true) { _componentStat.Width -= _sceneObjectsStat.Width;        }
			if (showPrefabs                        == true) { _componentStat.Width -= _prefabsStat.Width;             }
			if (showRPCs                           == true) { _componentStat.Width -= _rpcsStat.Width;                }
			if (showStateSize                      == true) { _componentStat.Width -= _stateSizeStat.Width;           }
			if (showExecutionOrder                 == true) { _componentStat.Width -= _executionOrderStat.Width;      }
			if (showTotalStateChanges              == true) { _componentStat.Width -= _totalStateChangesStat.Width;   }
			if (showAverageStateChanges            == true) { _componentStat.Width -= _averageStateChangesStat.Width; }

			Rect headerRect = region;
			headerRect.width  = _componentStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect typeFilterRect = headerRect;
			typeFilterRect.xMax -= 4.0f;
			typeFilterRect.xMin = typeFilterRect.xMax - headerRect.width * 0.4f;
			typeFilterRect.y += 0.5f;
			if (typeFilterRect.height > EditorGUIUtility.singleLineHeight)
			{
				typeFilterRect.y += (typeFilterRect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
				typeFilterRect.height = EditorGUIUtility.singleLineHeight;
			}
			components.TypeNameFilter = EditorGUI.TextField(typeFilterRect, components.TypeNameFilter, GUIStyles.SearchField);

			Rect typeRect = headerRect;
			typeRect.xMax = typeFilterRect.xMin;
			typeRect.xMin += _componentStat.IsSorted((int)components.SortMode) ? 0.0f : _sortWidth;
			if (GUI.Button(typeRect, _componentStat.GetContent(components.All.Count, components.Filtered.Count, default, (int)components.SortMode), _componentStat.Style) == true)
			{
				ToggleSortMode(ref components.SortMode, EComponentSortMode.TypeHierarchyAscending, EComponentSortMode.TypeHierarchyDescending);
			}

			if (showHasSpawned                     == true) { DrawHeader(_hasSpawnedStat,                     ref headerRect, ref components.HasSpawnedFilter);                     }
			if (showHasDespawned                   == true) { DrawHeader(_hasDespawnedStat,                   ref headerRect, ref components.HasDespawnedFilter);                   }
			if (showHasFixedUpdateNetwork          == true) { DrawHeader(_hasFixedUpdateNetworkStat,          ref headerRect, ref components.HasFixedUpdateNetworkFilter);          }
			if (showHasRender                      == true) { DrawHeader(_hasRenderStat,                      ref headerRect, ref components.HasRenderFilter);                      }

			if (showHasAfterSpawned                == true) { DrawHeader(_hasAfterSpawnedStat,                ref headerRect, ref components.HasAfterSpawnedFilter);                }
			if (showHasAfterHostMigration          == true) { DrawHeader(_hasAfterHostMigrationStat,          ref headerRect, ref components.HasAfterHostMigrationFilter);          }
			if (showStateAuthorityChanged          == true) { DrawHeader(_hasStateAuthorityChangedStat,       ref headerRect, ref components.HasStateAuthorityChangedFilter);       }
			if (showInputAuthorityGained           == true) { DrawHeader(_hasInputAuthorityGainedStat,        ref headerRect, ref components.HasInputAuthorityGainedFilter);        }
			if (showInputAuthorityLost             == true) { DrawHeader(_hasInputAuthorityLostStat,          ref headerRect, ref components.HasInputAuthorityLostFilter);          }
			if (showHasSimulationEnter             == true) { DrawHeader(_hasSimulationEnterStat,             ref headerRect, ref components.HasSimulationEnterFilter);             }
			if (showHasSimulationExit              == true) { DrawHeader(_hasSimulationExitStat,              ref headerRect, ref components.HasSimulationExitFilter);              }
			if (showHasInterestEnter               == true) { DrawHeader(_hasInterestEnterStat,               ref headerRect, ref components.HasInterestEnterFilter);               }
			if (showHasInterestExit                == true) { DrawHeader(_hasInterestExitStat,                ref headerRect, ref components.HasInterestExitFilter);                }

			if (showHasBeforeUpdate                == true) { DrawHeader(_hasBeforeUpdateStat,                ref headerRect, ref components.HasBeforeUpdateFilter);                }
			if (showHasBeforeCopyPreviousState     == true) { DrawHeader(_hasBeforeCopyPreviousStateStat,     ref headerRect, ref components.HasBeforeCopyPreviousStateFilter);     }
			if (showHasBeforeClientPredictionReset == true) { DrawHeader(_hasBeforeClientPredictionResetStat, ref headerRect, ref components.HasBeforeClientPredictionResetFilter); }
			if (showHasAfterClientPredictionReset  == true) { DrawHeader(_hasAfterClientPredictionResetStat,  ref headerRect, ref components.HasAfterClientPredictionResetFilter);  }
			if (showHasBeforeSimulation            == true) { DrawHeader(_hasBeforeSimulationStat,            ref headerRect, ref components.HasBeforeSimulationFilter);            }
			if (showHasBeforeAllTicks              == true) { DrawHeader(_hasBeforeAllTicksStat,              ref headerRect, ref components.HasBeforeAllTicksFilter);              }
			if (showHasBeforeTick                  == true) { DrawHeader(_hasBeforeTickStat,                  ref headerRect, ref components.HasBeforeTickFilter);                  }
			if (showHasAfterTick                   == true) { DrawHeader(_hasAfterTickStat,                   ref headerRect, ref components.HasAfterTickFilter);                   }
			if (showHasAfterAllTicks               == true) { DrawHeader(_hasAfterAllTicksStat,               ref headerRect, ref components.HasAfterAllTicksFilter);               }
			if (showHasAfterRender                 == true) { DrawHeader(_hasAfterRenderStat,                 ref headerRect, ref components.HasAfterRenderFilter);                 }
			if (showHasAfterUpdate                 == true) { DrawHeader(_hasAfterUpdateStat,                 ref headerRect, ref components.HasAfterUpdateFilter);                 }

			if (showSceneObjects                   == true) { DrawHeader(_sceneObjectsStat,        _sceneObjectsIconContent, ref headerRect, ref components.SortMode, EComponentSortMode.SceneObjectsDescending,        EComponentSortMode.SceneObjectsAscending);        }
			if (showPrefabs                        == true) { DrawHeader(_prefabsStat,             _prefabsIconContent,      ref headerRect, ref components.SortMode, EComponentSortMode.PrefabsDescending,             EComponentSortMode.PrefabsAscending);             }
			if (showRPCs                           == true) { DrawHeader(_rpcsStat,                null,                     ref headerRect, ref components.SortMode, EComponentSortMode.RPCsDescending,                EComponentSortMode.RPCsAscending);                }
			if (showStateSize                      == true) { DrawHeader(_stateSizeStat,           null,                     ref headerRect, ref components.SortMode, EComponentSortMode.StateSizeDescending,           EComponentSortMode.StateSizeAscending);           }
			if (showExecutionOrder                 == true) { DrawHeader(_executionOrderStat,      null,                     ref headerRect, ref components.SortMode, EComponentSortMode.ExecutionOrderAscending,       EComponentSortMode.ExecutionOrderDescending);     }
			if (showTotalStateChanges              == true) { DrawHeader(_totalStateChangesStat,   null,                     ref headerRect, ref components.SortMode, EComponentSortMode.TotalStateChangesDescending,   EComponentSortMode.TotalStateChangesAscending);   }
			if (showAverageStateChanges            == true) { DrawHeader(_averageStateChangesStat, null,                     ref headerRect, ref components.SortMode, EComponentSortMode.AverageStateChangesDescending, EComponentSortMode.AverageStateChangesAscending); }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				components.VisibleFieldsFoldout = !components.VisibleFieldsFoldout;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			region.yMin += headerRect.height;

			DrawFieldsGUI(ref region, components, visibleFields, toggleableFields);

			GUISeparators.AddY(region.y);

			Rect scrollRect     = new Rect(region.x, region.y, region.width + FusionInspector.RightWindowOffset, region.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, region.width, FusionInspector.LineHeight * components.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   indent           = 16.0f;
			float   minScrollY       = _scrollPosition.y;
			float   maxScrollY       = _scrollPosition.y + region.height;
			Rect    drawRect         = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition    = Event.current.mousePosition;
			bool    canShowHierarchy = components.All.Count == components.Filtered.Count && (components.SortMode == EComponentSortMode.TypeHierarchyAscending || components.SortMode == EComponentSortMode.TypeHierarchyDescending);

			for (int i = 0, count = components.Filtered.Count; i < count; ++i)
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

				ComponentInfo component = components.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _componentStat.Width;

				if (mousePosition.x >= region.xMin && mousePosition.x <= region.xMax && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
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

				int typeLevel = canShowHierarchy == true ? component.TypeLevel : 0;
				drawRect.xMin += typeLevel * indent;
				if (typeLevel > 0)
				{
					drawRect.xMin += GUIStyles.ObjectField.padding.left;
					EditorGUI.LabelField(new Rect(drawRect.x - indent, drawRect.y, indent, drawRect.height), GUISymbols.DERIVE, GUIStyles.Icon);
					drawRect.xMin -= GUIStyles.ObjectField.padding.left;
				}
				_scriptContent.text = component.TypeName;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _scriptContent, GUIStyles.ObjectField) == true && component.Script != null)
				{
					if (_selectionObject == component.Script && Time.unscaledTime < (_selectionTime + FusionInspector.SelectionDelay))
					{
						AssetDatabase.OpenAsset(component.Script);
						_selectionObject = default;
						_selectionTime   = default;
					}
					else
					{
						EditorGUIUtility.PingObject(component.Script);
						_selectionObject = component.Script;
						_selectionTime   = Time.unscaledTime;
					}
				}
				drawRect.height -= 1.0f; // GUI.Button dead zone fix
				drawRect.xMin -= typeLevel * indent;

				if (showHasSpawned                     == true) { FusionInspector.DrawIcon(_hasSpawnedStat                     , ref drawRect, component.HasSpawned);                     }
				if (showHasDespawned                   == true) { FusionInspector.DrawIcon(_hasDespawnedStat                   , ref drawRect, component.HasDespawned);                   }
				if (showHasFixedUpdateNetwork          == true) { FusionInspector.DrawIcon(_hasFixedUpdateNetworkStat          , ref drawRect, component.HasFixedUpdateNetwork);          }
				if (showHasRender                      == true) { FusionInspector.DrawIcon(_hasRenderStat                      , ref drawRect, component.HasRender);                      }

				if (showHasAfterSpawned                == true) { FusionInspector.DrawIcon(_hasAfterSpawnedStat                , ref drawRect, component.HasAfterSpawned);                }
				if (showHasAfterHostMigration          == true) { FusionInspector.DrawIcon(_hasAfterHostMigrationStat          , ref drawRect, component.HasAfterHostMigration);          }
				if (showStateAuthorityChanged          == true) { FusionInspector.DrawIcon(_hasStateAuthorityChangedStat       , ref drawRect, component.HasStateAuthorityChanged);       }
				if (showInputAuthorityGained           == true) { FusionInspector.DrawIcon(_hasInputAuthorityGainedStat        , ref drawRect, component.HasInputAuthorityGained);        }
				if (showInputAuthorityLost             == true) { FusionInspector.DrawIcon(_hasInputAuthorityLostStat          , ref drawRect, component.HasInputAuthorityLost);          }
				if (showHasSimulationEnter             == true) { FusionInspector.DrawIcon(_hasSimulationEnterStat             , ref drawRect, component.HasSimulationEnter);             }
				if (showHasSimulationExit              == true) { FusionInspector.DrawIcon(_hasSimulationExitStat              , ref drawRect, component.HasSimulationExit);              }
				if (showHasInterestEnter               == true) { FusionInspector.DrawIcon(_hasInterestEnterStat               , ref drawRect, component.HasInterestEnter);               }
				if (showHasInterestExit                == true) { FusionInspector.DrawIcon(_hasInterestExitStat                , ref drawRect, component.HasInterestExit);                }

				if (showHasBeforeUpdate                == true) { FusionInspector.DrawIcon(_hasBeforeUpdateStat                , ref drawRect, component.HasBeforeUpdate);                }
				if (showHasBeforeCopyPreviousState     == true) { FusionInspector.DrawIcon(_hasBeforeCopyPreviousStateStat     , ref drawRect, component.HasBeforeCopyPreviousState);     }
				if (showHasBeforeClientPredictionReset == true) { FusionInspector.DrawIcon(_hasBeforeClientPredictionResetStat , ref drawRect, component.HasBeforeClientPredictionReset); }
				if (showHasAfterClientPredictionReset  == true) { FusionInspector.DrawIcon(_hasAfterClientPredictionResetStat  , ref drawRect, component.HasAfterClientPredictionReset);  }
				if (showHasBeforeSimulation            == true) { FusionInspector.DrawIcon(_hasBeforeSimulationStat            , ref drawRect, component.HasBeforeSimulation);            }
				if (showHasBeforeAllTicks              == true) { FusionInspector.DrawIcon(_hasBeforeAllTicksStat              , ref drawRect, component.HasBeforeAllTicks);              }
				if (showHasBeforeTick                  == true) { FusionInspector.DrawIcon(_hasBeforeTickStat                  , ref drawRect, component.HasBeforeTick);                  }
				if (showHasAfterTick                   == true) { FusionInspector.DrawIcon(_hasAfterTickStat                   , ref drawRect, component.HasAfterTick);                   }
				if (showHasAfterAllTicks               == true) { FusionInspector.DrawIcon(_hasAfterAllTicksStat               , ref drawRect, component.HasAfterAllTicks);               }
				if (showHasAfterRender                 == true) { FusionInspector.DrawIcon(_hasAfterRenderStat                 , ref drawRect, component.HasAfterRender);                 }
				if (showHasAfterUpdate                 == true) { FusionInspector.DrawIcon(_hasAfterUpdateStat                 , ref drawRect, component.HasAfterUpdate);                 }

				if (showSceneObjects == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _sceneObjectsStat.Width;

					if (component.SceneObjects.ScriptCount.Value > 0)
					{
						_sceneObjectsContent.text = component.SceneObjects.ScriptCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _sceneObjectsContent, GUIStyles.RightLabel) == true)
						{
							db.SceneObjects.GetObjects(_tempSceneObjects, component.Type);

							if (_tempSceneObjects.Count > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int objectIndex = 0; objectIndex < _tempSceneObjects.Count; ++objectIndex)
								{
									SceneObjectInfo sceneObject = _tempSceneObjects[objectIndex];
									menu.AddItem(new GUIContent($"ID:[{sceneObject.GameObject.GetInstanceID()}] {sceneObject.Name}"), false, (gameObject) =>
									{
										GameObject go = gameObject as GameObject;
										if (go != null)
										{
											Selection.SetActiveObjectWithContext(go, go);
										}
									}, sceneObject.GameObject);
								}
								menu.ShowAsContext();
							}

							_tempSceneObjects.Clear();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showPrefabs == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _prefabsStat.Width;

					if (component.Prefabs.ScriptCount.Value > 0)
					{
						_prefabsContent.text = component.Prefabs.ScriptCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _prefabsContent, GUIStyles.RightLabel) == true)
						{
							db.Prefabs.GetObjects(_tempPrefabs, component.Type);

							if (_tempPrefabs.Count > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int objectIndex = 0; objectIndex < _tempPrefabs.Count; ++objectIndex)
								{
									PrefabInfo prefab = _tempPrefabs[objectIndex];
									menu.AddItem(new GUIContent($"ID:[{prefab.GameObject.GetInstanceID()}] {prefab.Name}"), false, (gameObject) =>
									{
										GameObject go = gameObject as GameObject;
										if (go != null)
										{
											Selection.SetActiveObjectWithContext(go, go);
										}
									}, prefab.GameObject);
								}
								menu.ShowAsContext();
							}

							_tempPrefabs.Clear();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showRPCs == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _rpcsStat.Width;

					if (component.RPCCount.Value > 0)
					{
						_rpcsContent.text = component.RPCCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _rpcsContent, GUIStyles.RightLabel) == true)
						{
							GenericMenu menu = new GenericMenu();
							foreach (System.Reflection.MethodInfo method in component.RPCs)
							{
								menu.AddItem(new GUIContent(ReflectionUtility.GetMethodDeclaration(method)), false, (script) =>
								{
									MonoScript monoScript = script as MonoScript;
									if (monoScript != null)
									{
										int methodLineNumber = ScriptUtility.GetLineNumber(monoScript, method);
										AssetDatabase.OpenAsset(monoScript, methodLineNumber);
									}
								}, component.Script);
							}
							menu.ShowAsContext();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					_stateSizeContent.text = component.StateSize.GetLabel();
					GUIColor.Set(component.StateSize.Value > 0 ? FusionInspector.NormalTextColor : FusionInspector.InactiveTextColor);
					EditorGUI.LabelField(drawRect, _stateSizeContent, GUIStyles.RightLabel);
					GUIColor.Reset();
				}

				if (showExecutionOrder)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _executionOrderStat.Width;

					if (component.ExecutionOrder.Value != 0)
					{
						_executionOrderContent.text = component.ExecutionOrder.GetLabel();
						EditorGUI.LabelField(drawRect, _executionOrderContent, GUIStyles.RightLabel);
					}
				}

				if (showTotalStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _totalStateChangesStat.Width;

					if (component.TotalStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, component.TotalStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showAverageStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _averageStateChangesStat.Width;

					if (component.AverageStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, component.AverageStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
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

		private void DrawFieldsGUI(ref Rect sectionRect, Components components, EComponentField visibleFields, EComponentField toggleableFields)
		{
			if (components.VisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2 + _fieldsWidth3 + _fieldsWidth4;
			float widthMultiplier = innerRect.width / totalWidth;

			//----------------------------------------------------------------

			Rect virtualMethodsRect = innerRect;
			virtualMethodsRect.xMin = innerRect.xMin;
			virtualMethodsRect.xMax = virtualMethodsRect.xMin + _fieldsWidth1 * widthMultiplier;

			virtualMethodsRect.y += yOffset;

			EditorGUI.LabelField(virtualMethodsRect, "Virtual Methods", EditorStyles.boldLabel);
			virtualMethodsRect.y += virtualMethodsRect.height;
			virtualMethodsRect.y += yOffset;

			DrawToggle(ref virtualMethodsRect, "Spawned()",            EComponentField.HasSpawned);
			DrawToggle(ref virtualMethodsRect, "Despawned()",          EComponentField.HasDespawned);
			DrawToggle(ref virtualMethodsRect, "FixedUpdateNetwork()", EComponentField.HasFixedUpdateNetwork);
			DrawToggle(ref virtualMethodsRect, "Render()",             EComponentField.HasRender);

			virtualMethodsRect.y += yOffset;

			//----------------------------------------------------------------

			Rect interfaces1Rect = innerRect;
			interfaces1Rect.xMin = virtualMethodsRect.xMax + xOffset;
			interfaces1Rect.xMax = interfaces1Rect.xMin + _fieldsWidth2 * widthMultiplier;

			interfaces1Rect.y += yOffset;

			EditorGUI.LabelField(interfaces1Rect, "Interfaces (1)", EditorStyles.boldLabel);
			interfaces1Rect.y += interfaces1Rect.height;
			interfaces1Rect.y += yOffset;

			DrawToggle(ref interfaces1Rect, "IAfterSpawned",                EComponentField.HasAfterSpawned);
			DrawToggle(ref interfaces1Rect, "IAfterHostMigration",          EComponentField.HasAfterHostMigration);
			DrawToggle(ref interfaces1Rect, "IStateAuthorityChanged",       EComponentField.HasStateAuthorityChanged);
			DrawToggle(ref interfaces1Rect, "IInputAuthorityGained",        EComponentField.HasInputAuthorityGained);
			DrawToggle(ref interfaces1Rect, "IInputAuthorityLost",          EComponentField.HasInputAuthorityLost);
			DrawToggle(ref interfaces1Rect, "ISimulationEnter",             EComponentField.HasSimulationEnter);
			DrawToggle(ref interfaces1Rect, "ISimulationExit",              EComponentField.HasSimulationExit);
			DrawToggle(ref interfaces1Rect, "IInterestEnter",               EComponentField.HasInterestEnter);
			DrawToggle(ref interfaces1Rect, "IInterestExit",                EComponentField.HasInterestExit);

			interfaces1Rect.y += yOffset;

			//----------------------------------------------------------------

			Rect interfaces2Rect = innerRect;
			interfaces2Rect.xMin = interfaces1Rect.xMax + xOffset;
			interfaces2Rect.xMax = interfaces2Rect.xMin + _fieldsWidth3 * widthMultiplier;

			interfaces2Rect.y += yOffset;

			EditorGUI.LabelField(interfaces2Rect, "Interfaces (2)", EditorStyles.boldLabel);
			interfaces2Rect.y += interfaces2Rect.height;
			interfaces2Rect.y += yOffset;

			DrawToggle(ref interfaces2Rect, "IBeforeUpdate",                EComponentField.HasBeforeUpdate);
			DrawToggle(ref interfaces2Rect, "IBeforeCopyPreviousState",     EComponentField.HasBeforeCopyPreviousState);
			DrawToggle(ref interfaces2Rect, "IBeforeClientPredictionReset", EComponentField.HasBeforeClientPredictionReset);
			DrawToggle(ref interfaces2Rect, "IAfterClientPredictionReset",  EComponentField.HasAfterClientPredictionReset);
			DrawToggle(ref interfaces2Rect, "IBeforeSimulation",            EComponentField.HasBeforeSimulation);
			DrawToggle(ref interfaces2Rect, "IBeforeAllTicks",              EComponentField.HasBeforeAllTicks);
			DrawToggle(ref interfaces2Rect, "IBeforeTick",                  EComponentField.HasBeforeTick);
			DrawToggle(ref interfaces2Rect, "IAfterTick",                   EComponentField.HasAfterTick);
			DrawToggle(ref interfaces2Rect, "IAfterAllTicks",               EComponentField.HasAfterAllTicks);
			DrawToggle(ref interfaces2Rect, "IAfterRender",                 EComponentField.HasAfterRender);
			DrawToggle(ref interfaces2Rect, "IAfterUpdate",                 EComponentField.HasAfterUpdate);

			interfaces2Rect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = interfaces2Rect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth4 * widthMultiplier;

			otherRect.y += yOffset;

			EditorGUI.LabelField(otherRect, "Other", EditorStyles.boldLabel);
			otherRect.y += otherRect.height;
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "Scene Objects",   EComponentField.SceneObjects);
			DrawToggle(ref otherRect, "Prefabs",         EComponentField.Prefabs);
			DrawToggle(ref otherRect, "RPCs",            EComponentField.RPCs);
			DrawToggle(ref otherRect, "State Size",      EComponentField.StateSize);
			DrawToggle(ref otherRect, "Execution Order", EComponentField.ExecutionOrder);
			DrawToggle(ref otherRect, "Σ State Changes", EComponentField.TotalStateChanges);
			DrawToggle(ref otherRect, "Ø State Changes", EComponentField.AverageStateChanges);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(virtualMethodsRect.yMin, interfaces1Rect.yMin, interfaces2Rect.yMin, otherRect.yMin);

			return;

			void DrawToggle(ref Rect rect, string label, EComponentField field)
			{
				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = components.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(rect, label, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					components.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref EComponentSortMode sortMode, EComponentSortMode primarySortMode, EComponentSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (EComponentSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref EComponentSortMode sortMode, EComponentSortMode primaryMode, EComponentSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
