using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using DTUIRebuildVisualizer.Internal;

namespace DTUIRebuildVisualizer {
	public class UIRebuildVisualizerView : MonoBehaviour {
		// PRAGMA MARK - Internal
		[SerializeField]
		private Toggle uiRebuildToggle_;

		void OnEnable() {
			if (UIRebuildVisualizer.Instance == null) {
				Debug.LogWarning("Cannot use UIRebuildVisualizerView without an instance of UIRebuildVisualizer inside the scene!");
				this.enabled = false;
				return;
			}

			uiRebuildToggle_.isOn = UIRebuildVisualizer.Instance.Enabled;
			uiRebuildToggle_.onValueChanged.AddListener(HandleUIRebuildToggleValueChanged);
		}

		void OnDisable() {
			uiRebuildToggle_.onValueChanged.RemoveListener(HandleUIRebuildToggleValueChanged);
		}

		private void HandleUIRebuildToggleValueChanged(bool value) {
			UIRebuildVisualizer.Instance.Enabled = value;
		}
	}
}