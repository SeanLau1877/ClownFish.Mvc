using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClownFish.Mvc.Serializer;


namespace ClownFish.Mvc
{
	/// <summary>
	/// JSON序列化的工具类
	/// </summary>
	public static class JsonHelper
	{
		/// <summary>
		/// 将一个对象序列化为JSON字符串。
		/// </summary>
		/// <param name="obj">要序列化的对象</param>
		/// <returns>序列化得到的JSON字符串</returns>
		public static string ToJson(this object obj)
		{
			JsonSerializer serializer = TypeExtend.ObjectFactory.New<JsonSerializer>();
			return serializer.ToJson(obj, false);
		}

		/// <summary>
		/// 将一个对象序列化为JSON字符串。
		/// </summary>
		/// <param name="obj">要序列化的对象</param>
		/// <param name="keepTypeInfo">尽量在序列化过程中保留类型信息（Newtonsoft.Json可支持）</param>
		/// <returns>序列化得到的JSON字符串</returns>
		public static string ToJson(this object obj, bool keepTypeInfo)
		{
			JsonSerializer serializer = TypeExtend.ObjectFactory.New<JsonSerializer>();
			return serializer.ToJson(obj, keepTypeInfo);
		}


		/// <summary>
		/// 将一个JSON字符串反序列化为对象
		/// </summary>
		/// <typeparam name="T">对象的类型参数</typeparam>
		/// <param name="json">JSON字符串</param>
		/// <returns>反序列化得到的结果</returns>
		public static T FromJson<T>(this string json)
		{
			JsonSerializer serializer = TypeExtend.ObjectFactory.New<JsonSerializer>();
			return serializer.FromJson<T>(json);
		}




	}
}
