using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClownFish.Mvc
{
	/// <summary>
	/// ActionExecutor中所有事件参数的基类
	/// </summary>
	public abstract class BaseWebEventArgs : System.EventArgs
	{
		/// <summary>
		/// HttpContext实例
		/// </summary>
		public HttpContext HttpContext { get; internal set; }
	}

	/// <summary>
	/// 表示进入BeginRequest阶段的事件参数类型
	/// </summary>
	public sealed class BeginRequestEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Controller实例
		/// </summary>
		public object ControllerInstance { get; internal set; }
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
	}

	/// <summary>
	/// 表示进入CorsCheck阶段的事件参数类型
	/// </summary>
	public sealed class CorsCheckEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Origin请求头信息
		/// </summary>
		public string Origin { get; internal set; }
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
		/// <summary>
		/// 是否禁止访问，如果在事件中设置为 true ，将禁止本次AJAX的跨域访问。
		/// </summary>
		public bool IsForbidden { get; set; }
	}
	/// <summary>
	/// 表示进入AuthorizeCheck阶段的事件参数类型
	/// </summary>
	public sealed class AuthorizeCheckEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Action方法上的AuthorizeAttribute实例
		/// </summary>
		public AuthorizeAttribute Attribute { get; internal set; }
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
	}

	/// <summary>
	/// 表示进入GetActionParameters阶段的事件参数类型
	/// </summary>
	public sealed class GetActionParametersEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
		/// <summary>
		/// 获取到的参数值数组
		/// </summary>
		public object[] Parameters { get; set; }
	}
	/// <summary>
	/// 表示进入 BeforeExceute / AfterExecute 阶段的事件参数类型
	/// </summary>
	public sealed class ExecuteActionEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Controller实例
		/// </summary>
		public object ControllerInstance { get; internal set; }
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
		/// <summary>
		/// 获取到的参数值数组
		/// </summary>
		public object[] Parameters { get; internal set; }
		/// <summary>
		/// 执行结果（仅对 AfterExecuteAction 事件有效）
		/// </summary>
		public object ExecuteResult { get; internal set; }
	}
	/// <summary>
	/// 表示进入设置OutputCache阶段的事件参数类型
	/// </summary>
	public sealed class OutputResultEventArgs : BaseWebEventArgs
	{
		/// <summary>
		/// Controller实例
		/// </summary>
		public object ControllerInstance { get; internal set; }
		/// <summary>
		/// Action方法的反射信息（MethodInfo实例）
		/// </summary>
		public MethodInfo ActionMethod { get; internal set; }
		/// <summary>
		/// 执行结果
		/// </summary>
		public object ExecuteResult { get; internal set; }
	}
}
