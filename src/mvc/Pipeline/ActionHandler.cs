using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.SessionState;
using ClownFish.Mvc.Reflection;


namespace ClownFish.Mvc
{
	internal class RequiresSessionActionHandler : ActionHandler, IRequiresSessionState
	{
	}

	internal class ReadOnlySessionActionHandler : ActionHandler, IRequiresSessionState, IReadOnlySessionState
	{
	}

	/// <summary>
	/// 用于同步操作的 HttpHandler
	/// </summary>
	internal class ActionHandler : IHttpHandler
	{
		internal InvokeInfo InvokeInfo { get; private set; }

		internal ActionExecutor ActionExecutor { get; private set; }

		public void ProcessRequest(HttpContext context)
		{
			this.ActionExecutor = ClownFish.Mvc.TypeExtend.ObjectFactory.New<ActionExecutor>();
			this.ActionExecutor.ProcessRequest(context, this);
		}

		public bool IsReusable
		{
			get { return false; }
		}

		public static IHttpHandler CreateHandler(InvokeInfo vkInfo)
		{
			SessionMode mode = vkInfo.GetSessionMode();
			
			if( mode == SessionMode.NotSupport )
				return new ActionHandler { InvokeInfo = vkInfo };

			else if( mode == SessionMode.ReadOnly )
				return new ReadOnlySessionActionHandler { InvokeInfo = vkInfo };

			else
				return new RequiresSessionActionHandler { InvokeInfo = vkInfo };
		}
		
	}
}
