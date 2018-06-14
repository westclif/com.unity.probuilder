﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;
using PHandleUtility = UnityEngine.ProBuilder.HandleUtility;
using UHandleUtility = UnityEditor.HandleUtility;
using MaterialEditor = UnityEditor.ProBuilder.MaterialEditor;
using RaycastHit = UnityEngine.ProBuilder.RaycastHit;

namespace UnityEditor.ProBuilder
{
	struct ScenePickerPreferences
	{
		public const float maxPointerDistanceFuzzy = 128f;
		public const float maxPointerDistancePrecise = 12f;
		public const CullingMode defaultCullingMode = CullingMode.Back;
		public const SelectionModifierBehavior defaultSelectionModifierBehavior = SelectionModifierBehavior.Difference;
		public const RectSelectMode defaultRectSelectionMode = RectSelectMode.Partial;

		public float maxPointerDistance;
		public CullingMode cullMode;
		public SelectionModifierBehavior selectionModifierBehavior;
		public RectSelectMode rectSelectMode;
	}

	class SceneSelection : IEquatable<SceneSelection>
	{
		public GameObject gameObject;
		public ProBuilderMesh mesh;
		public int vertex;
		public Edge edge;
		public Face face;

		public SceneSelection(GameObject gameObject = null)
		{
			this.gameObject = gameObject;
		}

		public SceneSelection(ProBuilderMesh mesh, int vertex) : this(mesh != null ? mesh.gameObject : null)
		{
			this.mesh = mesh;
			this.vertex = vertex;
			edge = Edge.Empty;
			face = null;
		}

		public SceneSelection(ProBuilderMesh mesh, Edge edge) : this(mesh != null ? mesh.gameObject : null)
		{
			this.mesh = mesh;
			vertex = -1;
			this.edge = edge;
			face = null;
		}

		public SceneSelection(ProBuilderMesh mesh, Face face) : this(mesh != null ? mesh.gameObject : null)
		{
			this.mesh = mesh;
			vertex = -1;
			edge = Edge.Empty;
			this.face = face;
		}

		public void Clear()
		{
			gameObject = null;
			mesh = null;
			face = null;
			edge = Edge.Empty;
			vertex = -1;
		}

		public void CopyTo(SceneSelection dst)
		{
			dst.gameObject = gameObject;
			dst.mesh = mesh;
			dst.face = face;
			dst.edge = edge;
			dst.vertex = vertex;
		}

		public override string ToString()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("GameObject: " + (gameObject != null ? gameObject.name : null));
			sb.AppendLine("ProBuilderMesh: " + (mesh != null ? mesh.name : null));
			sb.AppendLine("Face: " + (face != null ? face.ToString() : null));
			sb.AppendLine("Edge: " + edge.ToString());
			sb.AppendLine("Vertex: " + vertex);
			return sb.ToString();
		}

		public bool Equals(SceneSelection other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(gameObject, other.gameObject)
				&& Equals(mesh, other.mesh)
				&& vertex == other.vertex
				&& edge.Equals(other.edge)
				&& Equals(face, other.face);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((SceneSelection)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (gameObject != null ? gameObject.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (mesh != null ? mesh.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ vertex;
				hashCode = (hashCode * 397) ^ edge.GetHashCode();
				hashCode = (hashCode * 397) ^ (face != null ? face.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(SceneSelection left, SceneSelection right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SceneSelection left, SceneSelection right)
		{
			return !Equals(left, right);
		}
	}

	struct VertexPickerEntry
	{
		public ProBuilderMesh mesh;
		public int vertex;
		public float screenDistance;
		public Vector3 worldPosition;
	}

	static class EditorSceneViewPicker
	{
		static int s_DeepSelectionPrevious = 0x0;
		static SceneSelection s_Selection = new SceneSelection();
		static List<VertexPickerEntry> s_NearestVertexes = new List<VertexPickerEntry>();
		static List<GameObject> s_OverlappingGameObjects = new List<GameObject>();

		public static ProBuilderMesh DoMouseClick(Event evt, SelectMode selectionMode, ScenePickerPreferences pickerPreferences)
		{
			bool appendModifier = EditorHandleUtility.IsAppendModifier(evt.modifiers);

			if (!appendModifier)
				MeshSelection.SetSelection((GameObject) null);

			if (selectionMode == SelectMode.Edge)
			{
				if (!EdgeRaycast(evt.mousePosition, pickerPreferences, true, s_Selection))
					return null;
			}
			else if (selectionMode == SelectMode.Vertex)
			{
				if (!VertexRaycast(evt.mousePosition, pickerPreferences, true, s_Selection))
					return null;
			}
			else if (!FaceRaycast(evt.mousePosition, pickerPreferences, true, s_Selection, evt.clickCount > 1 ? -1 : 0, false))
			{
				return null;
			}

			evt.Use();

			MeshSelection.AddToSelection(s_Selection.gameObject);

			if (s_Selection.mesh != null)
			{
				var mesh = s_Selection.mesh;

				if (s_Selection.face != null)
				{
					// Check for other editor mouse shortcuts first (todo proper event handling for mouse shortcuts)
					MaterialEditor matEditor = MaterialEditor.instance;

					if (matEditor != null && matEditor.ClickShortcutCheck(Event.current.modifiers, mesh, s_Selection.face))
						return null;

					UVEditor uvEditor = UVEditor.instance;

					if (uvEditor != null && uvEditor.ClickShortcutCheck(mesh, s_Selection.face))
						return null;

					var ind = mesh.faces.IndexOf(s_Selection.face);
					var sel = mesh.selectedFaceIndexes.IndexOf(ind);

					UndoUtility.RecordSelection(mesh, "Select Face");

					if(sel > -1)
						mesh.RemoveFromFaceSelectionAtIndex(sel);
					else
						mesh.AddToFaceSelection(ind);
				}
				else if(s_Selection.edge != Edge.Empty)
				{
					int ind = mesh.selectedEdges.IndexOf(s_Selection.edge, mesh.sharedIndexesInternal.ToDictionary());

					UndoUtility.RecordSelection(mesh, "Select Edge");

					if (ind > -1)
						mesh.SetSelectedEdges(mesh.selectedEdges.ToArray().RemoveAt(ind));
					else
						mesh.SetSelectedEdges(mesh.selectedEdges.ToArray().Add(s_Selection.edge));
				}
				else if (s_Selection.vertex > -1)
				{
					int ind = Array.IndexOf(mesh.selectedIndexesInternal, s_Selection.vertex);

					UndoUtility.RecordSelection(mesh, "Select Vertex");

					if (ind > -1)
						mesh.SetSelectedVertexes(mesh.selectedIndexesInternal.RemoveAt(ind));
					else
						mesh.SetSelectedVertexes(mesh.selectedIndexesInternal.Add(s_Selection.vertex));
				}

				return mesh;
			}

			return null;
		}

		public static void DoMouseDrag(Rect mouseDragRect, SelectMode selectionMode, ScenePickerPreferences scenePickerPreferences)
		{
			var pickingOptions = new PickerOptions()
			{
				depthTest = scenePickerPreferences.cullMode == CullingMode.Back,
				rectSelectMode = scenePickerPreferences.rectSelectMode
			};

			var selection = MeshSelection.TopInternal();
			UndoUtility.RecordSelection(selection, "Drag Select");
			bool isAppendModifier = EditorHandleUtility.IsAppendModifier(Event.current.modifiers);

			if (!isAppendModifier)
				MeshSelection.ClearElementSelection();

			bool elementsInDragRect = false;

			switch (selectionMode)
			{
				case SelectMode.Vertex:
				{
					Dictionary<ProBuilderMesh, HashSet<int>> selected = Picking.PickVertexesInRect(
						SceneView.lastActiveSceneView.camera,
						mouseDragRect,
						selection,
						pickingOptions,
						EditorGUIUtility.pixelsPerPoint);

					foreach (var kvp in selected)
					{
						IntArray[] sharedIndexes = kvp.Key.sharedIndexesInternal;
						HashSet<int> common;

						if (isAppendModifier)
						{
							common = sharedIndexes.GetCommonIndexes(kvp.Key.selectedIndexesInternal);

							if (scenePickerPreferences.selectionModifierBehavior  == SelectionModifierBehavior.Add)
								common.UnionWith(kvp.Value);
							else if (scenePickerPreferences.selectionModifierBehavior  == SelectionModifierBehavior.Subtract)
								common.RemoveWhere(x => kvp.Value.Contains(x));
							else if (scenePickerPreferences.selectionModifierBehavior  == SelectionModifierBehavior.Difference)
								common.SymmetricExceptWith(kvp.Value);
						}
						else
						{
							common = kvp.Value;
						}

						elementsInDragRect = kvp.Value.Any();
						kvp.Key.SetSelectedVertexes(common.SelectMany(x => sharedIndexes[x].array));
					}

					break;
				}

				case SelectMode.Face:
				{
					Dictionary<ProBuilderMesh, HashSet<Face>> selected = Picking.PickFacesInRect(
						SceneView.lastActiveSceneView.camera,
						mouseDragRect,
						selection,
						pickingOptions,
						EditorGUIUtility.pixelsPerPoint);

					foreach (var kvp in selected)
					{
						HashSet<Face> current;

						if (isAppendModifier)
						{
							current = new HashSet<Face>(kvp.Key.selectedFacesInternal);

							if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Add)
								current.UnionWith(kvp.Value);
							else if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Subtract)
								current.RemoveWhere(x => kvp.Value.Contains(x));
							else if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Difference)
								current.SymmetricExceptWith(kvp.Value);
						}
						else
						{
							current = kvp.Value;
						}

						elementsInDragRect = kvp.Value.Any();
						kvp.Key.SetSelectedFaces(current);
					}

					break;
				}

				case SelectMode.Edge:
				{
					var selected = Picking.PickEdgesInRect(
						SceneView.lastActiveSceneView.camera,
						mouseDragRect,
						selection,
						pickingOptions,
						EditorGUIUtility.pixelsPerPoint);

					foreach (var kvp in selected)
					{
						ProBuilderMesh pb = kvp.Key;
						Dictionary<int, int> common = pb.sharedIndexesInternal.ToDictionary();
						HashSet<EdgeLookup> selectedEdges = EdgeLookup.GetEdgeLookupHashSet(kvp.Value, common);
						HashSet<EdgeLookup> current;

						if (isAppendModifier)
						{
							current = EdgeLookup.GetEdgeLookupHashSet(pb.selectedEdges, common);

							if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Add)
								current.UnionWith(selectedEdges);
							else if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Subtract)
								current.RemoveWhere(x => selectedEdges.Contains(x));
							else if (scenePickerPreferences.selectionModifierBehavior == SelectionModifierBehavior.Difference)
								current.SymmetricExceptWith(selectedEdges);
						}
						else
						{
							current = selectedEdges;
						}

						elementsInDragRect = kvp.Value.Any();
						pb.SetSelectedEdges(current.Select(x => x.local));
					}

					break;
				}
			}

			// if nothing was selected in the drag rect, clear the object selection too
			if(!elementsInDragRect && !isAppendModifier)
				MeshSelection.ClearElementAndObjectSelection();

			ProBuilderEditor.Refresh(false);
			SceneView.RepaintAll();
		}

		// Get the object & mesh selection that the mouse is currently nearest.
		// A ProBuilderMesh is returned because double click actions need to know what the last selected pb_Object was.
		// If deepClickOffset is specified, the object + deepClickOffset in the deep select stack will be returned (instead of next).
		internal static bool MouseRayHitTest(
			Vector3 mousePosition,
			SelectMode selectionMode,
			ScenePickerPreferences pickerOptions,
			SceneSelection selection,
			bool allowUnselected = false)
		{
			if (selectionMode == SelectMode.Edge)
				return EdgeRaycast(mousePosition, pickerOptions, allowUnselected, selection);

			if (selectionMode == SelectMode.Vertex)
				return VertexRaycast(mousePosition, pickerOptions, allowUnselected, selection);

			return FaceRaycast(mousePosition, pickerOptions, allowUnselected, selection, 0, true);
		}

		static bool FaceRaycast(Vector3 mousePosition,
			ScenePickerPreferences pickerOptions,
			bool allowUnselected,
			SceneSelection selection,
			int deepClickOffset = 0,
			bool isPreview = true)
		{
			GameObject pickedGo = null;
			ProBuilderMesh pickedPb = null;
			Face pickedFace = null;

			int newHash = 0;

			// If any event modifiers are engaged don't cycle the deep click
			EventModifiers em = Event.current.modifiers;

			if(isPreview || em != EventModifiers.None)
				EditorHandleUtility.GetHovered(mousePosition, s_OverlappingGameObjects);
			else
				EditorHandleUtility.GetAllOverlapping(mousePosition, s_OverlappingGameObjects);

			selection.Clear();

			for (int i = 0, next = 0, pickedCount = s_OverlappingGameObjects.Count; i < pickedCount; i++)
			{
				var go = s_OverlappingGameObjects[i];
				var mesh = go.GetComponent<ProBuilderMesh>();
				Face face = null;

				if (mesh != null && (allowUnselected || MeshSelection.TopInternal().Contains(mesh)))
				{
					Ray ray = UHandleUtility.GUIPointToWorldRay(mousePosition);
					RaycastHit hit;

					if (UnityEngine.ProBuilder.HandleUtility.FaceRaycast(ray,
						mesh,
						out hit,
						Mathf.Infinity,
						pickerOptions.cullMode))
					{
						face = mesh.facesInternal[hit.face];
					}
				}

				// pb_Face doesn't define GetHashCode, meaning it falls to object.GetHashCode (reference comparison)
				int hash = face == null ? go.GetHashCode() : face.GetHashCode();

				if (s_DeepSelectionPrevious == hash)
					next = (i + (1 + deepClickOffset)) % pickedCount;

				if (next == i)
				{
					pickedGo = go;
					pickedPb = mesh;
					pickedFace = face;

					newHash = hash;

					// a prior hash was matched, this is the next. if
					// it's just the first iteration don't break (but do
					// set the default).
					if (next != 0)
						break;
				}
			}

			if(!isPreview)
				s_DeepSelectionPrevious = newHash;

			if (pickedGo != null)
			{
				Event.current.Use();

				if (pickedPb != null)
				{
					if (pickedPb.isSelectable)
					{
						selection.gameObject = pickedGo;
						selection.mesh = pickedPb;
						selection.face = pickedFace;

						return true;
					}
				}

				if (!PreferencesInternal.GetBool(PreferenceKeys.pbPBOSelectionOnly))
				{
					// If clicked off a pb_Object but onto another gameobject, set the selection
					// and dip out.
					selection.gameObject = pickedGo;
					return true;
				}
			}

			return false;
		}

		static bool VertexRaycast(Vector3 mousePosition, ScenePickerPreferences pickerOptions, bool allowUnselected, SceneSelection selection)
		{
			Camera cam = SceneView.lastActiveSceneView.camera;
			selection.Clear();
			s_NearestVertexes.Clear();
			selection.gameObject = HandleUtility.PickGameObject(mousePosition, false);
			float maxDistance = pickerOptions.maxPointerDistance * pickerOptions.maxPointerDistance;

			if (selection.gameObject != null)
			{
				var mesh = selection.gameObject.GetComponent<ProBuilderMesh>();

				if (mesh != null && mesh.isSelectable)
				{
					if(MeshSelection.Top().Contains(mesh))
						GetNearestVertexes(mesh, mousePosition, s_NearestVertexes, maxDistance);
					else if (allowUnselected)
						s_NearestVertexes.Add(new VertexPickerEntry()
						{
							screenDistance = Mathf.Infinity,
							worldPosition = Vector3.zero,
							mesh = mesh,
							vertex = -1
						});
				}
			}

			if(selection.mesh == null)
			{
				foreach (var mesh in MeshSelection.Top())
				{
					if (!mesh.isSelectable)
						continue;
					GetNearestVertexes(mesh, mousePosition, s_NearestVertexes, maxDistance);
				}
			}

			s_NearestVertexes.Sort((x, y) => x.screenDistance.CompareTo(y.screenDistance));

			for (int i = 0; i < s_NearestVertexes.Count; i++)
			{
				if (s_NearestVertexes[i].vertex < 0)
				{
					selection.gameObject = s_NearestVertexes[i].mesh.gameObject;
					selection.mesh = s_NearestVertexes[i].mesh;
					selection.vertex = -1;
				}
				else if (!UnityEngine.ProBuilder.HandleUtility.PointIsOccluded(cam, s_NearestVertexes[i].mesh, s_NearestVertexes[i].worldPosition))
				{
					selection.gameObject = s_NearestVertexes[i].mesh.gameObject;
					selection.mesh = s_NearestVertexes[i].mesh;
					selection.vertex = s_NearestVertexes[i].vertex;
					return true;
				}
			}

			return selection.gameObject != null;
		}

		static void GetNearestVertexes(ProBuilderMesh mesh, Vector3 mousePosition, List<VertexPickerEntry> list, float maxDistance)
		{
			var positions = mesh.positionsInternal;
			var common = mesh.sharedIndexesInternal;

			for (int n = 0, c = common.Length; n < c; n++)
			{
				int index = common[n][0];
				Vector3 v = mesh.transform.TransformPoint(positions[index]);
				Vector3 p = UHandleUtility.WorldToGUIPoint(v);

				float dist = (p - mousePosition).sqrMagnitude;

				if (dist < maxDistance)
					list.Add(new VertexPickerEntry()
					{
						mesh = mesh,
						screenDistance = dist,
						worldPosition = v,
						vertex = index
					});
			}
		}

		static bool EdgeRaycast(Vector3 mousePosition, ScenePickerPreferences pickerPrefs, bool allowUnselected, SceneSelection selection)
		{
			selection.Clear();
			selection.gameObject = UHandleUtility.PickGameObject(mousePosition, false);
			var hoveredMesh = selection.gameObject != null ? selection.gameObject.GetComponent<ProBuilderMesh>() : null;

			float bestDistance = pickerPrefs.maxPointerDistance;
			bool hoveredIsInSelection = MeshSelection.TopInternal().Contains(hoveredMesh);

			if(hoveredMesh != null && (allowUnselected || hoveredIsInSelection))
			{
				var tup = GetNearestEdgeOnMesh(hoveredMesh, mousePosition);

				if (tup.edge.IsValid() && tup.distance < pickerPrefs.maxPointerDistance)
				{
					selection.gameObject = hoveredMesh.gameObject;
					selection.mesh = hoveredMesh;
					selection.edge = tup.edge;

					// if it's in the selection, it automatically wins as best. if not, treat this is a fallback.
					if (hoveredIsInSelection)
						return true;
				}
			}

			foreach (var mesh in MeshSelection.TopInternal())
			{
				var trs = mesh.transform;
				var positions = mesh.positionsInternal;

				foreach (var face in mesh.facesInternal)
				{
					foreach (var edge in face.edges)
					{
						int x = edge.a;
						int y = edge.b;

						float d = UHandleUtility.DistanceToLine(
							trs.TransformPoint(positions[x]),
							trs.TransformPoint(positions[y]));

						if (d < bestDistance)
						{
							selection.gameObject = mesh.gameObject;
							selection.mesh = mesh;
							selection.edge = new Edge(x, y);
							bestDistance = d;
						}
					}
				}
			}

			return selection.gameObject != null;
		}

		static SimpleTuple<Face, Vector3> s_DualCullModeRaycastBackFace = new SimpleTuple<Face, Vector3>();
		static SimpleTuple<Face, Vector3> s_DualCullModeRaycastFrontFace = new SimpleTuple<Face, Vector3>();

		struct EdgeAndDistance
		{
			public Edge edge;
			public float distance;
		}

		static EdgeAndDistance GetNearestEdgeOnMesh(ProBuilderMesh mesh, Vector3 mousePosition)
		{
			Ray ray = UHandleUtility.GUIPointToWorldRay(mousePosition);

			var res = new EdgeAndDistance()
			{
				edge = Edge.Empty,
				distance = Mathf.Infinity
			};

			// get the nearest hit face and point for both cull mode front and back, then prefer the result that is nearest the camera.
			if (PHandleUtility.FaceRaycastBothCullModes(ray, mesh, s_DualCullModeRaycastBackFace, s_DualCullModeRaycastFrontFace))
			{
				Vector3[] v = mesh.positionsInternal;

				if (s_DualCullModeRaycastBackFace.item1 != null)
				{
					foreach (var edge in s_DualCullModeRaycastBackFace.item1.edgesInternal)
					{
						float d = UHandleUtility.DistancePointLine(s_DualCullModeRaycastBackFace.item2, v[edge.a], v[edge.b]);

						if (d < res.distance)
						{
							res.edge = edge;
							res.distance = d;
						}
					}
				}

				if (s_DualCullModeRaycastFrontFace.item1 != null)
				{
					var a = mesh.transform.TransformPoint(s_DualCullModeRaycastBackFace.item2);
					var b = mesh.transform.TransformPoint(s_DualCullModeRaycastFrontFace.item2);
					var c = SceneView.lastActiveSceneView.camera.transform.position;

					if (Vector3.Distance(c, b) < Vector3.Distance(c, a))
					{
						foreach (var edge in s_DualCullModeRaycastFrontFace.item1.edgesInternal)
						{
							float d = UHandleUtility.DistancePointLine(s_DualCullModeRaycastFrontFace.item2, v[edge.a], v[edge.b]);

							if (d < res.distance)
							{
								res.edge = edge;
								res.distance = d;
							}
						}
					}
				}

				if (res.edge.IsValid())
					res.distance = UHandleUtility.DistanceToLine(
						mesh.transform.TransformPoint(v[res.edge.a]),
						mesh.transform.TransformPoint(v[res.edge.b]));

			}

			return res;
		}
	}
}