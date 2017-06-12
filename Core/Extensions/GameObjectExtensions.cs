using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DTUIRebuildVisualizer.Internal {
	public static class GameObjectExtensions {
		public static T GetOrAddComponent<T>(this GameObject g) where T : UnityEngine.Component {
			T component = g.GetComponent<T>();

			if (component == null) {
				component = g.AddComponent<T>();
			}

			return component;
		}
	}
}
