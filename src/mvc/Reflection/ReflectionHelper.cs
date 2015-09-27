using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;

namespace ClownFish.Mvc.Reflection
{
	internal static class ReflectionHelper
	{
		internal static readonly Type VoidType = typeof(void);


		/// <summary>
		/// 获取当前程序加载的所有程序集
		/// </summary>
		/// <returns></returns>
		internal static ICollection GetReferencedAssemblies()
		{
			if( WebConfig.IsAspnetApp )
				return System.Web.Compilation.BuildManager.GetReferencedAssemblies();
			else
				return AppDomain.CurrentDomain.GetAssemblies();
		}


		internal static List<Assembly> GetAssemblyList<T>() where T : Attribute
		{
			List<Assembly> list = new List<Assembly>(128);

			ICollection assemblies = GetReferencedAssemblies();
			foreach( Assembly assembly in assemblies ) {
				// 过滤以【System】开头的程序集，加快速度
				if( assembly.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase) )
					continue;

				if( assembly.GetCustomAttributes(typeof(T), false).Length == 0 )
					continue;

				list.Add(assembly);
			}

			return list;
		}


		internal static T GetMyAttribute<T>(this MemberInfo m, bool inherit) where T : Attribute
		{
			T[] array = m.GetCustomAttributes(typeof(T), inherit) as T[];

			if( array.Length == 1 )
				return array[0];

			if( array.Length > 1 )
				throw new InvalidProgramException(string.Format("方法 {0} 不能同时指定多次 [{1}]。", m.Name, typeof(T)));

			return default(T);
		}

		internal static T GetMyAttribute<T>(this MemberInfo m) where T : Attribute
		{
			return GetMyAttribute<T>(m, false);
		}


		internal static T[] GetMyAttributes<T>(this MemberInfo m, bool inherit) where T : Attribute
		{
			return m.GetCustomAttributes(typeof(T), inherit) as T[];
		}

		internal static T[] GetMyAttributes<T>(this MemberInfo m) where T : Attribute
		{
			return m.GetCustomAttributes(typeof(T), false) as T[];
		}


		/// <summary>
		/// 判断是否是一个可支持的参数类型。仅包括：基元类型，string ，decimal，DateTime，Guid, string[], 枚举
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsSupportableType(this Type type)
		{
			return type.IsPrimitive
				|| type == typeof(string)
				|| type == typeof(DateTime)
				|| type == typeof(decimal)
				|| type == typeof(Guid)
				|| type.IsEnum
				|| type == typeof(string[]);
		}

		/// <summary>
		/// 得到一个实际的类型（排除Nullable类型的影响）。比如：int? 最后将得到int
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Type GetRealType(this Type type)
		{
			if( type.IsGenericType )
				return Nullable.GetUnderlyingType(type) ?? type;
			else
				return type;
		}


		public static bool IsNullableType(this Type nullableType)
		{
			if( nullableType.IsGenericType 
				&& nullableType.IsGenericTypeDefinition == false 
				&& nullableType.GetGenericTypeDefinition() == typeof(Nullable<>) )
				return true;

			return false;
		}


		public static bool IsCompatible(this Type t, Type convertToType)
		{
			if( t == convertToType )
				return true;

			if( convertToType.IsInterface )
				return convertToType.IsAssignableFrom(t);
			else
				return t.IsSubclassOf(convertToType);
		}

		public static bool HasReturn(this MethodInfo m)
		{
			return m.ReturnType != typeof(void);
		}


		public static string GetCodeString(this Type t)
		{
			if( t == null )
				throw new ArgumentNullException("t");

			if( t.IsGenericType ) {
				StringBuilder sb = new StringBuilder(128);

				string genericTypeDefinition = t.GetGenericTypeDefinition().FullName;
				int p = genericTypeDefinition.IndexOf("`");
				if( p > 0 )
					sb.Append(genericTypeDefinition.Substring(0, p));
				else
					throw new InvalidOperationException();

				sb.Append("<");

				int index = 0;
				foreach( Type gt in t.GetGenericArguments() ) {
					if( index++ > 0 )
						sb.Append(",");
					sb.Append(GetCodeString(gt));
				}

				sb.Append(">");

				return sb.ToString();
			}
			else {
				if( t.HasElementType )
					return t.GetElementType().FullName;
				else
					return t.FullName.Replace('+', '.');
			}
		}


		public static Type GetArgumentType(this Type type, Type baseTypeDefinition)
		{
			if( type == null )
				throw new ArgumentNullException("type");

			if( baseTypeDefinition == null )
				throw new ArgumentNullException("baseTypeDefinition");

			if( baseTypeDefinition.IsGenericTypeDefinition == false )
				throw new ArgumentException("参数必须是一个泛型定义。", "baseTypeDefinition");


			if( type.IsGenericType && type.GetGenericTypeDefinition() == baseTypeDefinition )
				return type.GetGenericArguments()[0];

			//Type baseType = type;
			//while( baseType != typeof(object) ) {
			//    if( baseType.IsGenericType && baseType.GetGenericTypeDefinition() == baseTypeDefinition ) {
			//        return baseType.GetGenericArguments()[0];
			//    }
			//    else
			//        baseType = baseType.BaseType;
			//}

			return null;
		}


		/// <summary>
		/// 判断是不是一个 Task 方法
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool IsTaskMethod(this MethodInfo method)
		{
			if( method.ReturnType == typeof(Task) )
				return true;

			if( method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) )
				return true;


			return false;
		}


		/// <summary>
		/// 检查是不是Task&lt;T&gt;方法，如果是，则返回类型参数T，否则返回 null
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static Type GetTaskMethodResultType(this MethodInfo method)
		{
			Type type = method.ReturnType;

			if( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) )
				return type.GetGenericArguments()[0];


			return null;
		}

	}
}
