﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.UI;
using ClownFish.Mvc.Reflection;
using ClownFish.Mvc.Debug404;


namespace ClownFish.Mvc
{

	internal sealed class AspnetPageHandlerFactory : PageHandlerFactory { }

	/// <summary>
	/// MvcPageHandlerFactory
	/// </summary>
	public sealed class MvcPageHandlerFactory : IHttpHandlerFactory
	{
		private readonly AspnetPageHandlerFactory _msPageHandlerFactory = new AspnetPageHandlerFactory();

		IHttpHandler IHttpHandlerFactory.GetHandler(HttpContext context, 
							string requestType, string virtualPath, string physicalPath)
		{
			// 说明：这里不使用virtualPath变量，因为不同的配置，这个变量的值会不一样。
			// 例如：/mvc/*/*.aspx 和 /mvc/*
			// 为了映射HTTP处理器，下面直接使用context.Request.Path

			string requestPath = context.Request.Path;
			string vPath = context.GetRealVirtualPath();

			// 尝试根据请求路径获取Action
			ControllerResolver controllerResolver = new ControllerResolver(context);
			InvokeInfo vkInfo = controllerResolver.GetActionInvokeInfo(vPath);
			
			// 如果没有找到合适的Action，并且请求的是一个ASPX页面，则按ASP.NET默认的方式来继续处理
			if( vkInfo == null  ) {

				if( requestPath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ) {
					// 调用ASP.NET默认的Page处理器工厂来处理
					try {
						return _msPageHandlerFactory.GetHandler(context, requestType, requestPath, physicalPath);
					}
					catch(Exception ex) {
						if( controllerResolver.DiagnoseResult != null ) {
							controllerResolver.DiagnoseResult.ErrorMessages.Add("System.Web.UI.PageHandlerFactory不能根据指定的URL地址创建IHttpHandler实例。");
							controllerResolver.DiagnoseResult.ErrorMessages.Add(ex.Message);

							return Http404DebugModule.TryGetHttp404PageHandler(context);
						}

						throw;
					}
				}
			}

			return ActionHandlerFactory.CreateHandler(vkInfo);
		}

		void IHttpHandlerFactory.ReleaseHandler(IHttpHandler handler)
		{			
		}


	}

	
}
