using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClownFish.Mvc.Client
{
	/// <summary>
	/// 表示一次HTTP请求的描述信息
	/// </summary>
	public sealed class HttpOption
	{
		/// <summary>
		/// 构造方法
		/// </summary>
		public HttpOption()
		{
			_method = "GET";
			Format = SerializeFormat.FORM;
			ContentType = "application/x-www-form-urlencoded";
			Headers = new HttpHeaderCollection();
		}

		/// <summary>
		/// URL地址（建议查询字符串参数在Data属性中指定，此处只指定文件路径即可）
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// 原始的URL。
		/// 当GET请求时会根据Data属性重新计算URL属性，为了能让HttpOption第二次正常使用，所以设计这个属性来恢复URL属性。
		/// 在HttpWebRequest后，就可以使用这个属性来恢复原始的URL属性。
		/// </summary>
		internal string OriginalUrl { get; set; }

		private string _method;
		/// <summary>
		/// HTTP请求的方法，例如： GET, POST
		/// </summary>
		public string Method
		{
			get { return _method; }
			set
			{
				if( string.IsNullOrEmpty(value) )
					throw new ArgumentNullException("value");
				_method = value.ToUpper();
			}
		}

		/// <summary>
		/// 请求头列表
		/// </summary>
		public HttpHeaderCollection Headers { get; private set; }

		// NameValueCollection 在JSON序列化后，看不到数据，所以不用算了。
		//public NameValueCollection Headers { get; private set; }

		

		/// <summary>
		/// 需要提交的数据（与 $.ajax()方法的 Data 属性含义类似），
		/// 可指定一个FormDataCollection实例，或者一个 IDictionary实例，或者一个匿名对象实例
		/// 如果是GET请求，数据会自动转变化查询字参数，如果是POST，则随请求体一起发送
		/// </summary>
		public object Data { get; set; }

		/// <summary>
		/// 数据的序列化方式。
		/// 注意：不包含请求体的请求，不需要指定这个属性，例如：GET , HEAD
		/// </summary>
		public SerializeFormat Format { get; set; }


		/// <summary>
		/// 框架自动计算，不需要调用者指定，避免产生错误
		/// </summary>
		internal string ContentType { get; set; }

		/// <summary>
		/// 根据Method属性，返回是不是必须以查询字符串形式提交数据
		/// </summary>
		internal bool MustQueryString
		{
			// 参考 Fiddler 的判断规则
			get { return (this.Method == "GET" 
						|| this.Method == "HEAD"
						|| this.Method == "TRACE"
						|| this.Method == "DELETE"
						|| this.Method == "CONNECT"
						|| this.Method == "MKCOL"
						|| this.Method == "COPY"
						|| this.Method == "MOVE"
						|| this.Method == "UNLOCK"
						|| this.Method == "OPTIONS"
				); }
		}

		internal string PostText { get; set; }
		internal FormDataCollection PostFormData { get; set; }


		/// <summary>
		/// Cookie容器
		/// </summary>
		public CookieContainer Cookie { get; set; }
		/// <summary>
		/// 获取或设置请求的身份验证信息。
		/// </summary>
		public ICredentials Credentials { get; set; }


		/// <summary>
		/// 获取或设置 GetResponse 和 GetRequestStream 方法的超时值（以毫秒为单位）。
		/// </summary>
		public int? Timeout { get; set; }


		/// <summary>
		/// 指定一个委托，用于在发送请求前设置HttpWebRequest的其它属性
		/// </summary>
		public Action<HttpWebRequest> SetRequestAction { get; set; }



		/// <summary>
		/// 指定一个委托，用于在请求接收后调用，可获取请求头相关信息
		/// </summary>
		public Action<HttpWebResponse> AfterGetResponseAction { get; set; }

		/// <summary>
		/// 检查传入的属性是否存在冲突的设置
		/// </summary>
		public void CheckInput()
		{
			if( string.IsNullOrEmpty(this.Url) )
				throw new ArgumentNullException("Url");

			if( (Method == "GET" || Method == "HEAD") && Format != SerializeFormat.FORM )
				throw new InvalidOperationException("GET, HEAD 请求只能采用 FORM 序列化方式。");
		}


		internal void SetPostData()
		{
			if( this.Data == null )
				return;


			if( this.Data.GetType() == typeof(string) ) {
				this.PostText = this.Data.ToString();
				return;
			}


			switch( this.Format ) {
				case SerializeFormat.TEXT: {
						this.ContentType = "text/plain";
						this.PostText = this.Data.ToString();
						break;
					}

				case SerializeFormat.JSON: {
						this.ContentType = "application/json";
						this.PostText = (this.Data.GetType() == typeof(string))
									? (string)this.Data
									: JsonHelper.ToJson(this.Data, false);
						break;
					}

				case SerializeFormat.JSON2: {
						this.ContentType = "application/json";
						this.PostText = (this.Data.GetType() == typeof(string))
									? (string)this.Data
									: JsonHelper.ToJson(this.Data, true);
						break;
					}

				case SerializeFormat.XML: {
						this.ContentType = "application/xml";
						this.PostText = (this.Data.GetType() == typeof(string))
										? (string)this.Data
										 : XmlHelper.XmlSerialize(this.Data, Encoding.UTF8);
						break;
					}

				case SerializeFormat.FORM: {
						this.PostFormData = ClownFish.Mvc.Serializer.FormDataProvider.Serialize(this.Data);

						if( this.PostFormData.HasFile )
							this.ContentType = this.PostFormData.GetMultipartContentType();
						else
							this.ContentType = "application/x-www-form-urlencoded";

						break;
					}

				default:
					throw new NotSupportedException();
			}
		}
		

		/// <summary>
		/// 根据原始请求信息文本构建 HttpRequestOption 对象（格式可参考Fiddler的Inspectors标签页内容）
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static HttpOption FromRawText(string text)
		{
			// 示例数据：
			//POST http://www.fish-mvc-demo.com/Ajax/ns/TestAutoAction/submit.aspx HTTP/1.1
			//Host: localhost:37376
			//User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; rv:36.0) Gecko/20100101 Firefox/36.0
			//Accept: */*
			//Accept-Language: zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3
			//Accept-Encoding: gzip, deflate
			//Content-Type: application/x-www-form-urlencoded; charset=UTF-8
			//X-Requested-With: XMLHttpRequest
			//Referer: http://www.fish-mvc-demo.com/Pages/Demo/TestAutoFindAction.htm
			//Content-Length: 72
			//Cookie: hasplmlang=_int_; LoginBy=productKey; PageStyle=Style2;
			//Connection: keep-alive
			//Pragma: no-cache
			//Cache-Control: no-cache

			//input=Fish+Li&Base64=%E8%BD%AC%E6%8D%A2%E6%88%90Base64%E7%BC%96%E7%A0%81

			if( string.IsNullOrEmpty(text) )
				throw new ArgumentNullException("text");

			HttpOption option = new HttpOption();

			using( StringReader reader = new StringReader(text.Trim()) ) {
				string firstLine = reader.ReadLine();

				int p1 = firstLine.IndexOf(' ');
				int p2 = firstLine.LastIndexOf(' ');

				if( p1 < 0 || p1 == p2 )
					throw new ArgumentException("不能识别的请求文本格式。");


				option.Method = firstLine.Substring(0, p1);

				// 不使用HTTP协议版本，只做校验。
				string httpVersion = firstLine.Substring(p2 + 1);
				if( httpVersion.StartsWith("HTTP/") == false )
					throw new ArgumentException("不能识别的请求文本格式。");

				option.Url = firstLine.Substring(p1 + 1, p2 - p1 - 1);

				string line = null;
				while( (line = reader.ReadLine()) != null ) {
					if( line.Length > 0 ) {
						// 处理请求头
						int p3 = line.IndexOf(':');
						if( p3 > 1 ) {
							string name = line.Substring(0, p3);
							string value = line.Substring(p3 + 2);
							option.Headers.Add(name, value);
						}
						else
							throw new ArgumentException("不能识别的请求文本格式。");
					}
					else
						break;
				}

				// 请求体数据
				string postText = reader.ReadToEnd();
				if( string.IsNullOrEmpty(postText) == false )
					option.Data = postText;
			}

			return option;
		}
	}
}
