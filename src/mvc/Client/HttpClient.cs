using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using System.Threading.Tasks;
using ClownFish.Mvc.TypeExtend;
using System.Reflection;
using ClownFish.Mvc.OptimizeReflection;

namespace ClownFish.Mvc.Client
{
	/// <summary>
	/// 一个用于发送HTTP请求的客户端
	/// </summary>
	public sealed class HttpClient : BaseEventObject, IDisposable
	{
		static HttpClient()
		{
			// 设置无效证书的处理方式：忽略错误
			ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
		}

		private static bool RemoteCertificateValidationCallback(
			Object sender,
			System.Security.Cryptography.X509Certificates.X509Certificate certificate,
			System.Security.Cryptography.X509Certificates.X509Chain chain,
			System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			// 忽略证书错误。
			// HttpClient定位在后台代码调用，因此对方网站应该是确定的，
			// 因此，发生证书错误时，通常是由于证书过期导致的，所以这里就直接忽略这类错误。
			return true;
		}


		internal static void TriggerCctor()
		{
			// 触发静态构造方法，设置与HttpWebRequest相关的参数
		}


		/// <summary>
		/// HttpWebRequest实例引用
		/// </summary>
		public HttpWebRequest Request{get; private set;}

		/// <summary>
		/// HttpWebResponse实例引用，只有在发起HTTP调用之后才会被赋值。
		/// </summary>
		internal HttpWebResponse Response { get; private set; }


		private HttpOption _option;


		/// <summary>
		/// 请求发送前的事件参数类型
		/// </summary>
		public sealed class BeforeSendRequestEventArgs : System.EventArgs
		{
			/// <summary>
			/// HttpWebRequest实例
			/// </summary>
			public HttpWebRequest Request { get; internal set; }
			/// <summary>
			/// HttpOption实例
			/// </summary>
			public HttpOption Option { get; internal set; }
		}
		/// <summary>
		/// 创建请求前的事件参数类型
		/// </summary>
		public sealed class BeforeCreateRequestEventArgs : System.EventArgs
		{
			/// <summary>
			/// HttpOption实例
			/// </summary>
			public HttpOption Option { get; internal set; }
		}
				
		/// <summary>
		/// HttpClient在发送请求前的事件，可以在这里调整HttpWebRequest的必要属性
		/// </summary>
		public event EventHandler<BeforeSendRequestEventArgs> OnBeforeSendRequest;

		/// <summary>
		/// HttpWebRequest前将会引发此事件，提供最后一个修改请求参数的机会。
		/// </summary>
		public event EventHandler<BeforeCreateRequestEventArgs> OnBeforeCreateRequest;


		/// <summary>
		/// 以【同步】方式发起HTTP请求，并获取服务端的返回结果
		/// </summary>
		/// <typeparam name="T">返回服务端的调用结果，并转换成指定的类型</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public T GetResult<T>(HttpOption option)
		{
			Init(option);

			// 发送请求
			if( option.Data != null && option.MustQueryString == false  ) {
				using( Stream stream = this.Request.GetRequestStream() ) {
					WritePostData(stream);
				}
			}

			// 获取服务端响应
			this.Response = (HttpWebResponse)this.Request.GetResponse();

			// 处理回调
			if( _option.AfterGetResponseAction != null )
				_option.AfterGetResponseAction(this.Response);


			// 返回结果
			return GetResultInternal<T>();
		}

		/// <summary>
		/// 以【异步】方式发起HTTP请求，并获取服务端的返回结果
		/// </summary>
		/// <typeparam name="T">返回服务端的调用结果，并转换成指定的类型</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public async Task<T> GetResultAsync<T>(HttpOption option)
		{
			Init(option);

			// 发送请求
			if( option.Data != null && option.MustQueryString == false) {
				using( Stream stream = await this.Request.GetRequestStreamAsync() ) {
					WritePostData(stream);
				}
			}

			// 获取服务端响应
			this.Response = (HttpWebResponse)await this.Request.GetResponseAsync();


			// 处理回调
			if( _option.AfterGetResponseAction != null )
				_option.AfterGetResponseAction(this.Response);

			// 返回结果
			return GetResultInternal<T>();
		}
				

		private void Init(HttpOption option)
		{
			if( option == null )
				throw new ArgumentNullException("option");

			if( _option != null )
				throw new InvalidOperationException("当前HttpClient只能发送一次请求。");

			option.CheckInput();
			option.SetPostData();
						

			_option = option;

			// 重新计算请求参数
			ProcessOption();


			// 创建请求实例
			this.Request = CreateRequest();


			// 发送请求前事件
			EventHandler<BeforeSendRequestEventArgs> handler = OnBeforeSendRequest;
			if( handler != null )
				handler(this, new BeforeSendRequestEventArgs { Request = this.Request, Option = option });

		}


		private void ProcessOption()
		{
			// GET, HEAD 这类请求不允许包含请求体，所以要将提交数据放到URL中
			if( _option.MustQueryString && _option.Data != null ) {

				// 先保存用户指定的URL属性值
				_option.OriginalUrl = _option.Url;

				string postText = _option.PostFormData.ToString();
				if( string.IsNullOrEmpty(postText) == false ) {

					if( _option.Url.IndexOf('?') > 0 )
						_option.Url = _option.Url + "&" + postText;
					else
						_option.Url = _option.Url + "?" + postText;
				}
			}
			

			// 创建HttpWebRequest前事件
			EventHandler<BeforeCreateRequestEventArgs> handler = this.OnBeforeCreateRequest;
			if( handler != null )
				handler(this, new BeforeCreateRequestEventArgs { Option = this._option });
		}

		private HttpWebRequest CreateRequest()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp(_option.Url);

			if( _option.OriginalUrl != null ) {
				// 恢复 _option.Url 属性值，因为它可能被修改过（GET请求时，Data不为NULL ）
				_option.Url = _option.OriginalUrl;
				_option.OriginalUrl = null;
			}

			request.Method = _option.Method;
			request.ServicePoint.Expect100Continue = false;

			if( _option.Cookie != null )
				request.CookieContainer = _option.Cookie;

			if( _option.Credentials != null )
				request.Credentials = _option.Credentials;		// CredentialCache.DefaultCredentials;

			if( _option.Timeout.HasValue )
				request.Timeout = _option.Timeout.Value;

			if( _option.Headers["Content-Type"] == null )
				request.ContentType = _option.ContentType;

			if( _option.Headers["User-Agent"] == null )
				request.UserAgent = "ClownFish.Mvc.HttpClient";


			foreach( NameValue item in _option.Headers )
				request.Headers.InternalAdd(item.Name, item.Value);


			if( _option.SetRequestAction != null )
				_option.SetRequestAction(request);

			return request;
		}

		

		private void WritePostData(Stream stream)
		{
			Encoding defaultEncoding = Encoding.UTF8;

			if( _option.PostText != null ) {
				byte[] postData = defaultEncoding.GetBytes(_option.PostText);

				if( postData != null && postData.Length > 0 ) {
					using( BinaryWriter bw = new BinaryWriter(stream) ) {
						bw.Write(postData);
					}
				}
			}

			else if( _option.PostFormData != null ) {
				_option.PostFormData.WriteToStream(stream, this.Request, defaultEncoding);
			}			
		}


		private Stream GetResponseStream()
		{
			if( this.Response.Headers["Content-Encoding"] == "gzip" )
				return new GZipStream(this.Response.GetResponseStream(), CompressionMode.Decompress);
			else
				return this.Response.GetResponseStream();
		}


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202")]
		private string ReadResponse()
		{
			using( Stream responseStream = GetResponseStream() ) {
				Encoding encoding = GetResponseEncoding();

				using( StreamReader reader = new StreamReader(responseStream, encoding, true) ) {
					return reader.ReadToEnd();
				}
			}
		}


		private byte[] ReadResponseBytes()
		{
			using( Stream responseStream = GetResponseStream() ) {
				using( MemoryStream ms = new MemoryStream() ) {
					byte[] buffer = new byte[1024];
					int lenght = 0;

					while( (lenght = responseStream.Read(buffer, 0, 1024)) > 0 )
						ms.Write(buffer, 0, lenght);

					return ms.ToArray();
				}
			}
		}


		internal static Encoding GetEncodingFromContentType(string contentType)
		{
			try {
				if( string.IsNullOrEmpty(contentType) == false ) {
					string[] array = contentType.SplitTrim(new char[] { ';' });
					if( array.Length > 1 && array[1].StartsWith("charset=", StringComparison.OrdinalIgnoreCase) ) {
						string charset = array[1].Substring("charset=".Length);
						return Encoding.GetEncoding(charset);
					}
				}
			}
			catch { /* 忽略解析失败的场景，如果失败就用 UTF-8 编码 */ }
			return null;
		}

		private Encoding GetResponseEncoding()
		{
			// 说明：直接使用 response.CharacterSet 不靠谱！
			//      因为如果响应头不指定编码，它就默认返回 "ISO-8859-1"，最后也不知道是不是真的是"ISO-8859-1"编码，所以干脆不用这个属性。

			string contentType = this.Response.Headers["Content-Type"];
			return GetEncodingFromContentType(contentType) 
					?? Encoding.UTF8;	// utf-8 才是合理的默认值！
		}

		private T ConvertResult<T>(string responseText)
		{
			if( typeof(T) == typeof(string) )
				return (T)(object)responseText;


			if( this.Response.ContentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0 )
				return JsonHelper.FromJson<T>(responseText);

			else if( this.Response.ContentType.IndexOf("application/xml", StringComparison.OrdinalIgnoreCase) >= 0 )
				return XmlHelper.XmlDeserialize<T>(responseText, System.Text.Encoding.UTF8);

			else
				return (T)Convert.ChangeType(responseText, typeof(T));
		}


		private T GetResultInternal<T>()
		{
			if( typeof(T) == typeof(byte[]) )
				return (T)(object)ReadResponseBytes();

			else {
				string responseText = ReadResponse();

				// 转换结果
				return ConvertResult<T>(responseText);
			}
		}


		/// <summary>
		/// 当请求过程中发生HTTP异常时，可以尝试从WebException实例中读取服务端响应文本
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		public string TryReadResponseException(WebException ex)
		{
			if( ex == null )
				throw new ArgumentNullException("ex");

			if( ex.Response == null )
				return null;

			HttpWebResponse response = ex.Response as HttpWebResponse;

			if( response == null )
				return null;
			else {
				this.Response = response;
				return ReadResponse();
			}
		}


		#region IDisposable 成员

		/// <summary>
		/// 实现IDisposable接口
		/// </summary>
		public void Dispose()
		{
			if( this.Response != null ) {
				((IDisposable)this.Response).Dispose();
				this.Response = null;
			}
		}

		#endregion




		/// <summary>
		/// 根据指定的URL以及提交数据，用【同步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="url">要访问的URL地址</param>
		/// <param name="obj">要提交的数据对象</param>
		/// <param name="format">数据对象在传输过程中采用的序列化方式</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public static T Send<T>(string url, object obj = null, SerializeFormat format = SerializeFormat.FORM)
		{
			HttpOption option = new HttpOption {
				Url = url,
				Data = obj,
				Format = format	
			};

			using( HttpClient client = ObjectFactory.New<HttpClient>() ) {
				return client.GetResult<T>(option);
			}
		}

		/// <summary>
		/// 根据指定的URL以及提交数据，用【同步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="url">要访问的URL地址</param>
		/// <param name="obj">要提交的数据对象</param>
		/// <param name="format">数据对象在传输过程中采用的序列化方式</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public async static Task<T> SendAsync<T>(string url, object obj = null, SerializeFormat format = SerializeFormat.FORM)
		{
			HttpOption option = new HttpOption {
				Url = url,
				Data = obj,
				Format = format
			};

			using( HttpClient client = ObjectFactory.New<HttpClient>() ) {
				return await client.GetResultAsync<T>(option);
			}
		}


		/// <summary>
		/// 根据指定的HttpRequestOption参数，用【同步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public static T Send<T>(HttpOption option)
		{
			using( HttpClient client = ObjectFactory.New<HttpClient>() ) {
				return client.GetResult<T>(option);
			}
		}


		/// <summary>
		/// 根据指定的HttpRequestOption参数，用【异步】方式发起一次HTTP请求
		/// </summary>
		/// <typeparam name="T">返回值的类型参数</typeparam>
		/// <param name="option">HttpRequestOption的实例，用于描述请求参数</param>
		/// <returns>返回服务端的调用结果，并转换成指定的类型</returns>
		public static async Task<T> SendAsync<T>(HttpOption option)
		{
			using( HttpClient client = ObjectFactory.New<HttpClient>() ) {
				return await client.GetResultAsync<T>(option);
			}
		}

	}





}
