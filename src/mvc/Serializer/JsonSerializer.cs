using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClownFish.Mvc.Serializer;

namespace ClownFish.Mvc
{


//警告：如果需要修改JSON序列化方式，需要完成2件事件：
//    1. 重新实现JsonSerializer定义的虚方法
//    2. 重新实现ActionParametersProviderFactory


	/// <summary>
	/// JSON序列化包装类
	/// </summary>
	public class JsonSerializer
	{
		/// <summary>
		/// 将一个对象序列化为JSON字符串。
		/// </summary>
		/// <param name="obj">要序列化的对象</param>
		/// <param name="keepTypeInfo">尽量在序列化过程中保留类型信息（Newtonsoft.Json可支持）</param>
		/// <returns>序列化得到的JSON字符串</returns>
		public virtual string ToJson(object obj, bool keepTypeInfo)
		{
			if( obj == null )
				throw new ArgumentNullException("obj");

			return NewtonsoftJsonDataProvider.Serialize(obj, keepTypeInfo);

			//JavaScriptSerializer jss = new JavaScriptSerializer();
			//return jss.Serialize(obj);
		}


		/// <summary>
		/// 将一个JSON字符串反序列化为对象
		/// </summary>
		/// <typeparam name="T">对象的类型参数</typeparam>
		/// <param name="json">JSON字符串</param>
		/// <returns>反序列化得到的结果</returns>
		public virtual T FromJson<T>(string json)
		{
			if( string.IsNullOrEmpty(json) )
				throw new ArgumentNullException("json");

			return NewtonsoftJsonDataProvider.Deserialize<T>(json);

			//JavaScriptSerializer jss = new JavaScriptSerializer();
			//return jss.Deserialize<T>(json);	
		}
	}
}
