using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ClownFish.Mvc.Reflection;
using System.Collections;

namespace ClownFish.Mvc.TypeExtend
{
	/// <summary>
	/// 用于管理扩展类型的工具类
	/// </summary>
	internal static class ExtendManager
	{
		private static bool s_inited = false;
		private static readonly object s_lock = new object();

		/// <summary>
		/// 类型与继承类型的映射字典
		/// </summary>
		private static Dictionary<Type, Type> s_typeMapDict;

		/// <summary>
		/// 保存事件映射字典
		/// </summary>
		private static Dictionary<Type, List<Type>> s_eventDict;

		private static Exception s_initException;



		internal static void Init()
		{
			if( s_inited == false ) {
				lock( s_lock ) {
					if( s_inited == false ) {

						try {
							SearchAllExtend();
						}
						catch( Exception ex ) {
							// 这里吃掉异常是为了在后续调用中持续抛出。
							s_initException = ex;
						}
						finally {
							s_inited = true;
						}
					}
				}
			}


			if( s_initException != null )
				throw s_initException;
		}




		private static void SearchAllExtend()
		{
			Dictionary<Type, Type> dict1 = new Dictionary<Type, Type>(1024);
			Dictionary<Type, List<Type>> dict2 = new Dictionary<Type, List<Type>>(1024);

			List<Assembly> list = ReflectionHelper.GetAssemblyList<ExtendAssemblyAttribute>();
			
			foreach( Assembly assembly in list ) {

				// 原本打算通过约定程序集命名方式要求明确扩展程序集，便于形成开发规范以及方便框架识别。
				// 后来决定改成使用ExtendAssemblyAttribute的方式来识别，留给项目一定的个性化发挥空间。
				// 如果项目需要采用命名规范方式，可以取消下面二行代码的注释。

				//if( assembly.Location.EndsWith(".Extend.dll", StringComparison.OrdinalIgnoreCase) == false )
				//    continue;

				
				foreach( Type extType in assembly.GetExportedTypes() ) {
					if( extType.IsClass == false || extType.IsAbstract )
						continue;

					// 判断是不是【类型拦截】的扩展类型
					ExtendTypeAttribute extAttr = extType.GetMyAttribute<ExtendTypeAttribute>();
					if( extAttr != null ) {
						try {
							dict1.Add(extType.BaseType, extType);
						}
						catch( ArgumentException ex ) {
							throw new InvalidProgramException(string.Format("不能对类型 {0} 多次继承扩展，请考虑合并它们。",
								extType.BaseType.FullName), ex);
						}
					}

					// 判断是不是【事件订阅】的扩展类型
					else {
						// 检查是不是从 EventSubscriber<> 继承（这里不考虑多次继承）
						Type argumentType = extType.BaseType.GetArgumentType(typeof(EventSubscriber<>));
						if( argumentType != null ) {

							// 只接受无参的构造函数，因为不可能传入其它参数
							ConstructorInfo ctor = argumentType.GetConstructor(Type.EmptyTypes);
							if( ctor == null )
								continue;

							// 由于一个实例的允许有多个扩展事件订阅类型，所以使用一个列表来记录关联关系
							List<Type> types = null;
							if( dict2.TryGetValue(extType, out types) == false ) {
								// 如果关联列表不存在，就创建
								types = new List<Type>(2);
								dict2[argumentType] = types;
							}

							types.Add(extType);	
						}
					}
				}
			}

			s_typeMapDict = dict1;
			s_eventDict = dict2;
		}





		internal static Type GetExtendType(Type srcType)
		{
			Init();


			Type extType = null;
			s_typeMapDict.TryGetValue(srcType, out extType);
			return extType;
		}


		internal static List<Type> GetEventSubscribers(Type srcType)
		{
			Init();


			List<Type> types = null;
			s_eventDict.TryGetValue(srcType, out types);
			return types;
		}


	}
}
