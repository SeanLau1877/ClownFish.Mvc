using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClownFish.Mvc
{
	/// <summary>
	/// 线程安全的单例模式
	/// </summary>
	/// <typeparam name="T">数据的参数类型</typeparam>
	public sealed class SingletonObject<T> : Singleton<T> where T : class, new()
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		public SingletonObject()
			: base(TypeExtend.ObjectFactory.New<T>)
		{
		}
	}


	/// <summary>
	/// 线程安全的单例模式
	/// </summary>
	/// <typeparam name="T">数据的参数类型</typeparam>
	public class Singleton<T>
	{
		private Func<T> _getValue;
		private T _value;

		private Exception _exception;

		private bool _inited = false;
		private readonly object _lock = new object();
				

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="getValue">获取数据的委托</param>
		public Singleton(Func<T> getValue)
		{
			if( getValue == null )
				throw new ArgumentNullException("getValue");

			_getValue = getValue;
		}

		/// <summary>
		/// 返回缓存的单例结果
		/// </summary>
		public T Value
		{
			get
			{
				if( _inited == false ) {
					lock( _lock ) {
						if( _inited == false ) {
							try {
								// 获取数据
								_value = _getValue();
							}
							catch( Exception ex ) {
								_exception = ex;		// 先保存异常，防止第二次访问时没有异常提示。
							}
							finally {
								// 释放委托
								_getValue = null;

								// 设置初始化完成标记
								_inited = true;
							}
						}
					}
				}

				if( _exception != null )
					throw _exception;

				return _value;
			}
		}
	}
}
