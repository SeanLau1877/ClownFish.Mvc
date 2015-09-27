using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using ClownFish.Mvc.Client;

namespace TestApplication1.Common
{
	public static class SomeExtensions
	{
		public static int GetStatusCode(this WebException ex)
		{
			return (int)(ex.Response as HttpWebResponse).StatusCode;
		}


		public async static Task<int> GetStatusCode(this HttpOption option)
		{
			try {
				await HttpClient.SendAsync<string>(option);
				return 200;
			}
			catch( WebException ex ) {
				return ex.GetStatusCode();
			}
			catch( Exception ) {
				return 500;
			}
		}
	}
}
