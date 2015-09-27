using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClownFish.Mvc;
using ClownFish.Mvc.Client;
using TestApplication1.Common;

namespace TestApplication1.Test
{
	class SimpleAjaxTest : TestBase
	{
		[TestMethod("简单AJAX测试：一个字符串参数")]
		public async Task TestMd5()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/ns/Demo/GetMd5.aspx",
				Data = new { input = "Fish Li" },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "44d2d9635ed5cdea2a858cd7a1cc2b0c";

			Assert.AreEqual(expected, actual);
		}


		[TestMethod("简单AJAX测试：命名空间-1")]
		public async Task TestNamespace_1()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/Fish.AA.Test/Add.aspx",
				Data = new { a = 2, b= 3 },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "5";

			Assert.AreEqual(expected, actual);
		}

		[TestMethod("简单AJAX测试：命名空间-2")]
		public async Task TestNamespace_2()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/Fish.BB.Test/Add.aspx",
				Data = new { a = 2, b = 3 },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "15";

			Assert.AreEqual(expected, actual);
		}

		[TestMethod("简单AJAX测试：命名空间-3")]
		public async Task TestNamespace_3()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/DEMO.Controllers.TestTask.TaskDemoService/Add.aspx",
				Data = new { a = 2, b = 3 },
				Method = "GET"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = "105";

			Assert.AreEqual(expected, actual);
		}


		[TestMethod("简单AJAX测试：Action使用自定义的数据类型")]
		public async Task TestCustomerType()
		{
			HttpOption option = new HttpOption {
				Url = "http://www.fish-mvc-demo.com/Ajax/ns/Demo2/TestCustomerType.aspx",
				Data = new FormDataCollection()
							.Add("customer.Name", "name----1")
							.Add("customer.Tel", "tel----1")
							.Add("salesman.Name", "name----2")
							.Add("salesman.Tel", "tel----2"),
				Method = "POST"
			};

			string actual = await HttpClient.SendAsync<string>(option);
			string expected = @"
customer.Name = name----1
customer.Tel = tel----1
salesman.Name = name----2
salesman.Name = tel----2".Trim();

			Assert.AreEqual(expected, actual);
		}
	}
}
