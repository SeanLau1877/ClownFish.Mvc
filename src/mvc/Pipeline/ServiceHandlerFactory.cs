using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using ClownFish.Mvc.Reflection;
using ClownFish.Mvc.TypeExtend;
using ClownFish.Mvc.Debug404;


namespace ClownFish.Mvc
{
	/// <summary>
	/// 响应服务请求的HttpHandlerFactory。它要求将所有Action放在一个以Service结尾的类型中。
	/// </summary>
	public class ServiceHandlerFactory : BaseActionHandlerFactory
	{
		//private static readonly ControllerRecognizer s_recognizer = ObjectFactory.New<ControllerRecognizer>();

		private static readonly UrlParser s_UrlParser = ObjectFactory.New<UrlParser>();


		/// <summary>
		/// 解析URL，提取UrlActionInfo对象
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public override UrlActionInfo ParseUrl(HttpContext context, string path)
		{
			return s_UrlParser.GetUrlActionInfo(context, path);
		}


		

	}
}
