using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit {
	/// <summary>
	/// Singletonなクラスを作るためのヘルパ。
	/// </summary>
	public static class Singleton<T> where T : class {
		static T instance = null;
		static object lockObject = new object();

		public delegate T Factory();

		/// <summary>
		/// インスタンスを取得する。
		/// </summary>
		public static T GetInstance(Factory factory) {
			// double checked locking
			if (instance == null) {
				lock (lockObject) {
					if (instance == null) {
						instance = factory();
					}
				}
			}
			return instance;
		}
	}
}
