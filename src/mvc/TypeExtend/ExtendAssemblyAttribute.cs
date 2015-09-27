using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClownFish.Mvc.TypeExtend
{
	/// <summary>
	/// 标记扩展程序集的修饰属性
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public sealed class ExtendAssemblyAttribute : System.Attribute
	{
		// 目前仅用作标记，暂时没有数据成员。
		// 未来可以添加第三方的识别信息
	}
}
