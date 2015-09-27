using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Threading;

namespace ClownFish.Mvc
{
#if DEBUG

	/// <summary>
	/// 提供DEBUG信息输出的工具类
	/// </summary>
	public static class DebugHelper
	{
		/// <summary>
		/// 将DEBUG信息写到响应头，格式："ThreadId, text"
		/// </summary>
		/// <param name="context"></param>
		/// <param name="text"></param>
		public static void WriteHeader(this HttpContext context, string text)
		{
			if( context == null )
				throw new ArgumentNullException("context");

			if( context.IsDebuggingEnabled == false )
				return;

			object currentIndex = context.Items["DebugHelper-WriteHeader-Index"];
			if( currentIndex == null)
				currentIndex = 100;
			

			int index = (int)currentIndex + 1;
			context.Items["DebugHelper-WriteHeader-Index"] = index;

			string name = "debug-info-" + index.ToString();

			string value = string.Format("ThreadId: {0}, {1}", Thread.CurrentThread.ManagedThreadId, text);

			try {
				context.Response.Headers.Add(name, value);
			}
			catch { }
		}


	}
#endif

}
