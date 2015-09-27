﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ClownFish.Mvc.Reflection;

namespace ClownFish.Mvc
{
	internal static class ActionHandlerFactory
	{
		public static IHttpHandler CreateHandler(InvokeInfo vkInfo)
		{
			// MyMMC对异步的支持，只限于返回值类型是 Task 或者 Task<T>
			// 所以这里只判断是不是Task类型的返回值，如果是，则表示是一个异步Action

			if( vkInfo.Action.MethodInfo.IsTaskMethod() )
				return TaskAsyncActionHandler.CreateHandler(vkInfo);

			else
				return ActionHandler.CreateHandler(vkInfo);
		}


	}
}
