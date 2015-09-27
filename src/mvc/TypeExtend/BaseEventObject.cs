using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ClownFish.Mvc.OptimizeReflection;

namespace ClownFish.Mvc.TypeExtend
{
	/// <summary>
	/// 可用于外部事件订阅的基类，继承这个类型后，就可以从其它类型中订阅当前类型的实例事件。
	/// </summary>
	public abstract class BaseEventObject
	{
		internal void SubscribeEvent()
		{
			List<Type> types = ExtendManager.GetEventSubscribers(this.GetType());
			if( types == null )
				return;

			foreach( Type t in types ) {
				object subscriber = t.FastNew();
				MethodInfo method = t.GetMethod("SubscribeEvent", BindingFlags.Public | BindingFlags.Instance);
				if( method  != null )
					method.FastInvoke(subscriber, this);
			}
		}
	}


	/// <summary>
	/// 用于订阅BaseEventObject派生类型事件的基类，
	/// 如果要订阅BaseEventObject派生类型事件，必须继承此类型
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class EventSubscriber<T>
	{
		/// <summary>
		/// 订阅事件
		/// </summary>
		/// <param name="instance"></param>
		public abstract void SubscribeEvent(T instance);
	}


}
