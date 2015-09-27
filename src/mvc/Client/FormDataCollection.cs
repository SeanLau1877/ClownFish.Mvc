using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ClownFish.Mvc.Client
{
	/// <summary>
	/// 表示HTTP表单的数据集合（key=value ）
	/// </summary>
	public sealed class FormDataCollection
	{
		private static readonly string s_boundary = "------------" + Guid.NewGuid().ToString("N");

		private List<KeyValuePair<string, object>> _list = new List<KeyValuePair<string, object>>(8);

		/// <summary>
		/// 是否包含上传文件
		/// </summary>
		public bool HasFile { get; set; }



		/// <summary>
		/// 往集合中添加一个键值对（允许key重复）
		/// </summary>
		/// <param name="key">数据项的名称</param>
		/// <param name="value">数据值</param>
		public FormDataCollection Add(string key, string value)
		{
			if( string.IsNullOrEmpty(key) )
				throw new ArgumentNullException("key");

			//if( _sb.Length > 0 )
			//	_sb.Append("&");

			//_sb.Append(System.Web.HttpUtility.UrlEncode(key))
			//	.Append("=")
			//	.Append(System.Web.HttpUtility.UrlEncode(value ?? string.Empty));

			_list.Add(new KeyValuePair<string, object>(key, value ?? string.Empty));

			return this;
		}


		/// <summary>
		/// 往集合中添加一个键值对（允许key重复）
		/// </summary>
		/// <param name="key">数据项的名称</param>
		/// <param name="value">数据值</param>
		public FormDataCollection Add(string key, object value)
		{
			if( string.IsNullOrEmpty(key) )
				throw new ArgumentNullException("key");

			// 除了上传文件之外，其它数据都转换成字符串。

			if( value == null )
				return Add(key, string.Empty);

			Type valueType = value.GetType();

			if( valueType == typeof(string) )
				return Add(key, (string)value);


			if( valueType == typeof(FileInfo) ) {
				// -----------------------------------------------
				HasFile = true;			// 标记包含上传文件
				// -----------------------------------------------
				_list.Add(new KeyValuePair<string, object>(key, value));
				return this;
			}

			if( valueType == typeof(byte[]) ) {
				string text = Convert.ToBase64String((byte[])value);
				return Add(key, text);
			}

			// string[] ，不处理算了，可以通过给 Data 设置来解决。

			_list.Add(new KeyValuePair<string, object>(key, value.ToString()));
			return this;
		}

		/// <summary>
		/// 输出集合数据为 "application/x-www-form-urlencoded" 格式。
		/// 注意：1、忽略上传文件
		///      2、每次调用都会重新计算（因此尽量避免重复调用）
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			foreach( KeyValuePair<string, object> kvp in _list ) {
				if( kvp.Value.GetType() == typeof(string) ) {
					if( sb.Length > 0 )
						sb.Append("&");

					sb.Append(System.Web.HttpUtility.UrlEncode(kvp.Key))
						.Append("=")
						.Append(System.Web.HttpUtility.UrlEncode((string)kvp.Value ?? string.Empty));
				}
			}
			return sb.ToString();
		}


		internal string GetMultipartContentType()
		{
			return "multipart/form-data; boundary=" + s_boundary;
		}

		/// <summary>
		/// 将收集的表单数据写入流
		/// </summary>
		/// <param name="stream">Stream实例，用于写入</param>
		/// <param name="request">HttpWebRequest实例，用于上传文件时指定ContentType属性</param>
		/// <param name="encoding">字符编码方式</param>
		public void WriteToStream(Stream stream, HttpWebRequest request, Encoding encoding)
		{
			if( stream == null )
				throw new ArgumentNullException("stream");
			if( request == null )
				throw new ArgumentNullException("request");
			if( encoding == null )
				throw new ArgumentNullException("encoding");

			if( HasFile == false ) {
				// 获取编码后的字符串
				string text = this.ToString();

				if( string.IsNullOrEmpty(text) == false ) {
					byte[] postData = encoding.GetBytes(text);

					// 写输出流
					using( BinaryWriter bw = new BinaryWriter(stream) ) {
						bw.Write(postData);
					}
				}
			}
			else {
				WriteMultiFormToStream(stream, request, encoding);
			}
		}


		private void WriteMultiFormToStream(Stream stream, HttpWebRequest request, Encoding encoding)
		{
			if( request == null )
				throw new ArgumentNullException("request");


			// copy from: http://www.cnblogs.com/fish-li/archive/2011/07/17/2108884.html


			// 数据块的分隔标记，用于设置请求头，注意：这个地方最好不要使用汉字。
			//string boundary = "---------------------------" + Guid.NewGuid().ToString("N");
			// 数据块的分隔标记，用于写入请求体。
			//   注意：前面多了一段： "--" ，而且它们将独占一行。
			byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + s_boundary + "\r\n");

			// 设置请求头。指示是一个上传表单，以及各数据块的分隔标记。
			//request.ContentType = "multipart/form-data; boundary=" + boundary;


			// 写入非文件的keyvalues部分
			foreach( KeyValuePair<string, object> kvp in _list ) {
				if( kvp.Value.GetType() == typeof(string) ) {

					// 写入数据块的分隔标记
					stream.Write(boundaryBytes, 0, boundaryBytes.Length);

					// 写入数据项描述，这里的Value部分可以不用URL编码
					string str = string.Format(
							"Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}",
							kvp.Key, kvp.Value);

					byte[] data = encoding.GetBytes(str);
					stream.Write(data, 0, data.Length);
				}
			}


			// 写入要上传的文件
			foreach( KeyValuePair<string, object> kvp in _list ) {
				if( kvp.Value.GetType() == typeof(FileInfo) ) {
					FileInfo file = (FileInfo)kvp.Value;

					// 写入数据块的分隔标记
					stream.Write(boundaryBytes, 0, boundaryBytes.Length);

					// 写入文件描述，这里设置一个通用的类型描述：application/octet-stream，具体的描述在注册表里有。
					string description = string.Format(
							"Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
							"Content-Type: application/octet-stream\r\n\r\n",
							kvp.Key, file.FullName);

					// 注意：这里如果不使用UTF-8，对于汉字会有乱码。
					byte[] header = Encoding.UTF8.GetBytes(description);
					stream.Write(header, 0, header.Length);

					// 写入文件内容
					byte[] body = File.ReadAllBytes(file.FullName);
					stream.Write(body, 0, body.Length);
				}				
			}

			// 写入结束标记
			boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + s_boundary + "--\r\n");
			stream.Write(boundaryBytes, 0, boundaryBytes.Length);

		}

	}
}
