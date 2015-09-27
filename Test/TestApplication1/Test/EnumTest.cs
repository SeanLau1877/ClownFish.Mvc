using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClownFish.Mvc.Client;
using TestApplication1.Common;

namespace TestApplication1.Test
{
	public class EnumTest : TestBase
	{
		[TestMethod("Enum测试：提交枚举名称")]
		public async Task TestSubmitName()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/ns/Demo/TestEnum.aspx?submit=submit",
				Data = new { week = "Thursday" },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "Thursday";

			Assert.AreEqual(expected, actual);
		}


		[TestMethod("Enum测试：提交数字")]
		public async Task TestSubmitNumber()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/ns/Demo/TestEnum.aspx?submit=submit",
				Data = new { week = "2" },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "Tuesday";

			Assert.AreEqual(expected, actual);
		}
	}
}
