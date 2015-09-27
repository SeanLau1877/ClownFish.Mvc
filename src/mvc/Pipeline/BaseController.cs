using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClownFish.Mvc
{
	/// <summary>
	/// 所有控制器的基类
	/// </summary>
	public abstract class BaseController
	{
		/// <summary>
		/// HTTP上下文相关对象（HttpContextBase的实例）
		/// </summary>
		public HttpContextBase HttpContext { get; internal set; }


		/// <summary>
		/// 获取 MvcRuntime 实例的引用
		/// </summary>
		public MvcRuntime MvcRuntime
		{
			get { return MvcRuntime.Instance; }
		}


		/// <summary>
		/// 从当前请求中读取Cookie
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public HttpCookie GetCookie(string name)
		{
			return this.HttpContext.Request.Cookies[name];
		}

		/// <summary>
		/// 写入一个Cookie到当前响应输出
		/// </summary>
		/// <param name="cookie"></param>
		public void AddCookie(HttpCookie cookie)
		{
			this.HttpContext.Response.Cookies.Add(cookie);
		}


	}
}
