using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DTUIRebuildVisualizer.Internal {
	public static class DictionaryExtensions {
		public static V GetOrCreateCached<U, V>(this IDictionary<U, V> source, U key, Func<U, V> valueCreator) {
			if (!source.ContainsKey(key)) {
				source[key] = valueCreator.Invoke(key);
			}
			return source[key];
		}
	}
}