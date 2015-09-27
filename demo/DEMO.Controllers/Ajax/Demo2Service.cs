﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClownFish.Mvc;

namespace DEMO.Controllers.Ajax
{
	public class Customer2
	{
		public string Name;
		public string Tel;
	}
	public class Salesman2
	{
		public string Name { get; set; }
		public string Tel { get; set; }
	}


	public class Demo2Service
	{
		[Action]
		public string TestCustomerType(Customer2 customer, Salesman2 salesman)
		{
			return "customer.Name = " + customer.Name + "\r\n" +
				"customer.Tel = " + customer.Tel + "\r\n" +
				"salesman.Name = " + salesman.Name + "\r\n" +
				"salesman.Name = " + salesman.Tel;
		}
	}
}
