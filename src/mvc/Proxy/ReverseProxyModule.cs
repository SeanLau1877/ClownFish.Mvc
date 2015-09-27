﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClownFish.Mvc.Proxy
{
	/// <summary>
	/// 一个简单的反向代理的HTTP模块
	/// </summary>
	public class ReverseProxyModule : IHttpModule
	{
		/// <summary>
		/// 常量字符串：代理站点的Cookie名字
		/// </summary>
		private static readonly string s_ProxySiteCookieName = "fmps12";// 随便取个特殊的名字，只要不被其他人使用就好

		
		/// <summary>
		/// 实现 IHttpModule.Init 方法
		/// </summary>
		/// <param name="app"></param>
		public void Init(HttpApplication app)
		{
			app.BeginRequest += app_BeginRequest;
		}

		void app_BeginRequest(object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;

			string proxyAddress = GetProxySiteAddress(app);
			if( string.IsNullOrEmpty(proxyAddress) )
				return;


			string destAddress = proxyAddress + app.Request.RawUrl;
			app.Context.Items[ProxyTransferHandler.TargetUrlKeyName] = destAddress;

			IHttpHandler hander = new ProxyTransferHandler();
			app.Context.RemapHandler(hander);

			app.Context.Response.Headers.Add("x-ReverseProxyModule", destAddress);	// 用于调试诊断
		}

		/// <summary>
		/// 获取代理目标的站点地址
		/// </summary>
		/// <param name="app"></param>
		/// <returns></returns>
		protected virtual string GetProxySiteAddress(HttpApplication app)
		{
			// ReverseProxyModule 只管读Cookie中的目标站点，

			HttpCookie cookie = app.Request.Cookies[s_ProxySiteCookieName];
			if( cookie != null ) {
				string value = cookie.Value;
				if( string.IsNullOrEmpty(value) == false )
					try {
						return Encoding.UTF8.GetString(Convert.FromBase64String(value));
					}
					catch { /* 如果无法正确读取，就忽略   */ }
			}

			return null;
		}




		/// <summary>
		/// 生成可供ReverseProxyModule读取的代理站点Cookie
		/// </summary>
		/// <param name="siteAddress"></param>
		public static HttpCookie CreateProxySiteCookie(string siteAddress)
		{
			if( string.IsNullOrEmpty(siteAddress) )
				throw new ArgumentNullException("siteAddress");

			if( (siteAddress.StartsWith("http://") == false) && (siteAddress.StartsWith("https://") == false) )
				throw new ArgumentException("参数要求以 http:// 或者 https:// 开头的绝对地址URL格式。");

			if( siteAddress.EndsWith("\\") )	// 因为要和 Request.RawUrl 拼接，所以需要去掉【反斜杠】字符
				siteAddress = siteAddress.Substring(0, siteAddress.Length - 1);

			string value = Convert.ToBase64String(Encoding.UTF8.GetBytes(siteAddress));	// 编码，防止出现特殊字符影响
			HttpCookie cookie = new HttpCookie(s_ProxySiteCookieName, value);			// 临时会话Cookie
			cookie.HttpOnly = true;
			return cookie;
		}




		/// <summary>
		/// 实现 IHttpModule.Dispose 方法
		/// </summary>
		public void Dispose()
		{
		}
	}
}
