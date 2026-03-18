using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit {
	/// <summary>
	/// std::pairみたいな。
	/// </summary>
	/// <typeparam name="FirstType">型その1</typeparam>
	/// <typeparam name="SecondType">型その2</typeparam>
	public struct Pair<FirstType, SecondType> {
		/// <summary>
		/// 1個目の。
		/// </summary>
		public FirstType First;
		/// <summary>
		/// 2個目の。
		/// </summary>
		public SecondType Second;

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="f">1個目の。</param>
		/// <param name="s">2個目の。</param>
		public Pair(FirstType f, SecondType s) {
			First = f;
			Second = s;
		}
	}
}
