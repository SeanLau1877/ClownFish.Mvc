using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace ClownFish.Mvc
{
	/// <summary>
	/// 包含流的Action执行结果，通常用于实现文件下载。
	/// </summary>
	public sealed class StreamResult : IActionResult
	{
		private byte[] _buffer;
		private string _contentType;
		private string _filename;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="buffer">文件内容的字节数组</param>
		public StreamResult(byte[] buffer)
			: this(buffer, null)
		{
		}
		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="buffer">文件内容的字节数组</param>
		/// <param name="contentType">文档类型，允许为空</param>
		public StreamResult(byte[] buffer, string contentType)
			: this(buffer, contentType, null)
		{
		}
		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="buffer">文件内容的字节数组</param>
		/// <param name="contentType">文档类型，允许为空</param>
		/// <param name="filename">下载对话框显示的文件名</param>
		public StreamResult(byte[] buffer, string contentType, string filename)
		{
			if( buffer == null || buffer.Length == 0 )
				throw new ArgumentNullException("buffer");

			_buffer = buffer;
			_filename = filename;


			if( string.IsNullOrEmpty(contentType) )
				_contentType = "application/octet-stream";
			else
				_contentType = contentType;
		}

		/// <summary>
		/// 实现IActionResult接口，执行输出
		/// </summary>
		/// <param name="context"></param>
		public void Ouput(HttpContext context)
		{
			context.Response.ContentType = _contentType;

			if( string.IsNullOrEmpty(_filename) == false ) {

				string headerValue = GetFileNameHeader(context, _filename);
				context.Response.AddHeader("Content-Disposition", headerValue);
			}

			context.Response.OutputStream.Write(_buffer, 0, _buffer.Length);
		}


		/// <summary>
		/// 根据指定的文件名，按照HTTP相关规范计算用于响应头可以接受的字符串
		/// </summary>
		/// <param name="context"></param>
		/// <param name="filename"></param>
		/// <returns></returns>
		private string GetFileNameHeader(HttpContext context, string filename)
		{
			// 参考：
			// http://www.iefans.net/xiazai-wenjian-http-bianma-content-disposition/

			// Safari 不识别下面的编码方式
			//string headerValue = "attachment; filename*=UTF-8''" + HttpUtility.UrlEncode(filename);

			string headerValue = null;

			if( context.Request.Browser.Browser == "IE" )		// 老版本IE
				headerValue = string.Format("attachment; filename=\"{0}\"", HttpUtility.UrlEncode(filename));

			else if( context.Request.Browser.Browser == "Safari" ) {
				headerValue = string.Format("attachment; filename=\"{0}\"", filename);

				// Safari 这货不支持UTF-8编码标准，也不支持UrlEncode，只能直接使用原文
				// 但是，直接输出原文不是标准的做法，只是这货支持而已，
				// 这样处理后，代理又拿到错误的结果（已被强行转义），所以再增加一个符合规范字符范围的响应头。

				// 也有可能是我没找到更有效的方法，如果看到注释的你有更好的解决方法，请告诉我：liqifeng0503@163.com ，谢谢。

				if( context.Request.Headers[Proxy.ProxyTransferHandler.ProxyFlagHeader] != null )	// 反向代理发出的请求
					context.Response.AddHeader("Content-Disposition-proxy", HttpUtility.UrlEncode(filename));
			}

			else  // 符合新标准的浏览器（部分特殊字符仍然有问题，汉字没问题）
				headerValue = "attachment; filename*=UTF-8''" + HttpUtility.UrlEncode(filename);


			return headerValue;
		}



	}
}
