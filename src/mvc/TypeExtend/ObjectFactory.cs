using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClownFish.Mvc.OptimizeReflection;

namespace ClownFish.Mvc.TypeExtend
{
	/// <summary>
	/// 创建扩展对象的工厂类型
	/// </summary>
	public static class ObjectFactory
	{
		/// <summary>
		/// 尝试创建指定类型的扩展类
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T New<T>() where T : class, new()
		{
			return (T)New(typeof(T));
		}


		/// <summary>
		/// 尝试创建指定类型的扩展类，并且允许订阅实例事件。
		/// 注意：指定的类型必须包含无参数的构造方法。
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static object New(this Type t)
		{
			if( t == null )
				throw new ArgumentNullException("t");

			Type extType =  ExtendManager.GetExtendType(t);


			object instance = (extType == null)					
							? t.FastNew()			// 没有扩展类型，就直接使用原类型					
							: extType.FastNew();	// 有扩展类型就创建扩展类型的实例

			// 供扩展类型订阅事件
			BaseEventObject obj = instance as BaseEventObject;
			if( obj != null )
				obj.SubscribeEvent();

			return instance;
		}
	}
}
