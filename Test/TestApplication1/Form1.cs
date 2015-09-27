using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using TestApplication1.Common;
using TestApplication1.Test;
using System.Threading;
using Newtonsoft.Json;
using System.Net;
using ClownFish.Mvc.Client;

namespace TestApplication1
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			foreach( ListViewItem item in this.listView1.Items )
				item.Checked = true;
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			foreach( ListViewItem item in this.listView1.Items )
				item.Checked = false;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			HttpClientExt.OnBeforeSendRequest += HttpClientExt_OnBeforeSendRequest;
			checkBox1_CheckedChanged(null, null);
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			HttpClientExt.EnableCorsTest = this.checkBox1.Checked;
		}

		private StringBuilder _httpLog = new StringBuilder();
		void HttpClientExt_OnBeforeSendRequest(object sender, ClownFish.Mvc.Client.HttpClient.BeforeSendRequestEventArgs e)
		{
			_httpLog.AppendLine(e.Request.RequestUri.AbsoluteUri);

			string json = JsonConvert.SerializeObject(e.Option, Formatting.Indented);
			_httpLog.AppendLine(json).AppendLine("\r\n\r\n\r\n");

		}

		private void Form1_Shown(object sender, EventArgs e)
		{
			MethodInfo[] methods =
					(from t in this.GetType().Assembly.GetTypes()
					 from m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
					 let a = m.GetCustomAttribute<TestMethodAttribute>()
					 where a != null
					 select m).ToArray();

			foreach( MethodInfo m in methods ) {
				ListViewItem item = new ListViewItem(m.GetCustomAttribute<TestMethodAttribute>().Description, 0);
				item.Tag = m;
				//item.Checked = true;
				listView1.Items.Add(item);
			}
		}
		private async void btnRun_Click(object sender, EventArgs e)
		{
			if( btnRun.Enabled == false )
				return;

			btnRun.Enabled = false;

			if(    CheckWebsiteIsRunning("http://www.fish-ajax-cors.com/") == false			// CrosClientWebSite1
				|| CheckWebsiteIsRunning("http://www.fish-mvc-demo.com/") == false ) {	// DemoWebSite1

				btnRun.Enabled = true;
				return;
			}
			

			_httpLog.Clear();
			txtHttpLog.Text = string.Empty;
			txtTestResult.Text = string.Empty;
			StringBuilder sb = new StringBuilder();
			int count = 0;

			foreach( ListViewItem item in this.listView1.Items ) {
				item.ImageIndex = 0;
			}

			foreach( ListViewItem item in this.listView1.Items ) {
				if( item.Checked ) {
					count++;

					MethodInfo m = (MethodInfo)item.Tag;
					item.ImageIndex = 1;

					try {
						object instance = Activator.CreateInstance(m.DeclaringType);
						Task task = m.Invoke(instance, null) as Task;
						if( task != null )
							await task;

						//Task.Run(() => m.Invoke(instance, null)).Wait();

						item.ImageIndex = 2;
					}
					catch( Exception ex ) {
						item.ImageIndex = 3;

						string errorMessage = null;
						WebException webException = ex as WebException;
						if( webException != null ) {
							HttpClient client = new HttpClient();
							string html = client.TryReadResponseException(webException);
							errorMessage = GetHtmlTitle(html);
						}


						sb.AppendLine("Method: " + m.DeclaringType.FullName + "." + m.Name)
							.AppendLine("---------------------------------------------------")
							.Append(errorMessage ?? ex.GetBaseException().Message)
							.AppendLine("\r\n---------------------------------------------------\r\n\r\n");
					}
				}
			}


			btnRun.Enabled = true;

			if( count == 0 ) {
				txtTestResult.Text = "没有需要执行的测试用例。";
			}
			else {
				if( sb.Length == 0 )
					txtTestResult.Text = "全部测试用例已通过。";
				else
					txtTestResult.Text = sb.ToString();
			}

			txtHttpLog.Text = _httpLog.ToString();
			_httpLog.Clear();
		}


		private bool CheckWebsiteIsRunning(string url)
		{
			Task<string> task1 = Task.Run(
				() => HttpClient.Send<string>(url)
				);

			if( task1.Wait(2000) == false ) {
				MessageBox.Show(string.Format("站点 {0} 没有启动，或者没有在2秒内应答，请确保站点已启动或者再试一次。", url), 
							this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			return true;
		}


		public static string GetHtmlTitle(string text)
		{
			if( string.IsNullOrEmpty(text) )
				return null;

			int p1 = text.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
			int p2 = text.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);

			if( p2 > p1 && p1 > 0 ) {
				p1 += "<title>".Length;
				return text.Substring(p1, p2 - p1);
			}

			return null;
		}

		private void linkLabel3_Click(object sender, EventArgs e)
		{
			string message = @"
请确保在测试前，在本机创建2个域名，
可以在 C:\Windows\System32\drivers\etc\hosts 中指定：

127.0.0.1       www.fish-ajax-cors.com
127.0.0.1       www.fish-mvc-demo.com
";

			MessageBox.Show(message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}


	}
}
