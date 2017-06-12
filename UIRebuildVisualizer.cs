using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

using DTUIRebuildVisualizer.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DTUIRebuildVisualizer {
	public class UIRebuildVisualizer : MonoBehaviour {
		// PRAGMA MARK - Static
		public static UIRebuildVisualizer Instance {
			get; private set;
		}


		// PRAGMA MARK - Public Interface
		public bool Enabled {
			get { return visualizationEnabled_; }
			set {
				if (visualizationEnabled_ == value) {
					return;
				}

				visualizationEnabled_ = value;
				if (visualizationEnabled_) {
					StartVisualization();
				} else {
					StopVisualization();
				}
			}
		}


		// PRAGMA MARK - Static Internal
		private const float kCanvasRebuildingAlpha = 0.5f;

		[Header("Properties")]
		[SerializeField]
		private KeyCode toggleKey_ = KeyCode.V;

		private IList<ICanvasElement> graphicRebuildQueue_ = null;
		private IList<ICanvasElement> GraphicRebuildQueue_ {
			get {
				if (graphicRebuildQueue_ == null) {
					FieldInfo fGraphic = typeof(CanvasUpdateRegistry).GetField("m_GraphicRebuildQueue", BindingFlags.Instance | BindingFlags.NonPublic);
					graphicRebuildQueue_ = (IList<ICanvasElement>)fGraphic.GetValue(CanvasUpdateRegistry.instance);
				}
				return graphicRebuildQueue_;
			}
		}

		private readonly Dictionary<GameObject, CanvasGroup> canvasGroupMapping_ = new Dictionary<GameObject, CanvasGroup>();
		private readonly Dictionary<CanvasGroup, float> canvasGroupLastDirtyTime_ = new Dictionary<CanvasGroup, float>();
		private readonly Dictionary<GameObject, float> gameObjectLastDirtyTime_ = new Dictionary<GameObject, float>();

		private float lastUpdateTime_;
		private bool visualizationEnabled_ = false;

		private void Awake() {
			Instance = this;
			GameObject.DontDestroyOnLoad(this);
		}

		private void StartVisualization() {
			// HACK (darren): add event handler to front of willRenderCanvases
			// so my callback always happens before the queues are cleared
			var fieldInfo = typeof(Canvas).GetField("willRenderCanvases", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			MulticastDelegate eventDelegate = (MulticastDelegate)fieldInfo.GetValue(null);
			if (eventDelegate != null) {
				var delegates = eventDelegate.GetInvocationList();
				foreach (var handler in delegates) {
					Canvas.willRenderCanvases -= (Canvas.WillRenderCanvases)handler;
				}

				Canvas.willRenderCanvases += HandleWillRenderCanvases;
				foreach (var handler in delegates) {
					Canvas.willRenderCanvases += (Canvas.WillRenderCanvases)handler;
				}
			}

			#if UNITY_EDITOR
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
			#endif
		}

		private void StopVisualization() {
			foreach (var canvasGroup in canvasGroupLastDirtyTime_.Keys) {
				if (canvasGroup == null) {
					continue;
				}

				canvasGroup.alpha = 1.0f;
			}
			canvasGroupLastDirtyTime_.Clear();

			Canvas.willRenderCanvases -= HandleWillRenderCanvases;

			#if UNITY_EDITOR
			EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
			#endif

			visualizationEnabled_ = false;
		}

		private void Update() {
			UpdateToggle();
			UpdateVisualization();
		}

		private void UpdateToggle() {
			if (Input.GetKeyDown(toggleKey_)) {
				Enabled = !Enabled;
			}
		}

		private void UpdateVisualization() {
			if (!visualizationEnabled_) {
				return;
			}

			float now = Time.realtimeSinceStartup;
			foreach (var kvp in canvasGroupLastDirtyTime_) {
				var canvasGroup = kvp.Key;
				float lastDirtyTime = kvp.Value;

				if (canvasGroup == null) {
					continue;
				}

				if (lastDirtyTime >= lastUpdateTime_ - Mathf.Epsilon) {
					canvasGroup.alpha = kCanvasRebuildingAlpha;
				} else {
					canvasGroup.alpha = 1.0f;
				}
			}

			lastUpdateTime_ = now;
		}

		private void HandleWillRenderCanvases() {
			float now = Time.realtimeSinceStartup;

			for (int i = 0; i < GraphicRebuildQueue_.Count; i++) {
				ICanvasElement elem = GraphicRebuildQueue_[i];

				Graphic graphic = elem as Graphic;
				if (graphic == null) {
					Debug.LogError("Don't know how to handle other types beyond Graphic!");
					continue;
				}

				Canvas canvas = graphic.canvas;
				if (canvas == null) {
					// NOTE (darren): this is a valid case
					// because sometimes the canvas can be destroyed
					continue;
				}

				var canvasGroup = canvasGroupMapping_.GetOrCreateCached(canvas.gameObject, g => g.GetOrAddComponent<CanvasGroup>());

				canvasGroupLastDirtyTime_[canvasGroup] = now;
				gameObjectLastDirtyTime_[graphic.gameObject] = now;
			}
		}

		#if UNITY_EDITOR
		private void HandleHierarchyWindowItemOnGUI(int guid, Rect drawRect) {
			if (!Application.isPlaying) {
				EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
				return;
			}

			GameObject g = EditorUtility.InstanceIDToObject(guid) as GameObject;
			if (g == null || !gameObjectLastDirtyTime_.ContainsKey(g)) {
				return;
			}

			Color previousBackgroundColor = GUI.backgroundColor;

			float lastDirtyTime = gameObjectLastDirtyTime_[g];
			if (lastDirtyTime >= lastUpdateTime_ - Mathf.Epsilon - 0.5f) {
				GUI.backgroundColor = Color.cyan.WithAlpha(0.3f);
				GUI.Box(drawRect, "");
				EditorApplication.RepaintHierarchyWindow();
			}

			GUI.backgroundColor = previousBackgroundColor;
		}
		#endif
	}
}