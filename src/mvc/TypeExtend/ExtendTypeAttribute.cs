using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClownFish.Mvc.TypeExtend
{
	/// <summary>
	/// 用于标记扩展类型的修饰属性
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ExtendTypeAttribute : System.Attribute
	{

	}
}
