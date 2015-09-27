using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClownFish.Mvc;
using ClownFish.Mvc.Client;
using ClownFish.Mvc.TypeExtend;

namespace TestApplication1.Common
{
	public class HttpClientExt : EventSubscriber<HttpClient>
	{
		internal static CookieContainer ShareCookie = null;

		internal static bool EnableCorsTest = true;

		/// <summary>
		/// HttpClient在发送请求前的事件，可以在这里调整HttpWebRequest的必要属性
		/// </summary>
		public static event EventHandler<HttpClient.BeforeSendRequestEventArgs> OnBeforeSendRequest;

		public override void SubscribeEvent(HttpClient instance)
		{
			instance.OnBeforeSendRequest += instance_OnBeforeSendRequest;

			if( EnableCorsTest )
				instance.OnBeforeCreateRequest += instance_OnBeforeCreateRequest;
		}

		void instance_OnBeforeSendRequest(object sender, HttpClient.BeforeSendRequestEventArgs e)
		{
			e.Request.Timeout = 5000;
			e.Request.AllowAutoRedirect = false;

			if( e.Request.CookieContainer == null )
				e.Request.CookieContainer = ShareCookie;

			//e.Request.Proxy = new WebProxy("127.0.0.1", 8888);


			if( _lastUrl != null ) {
				e.Option.Url = _lastUrl;
				_lastUrl = null;
			}
			

			EventHandler<HttpClient.BeforeSendRequestEventArgs> handler = OnBeforeSendRequest;
			if( handler != null )
				handler(null, e);
		}

		private string _lastUrl = null;

		void instance_OnBeforeCreateRequest(object sender, HttpClient.BeforeCreateRequestEventArgs e)
		{
			e.Option.Headers.Remove("target-url");	// 先移除上次【可能】设置过的参数

			_lastUrl = e.Option.Url;
			e.Option.Headers.Add("target-url", e.Option.Url);
			e.Option.Url = "http://www.fish-ajax-cors.com/cors-transfer/test.aspx";
		}

		

		
	}
}
