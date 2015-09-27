using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClownFish.Mvc.Client
{
	/// <summary>
	/// 定义HttpClient的扩展方法的工具类
	/// </summary>
	public static class ClientExtensions
	{
		/// <summary>
		/// 根据指定的HttpRequestOption参数，用【同步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public static T Send<T>(this HttpOption option)
		{
			return HttpClient.Send<T>(option);
		}


		/// <summary>
		/// 根据指定的HttpRequestOption参数，用【异步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public static Task<T> SendAsync<T>(this HttpOption option)
		{
			return HttpClient.SendAsync<T>(option);
		}


	}
}
