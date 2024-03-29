﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Reflection;
using System.Configuration;
using System.Web.Configuration;
using ClownFish.Mvc.OptimizeReflection;
using System.Collections;

namespace ClownFish.Mvc.Reflection
{
	internal static class ModelHelper
	{

		private static Hashtable s_modelTable = Hashtable.Synchronized(
											new Hashtable(4096, StringComparer.OrdinalIgnoreCase));

		/// <summary>
		/// 返回一个实体类型的描述信息（全部属性及字段）。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static ModelDescription GetModelDescription(Type type)
		{
			if( type == null )
				throw new ArgumentNullException("type");

			string key = type.FullName;
			ModelDescription mm = (ModelDescription)s_modelTable[key];

			if( mm == null ) {
				List<DataMember> list = new List<DataMember>();

				(from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				 select new PropertyMember(p)).ToList().ForEach(x => list.Add(x));

				(from f in type.GetFields(BindingFlags.Instance | BindingFlags.Public)
				 select new FieldMember(f)).ToList().ForEach(x => list.Add(x));

				mm = new ModelDescription { Fields = list.ToArray() };
				s_modelTable[key] = mm;
			}
			return mm;
		}

		///// <summary>
		///// 返回一个实体类型的指定名称的数据成员
		///// </summary>
		///// <param name="type"></param>
		///// <param name="name"></param>
		///// <returns></returns>
		//public static DataMember GetMemberByName(Type type, string name, bool ifNotFoundThrowException)
		//{
		//    ModelDescription description = GetModelDescription(type);
		//    foreach( DataMember member in description.Fields )
		//        if( member.Name == name )
		//            return member;

		//    if( ifNotFoundThrowException )
		//        throw new ArgumentOutOfRangeException(
		//                string.Format("指定的成员 {0} 在类型 {1} 中并不存在。", name, type.ToString()));

		//    return null;
		//}



		/// <summary>
		/// 根据HttpRequest填充一个数据实体。
		/// 这里不支持嵌套类型的数据实体，且要求各数据成员都是简单的数据类型。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="model"></param>
		/// <param name="paramName"></param>
		public static void FillModel(HttpContext context, object model, string paramName)
		{
			ModelDescription descripton = GetModelDescription(model.GetType());

			object val = null;
			foreach( DataMember field in descripton.Fields ) {
				if( field.Ignore )
					continue;

				// 这里的实现方式不支持嵌套类型的数据实体。
				// 如果有这方面的需求，可以将这里改成递归的嵌套调用。

				val = GetValueByNameAndTypeFrommRequest(
									context, field.Name, field.Type.GetRealType(), paramName);
				if( val != null )
					field.SetValue(model, val);
			}
		}


		/// <summary>
		/// 读取一个HTTP参数值。这里只读取QueryString以及Form
		/// </summary>
		/// <param name="context"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private static string[] GetHttpValues(HttpContext context, string name)
		{
			string[] val = context.Request.QueryString.GetValues(name);

			if( val == null )
				val = context.Request.Form.GetValues(name);

			if( val == null ) {
				ActionHandler hanlder = context.Handler as ActionHandler;
				if( hanlder != null  ) {

					// 尝试从 PageRegexUrlAttribute 中读取正则表达式的匹配结果。
					if( hanlder.InvokeInfo.RegexMatch != null ) {
						string m = hanlder.InvokeInfo.RegexMatch.Groups[name].Value;
						if( m != null )
							val = new string[1] { m };
					}
					// 尝试从提取到的UrlActionInfo中获取。
					else if( hanlder.InvokeInfo.UrlActionInfo != null && hanlder.InvokeInfo.UrlActionInfo.Params != null ) {
						val = hanlder.InvokeInfo.UrlActionInfo.Params.GetValues(name);
					}
				}
			}

			return val;
		}
		//public static string GetHttpValue(HttpRequest request, string name)
		//{
		//    string val = request.QueryString[name];
		//    if( val == null )
		//        val = request.Form[name];

		//    return val;
		//}

		private static string[] GetValueFromHttpRequest(HttpContext context, string name, string parentName)
		{
			string[] val = GetHttpValues(context, name);
			if( val == null ) {
				// 再试一次。有可能是多个自定义类型，Form表单元素采用变量名做为前缀。
				if( string.IsNullOrEmpty(parentName) == false ) {
					val = GetHttpValues(context, parentName + "." + name);
				}
			}
			return val;
		}
		
		public static object GetValueByNameAndTypeFrommRequest(
							HttpContext context, string name, Type type, string parentName)
		{
			string[] val = GetValueFromHttpRequest(context, name, parentName);

			if( type == typeof(string[]) )
				return val;

			if( val == null || val.Length == 0 ) 
				return null;

			// 还原ASP.NET的默认数据格式
			string str = val.Length == 1 ? val[0] : string.Join(",", val);

			try {
				return UnSafeChangeType(str.Trim(), type);
			}
			catch( Exception ex ) {
				throw new InvalidCastException("数据转换失败，当前参数名：" +
							(string.IsNullOrEmpty(parentName) ? name : parentName + "." + name), ex);
			}
		}

		private static readonly char[] s_stringSlitArray = new char[] { ',' };

		public static object UnSafeChangeType(string value, Type conversionType)
		{
			// 注意：这个方法应该与ReflectionExtensions2.IsSupportableType保持一致性。
			// 如果 conversionType.IsSupportableType() 返回 true，这里应该能转换，除非字符串的格式不正确。

			if( conversionType == typeof(string) )
				return value;

			if( value == null || value.Length == 0 ) {
				// 空字符串根本不能做任何转换，所以直接返回null
				return null;
			}

			if( conversionType == typeof(string[]) )
				return value.Split(s_stringSlitArray, StringSplitOptions.RemoveEmptyEntries);	// 保持与NameValueCollection的行为一致。


			if( conversionType == typeof(Guid) )
				return new Guid(value);

			if( conversionType.IsEnum )
				return Enum.Parse(conversionType, value);

			// 尝试使用类型的隐式转换（从字符串转换）
			if( conversionType.IsSupportableType() == false ) {
				MethodInfo stringImplicit = GetStringImplicit(conversionType);
				if( stringImplicit != null )
					return stringImplicit.FastInvoke(null, value);
			}


			// 为了简单，直接调用 .net framework中的方法。
			// 如果转换失败，将会抛出异常。
			return Convert.ChangeType(value, conversionType);
		}


		/// <summary>
		/// 判断指定的类型是否能从String类型做隐式类型转换，如果可以，则返回相应的方法
		/// </summary>
		/// <param name="conversionType"></param>
		/// <returns></returns>
		private static MethodInfo GetStringImplicit(Type conversionType)
		{
			MethodInfo m = conversionType.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public);

			if( m != null && m.IsStatic && m.IsSpecialName && m.ReturnType == conversionType ) {
				ParameterInfo[] paras = m.GetParameters();
				if( paras.Length == 1 && paras[0].ParameterType == typeof(string) )
					return m;
			}

			return null;
		}


	}


	
}
