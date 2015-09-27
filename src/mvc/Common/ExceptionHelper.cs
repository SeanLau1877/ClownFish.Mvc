using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ClownFish.Mvc
{
	internal static class ExceptionHelper
	{
		public static void Throw403Exception(HttpContext context)
		{
			if( context == null )
				throw new HttpException(403, "很抱歉，您没有合适的权限访问该资源。");

			throw new HttpException(403,
				"很抱歉，您没有合适的权限访问该资源：" + context.Request.RawUrl);
		}

		public static void Throw404Exception(HttpContext context)
		{
			if( context == null )
				throw new HttpException(404, "要请求的资源不存在。");

			throw new HttpException(404,
				"不能根据当前URL创建请求处理器，当前URL：" + context.Request.RawUrl);
		}
	}
}
