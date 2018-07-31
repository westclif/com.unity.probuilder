﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace UnityEditor.ProBuilder.Debug
{
	enum MeshViewState
	{
		None,
		Selected,
		All
	}

	abstract class MeshDebugView
	{
		ProBuilderMesh m_Mesh;
		MeshViewState m_ViewState;
		static GUIContent s_TempContent = new GUIContent();

		protected MeshDebugView()
		{
			ProBuilderMesh.elementSelectionChanged += SelectionChanged;
		}

		~MeshDebugView()
		{
			ProBuilderMesh.elementSelectionChanged -= SelectionChanged;
		}

		public ProBuilderMesh mesh
		{
			get { return m_Mesh; }
		}

		public MeshViewState viewState
		{
			get { return m_ViewState; }
		}

		public void SetMesh(ProBuilderMesh mesh)
		{
			m_Mesh = mesh;
			MeshAssigned();
			AnythingChanged();
		}

		public void SetViewState(MeshViewState state)
		{
			if (m_ViewState == state)
				return;
			m_ViewState = state;
			MeshViewStateChanged();
			AnythingChanged();
		}

		void SelectionChanged(ProBuilderMesh mesh)
		{
			if (mesh == m_Mesh)
			{
				SelectionChanged();
				AnythingChanged();
			}
		}

		protected virtual void MeshAssigned() {}
		protected virtual void MeshViewStateChanged() {}
		protected virtual void SelectionChanged() {}
		protected virtual void AnythingChanged() {}

		public virtual void OnGUI() { }

		public abstract void Draw(SceneView view);

		internal static void DrawSceneLabel(Vector3 worldPosition, string contents)
		{
			s_TempContent.text = contents;
			var rect = HandleUtility.WorldPointToSizedRect(worldPosition, s_TempContent, UI.EditorStyles.sceneTextBox);
			GUI.Label(rect, s_TempContent, UI.EditorStyles.sceneTextBox);
		}
	}

	sealed class MeshViewer : EditorWindow
	{
		[Serializable]
		class MeshViewSetting
		{
			[SerializeField]
			string m_Title;

			[SerializeField]
			MeshViewState m_ViewState;

			[SerializeField]
			bool m_Details;

			[SerializeField]
			string m_AssemblyQualifiedType;

			Type m_Type;

			public string title
			{
				get { return m_Title; }
			}

			public MeshViewState viewState
			{
				get { return m_ViewState; }
				set { m_ViewState = value; }
			}

			public bool detailsExpanded
			{
				get { return m_Details; }
				set { m_Details = value; }
			}

			public Type type
			{
				get
				{
					if (m_Type == null)
						m_Type = Type.GetType(m_AssemblyQualifiedType);
					return m_Type;
				}
			}

			public MeshViewSetting(string title, MeshViewState viewState, Type type)
			{
				if(!typeof(MeshDebugView).IsAssignableFrom(type))
					throw new ArgumentException("Type must be assignable to MeshDebugView.");

				m_Title = title;
				m_ViewState = viewState;
				m_AssemblyQualifiedType = type.AssemblyQualifiedName;
				m_Type = type;
			}

			public MeshDebugView GetDebugView(ProBuilderMesh mesh)
			{
				var view = (MeshDebugView)Activator.CreateInstance(type);
				view.SetViewState(m_ViewState);
				view.SetMesh(mesh);
				return view;
			}
		}

		List<MeshDebugView> m_MeshViews = new List<MeshDebugView>();

//		[SerializeField]
		List<MeshViewSetting> m_MeshViewSettings = new List<MeshViewSetting>()
		{
			new MeshViewSetting("Vertexes", MeshViewState.Selected, typeof(SharedVertexView)),
			new MeshViewSetting("Edges", MeshViewState.Selected, typeof(EdgeView))
		};

		[MenuItem("Tools/Debug/Mesh Viewer")]
		static void Init()
		{
			GetWindow<MeshViewer>(false, "Mesh Viewer", true);
		}

		void OnEnable()
		{
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			MeshSelection.objectSelectionChanged += SelectionChanged;
			ProBuilderMesh.elementSelectionChanged += SelectionChanged;
			EditorMeshUtility.meshOptimized += MeshOptimized;
			SelectionChanged();
		}

		void OnDisable()
		{
			EditorMeshUtility.meshOptimized -= MeshOptimized;
			ProBuilderMesh.elementSelectionChanged -= SelectionChanged;
			MeshSelection.objectSelectionChanged -= SelectionChanged;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
		}

		void OnGUI()
		{
			foreach (var view in m_MeshViewSettings)
			{
				EditorGUI.BeginChangeCheck();
				view.viewState = (MeshViewState) EditorGUILayout.EnumPopup(view.title, view.viewState);
				if (EditorGUI.EndChangeCheck())
					SetViewState(view);

				view.detailsExpanded = EditorGUILayout.Foldout(view.detailsExpanded, "Details");

				if (view.detailsExpanded)
				{
					GUILayout.BeginVertical(UI.EditorStyles.settingsGroup);

					foreach(var v in m_MeshViews)
						v.OnGUI();

					GUILayout.EndVertical();
				}
			}
		}

		void SetViewState(MeshViewSetting settings)
		{
			foreach(var view in m_MeshViews)
				if(settings.type.IsInstanceOfType(view))
					view.SetViewState(settings.viewState);
			SceneView.RepaintAll();
		}

		void SelectionChanged()
		{
			m_MeshViews.Clear();

			foreach(var view in m_MeshViewSettings)
				foreach(var mesh in MeshSelection.Top())
					m_MeshViews.Add(view.GetDebugView(mesh));

			Repaint();
			SceneView.RepaintAll();
		}

		void SelectionChanged(ProBuilderMesh mesh)
		{
			SelectionChanged();
		}

		void MeshOptimized(ProBuilderMesh pmesh, Mesh umesh)
		{
			SelectionChanged();
		}

		void OnSceneGUI(SceneView view)
		{
			Handles.BeginGUI();

			foreach (var mesh in m_MeshViews)
			{
				if(mesh.viewState != MeshViewState.None)
					mesh.Draw(view);
			}

			Handles.EndGUI();
		}
	}
}
