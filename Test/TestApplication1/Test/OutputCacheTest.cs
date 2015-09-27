using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClownFish.Mvc.Client;
using TestApplication1.Common;

namespace TestApplication1.Test
{
	public class OutputCacheTest : TestBase
	{
		[TestMethod("OutputCache测试")]
		public async Task TestSubmitName()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Pages/Demo/TestOutputCache.aspx",
				Method = "GET"
			};

			string firstText = await HttpClient.SendAsync<string>(option);

			await Task.Run(() => System.Threading.Thread.Sleep(100));

			string secondText = await HttpClient.SendAsync<string>(option);
			Assert.AreEqual(firstText, secondText);

			await Task.Run(() => System.Threading.Thread.Sleep(100));

			string text3 = await HttpClient.SendAsync<string>(option);
			Assert.AreEqual(firstText, text3);

			await Task.Run(() => System.Threading.Thread.Sleep(3000));

			string text4 = await HttpClient.SendAsync<string>(option);
			Assert.AreNotEqual(firstText, text4);
			
		}
	}
}
