using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Reflection;
using System.Collections.Specialized;
using ClownFish.Mvc.OptimizeReflection;
using ClownFish.Mvc.Serializer;
using ClownFish.Mvc.Reflection;
using System.Threading.Tasks;
using System.Threading;


namespace ClownFish.Mvc
{

	/// <summary>
	/// 执行Action的处理器
	/// </summary>
	public sealed class ActionExecutor : ClownFish.Mvc.TypeExtend.BaseEventObject
	{
		/// <summary>
		/// HttpContext实例引用
		/// </summary>
		public HttpContext HttpContext { get; private set; }

		private IHttpHandler Handler;

		private InvokeInfo InvokeInfo;

		/// <summary>
		/// 保存用户额外的数据，如果需要使用，请自行赋值。
		/// </summary>
		public System.Collections.Hashtable UserData { get; set; }

		private static readonly string STR_MvcVersion
			= System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(ActionExecutor).Assembly.Location).FileVersion;

		#region 事件定义

		/// <summary>
		/// 开始进入执行阶段的事件
		/// </summary>
		public event EventHandler<BeginRequestEventArgs> OnBeginRequest;
		/// <summary>
		/// 执行跨域检查阶段的事件
		/// </summary>
		public event EventHandler<CorsCheckEventArgs> OnCorsCheck;
		/// <summary>
		/// 授权检查阶段的事件
		/// </summary>
		public event EventHandler<AuthorizeCheckEventArgs> OnAuthorizeRequest;
		/// <summary>
		/// 获取Action参数阶段的事件
		/// </summary>
		public event EventHandler<GetActionParametersEventArgs> OnGetActionParameters;
		/// <summary>
		/// 执行Action【前】的事件
		/// </summary>
		public event EventHandler<ExecuteActionEventArgs> BeforeExecuteAction;
		/// <summary>
		/// 执行Action【后】的事件
		/// </summary>
		public event EventHandler<ExecuteActionEventArgs> AfterExecuteAction;
		/// <summary>
		/// 设置输出阶段的事件
		/// </summary>
		public event EventHandler<OutputResultEventArgs> OnOutputResult;

		#endregion


		#region 核心执行逻辑


		private void SetController()
		{
			BaseController controller = this.InvokeInfo.Instance as BaseController;

			if( controller != null ) 
				controller.HttpContext = new HttpContextWrapper(this.HttpContext);
			
		}

		internal void ProcessRequest(HttpContext context, ActionHandler handler)
		{
			if( context == null )
				throw new ArgumentNullException("context");
			if( handler == null )
				throw new ArgumentNullException("handler");

			this.HttpContext = context;
			this.Handler = handler;
			this.InvokeInfo = handler.InvokeInfo;

			// 设置 BaseController 的相关属性
			SetController();


			// 进入请求处理阶段
			BeginRequest();

			// 安全检查
			SecurityCheck();

			// 授权检查
			AuthorizeRequest();
			

			// 执行 Action
			object actionResult = ExecuteAction();

			// 设置输出缓存
			SetOutputCache();

			// 处理方法的返回结果
			OutputResult(actionResult);
		}
		
		internal async Task ProcessRequestAsync(HttpContext context, TaskAsyncActionHandler handler)
		{
			if( context == null )
				throw new ArgumentNullException("context");
			if( handler == null )
				throw new ArgumentNullException("handler");

			this.HttpContext = context;
			this.Handler = handler;
			this.InvokeInfo = handler.InvokeInfo;

			// 设置 BaseController 的相关属性
			SetController();

			// 在异步执行前，先保存当前同步上下文的实例，供异步完成后执行切换调用。
			SynchronizationContext syncContxt = SynchronizationContext.Current;


			// 进入请求处理阶段
			BeginRequest();

			// 安全检查
			SecurityCheck();

			// 授权检查
			AuthorizeRequest();
			

			//this.HttpContext.WriteHeader("ProcessRequestAsync-before-await");
			
			// 执行 Action
			object actionResult = await ExecuteActionAsync();

			//this.HttpContext.WriteHeader("ProcessRequestAsync-after-await");
			

			// 切换到原先的上下文环境，执行余下操作
			syncContxt.Send(x => {
				//System.Runtime.Remoting.Messaging.CallContext.HostContext = (HttpContext)x;

				// 设置输出缓存
				SetOutputCache();				

				// 处理方法的返回结果
				//this.HttpContext.WriteHeader("call OutputResult()");
				OutputResult(actionResult);
			}, this.HttpContext);
		}
				
		private object ExecuteAction()
		{
			// 准备要传给调用方法的参数
			object[] parameters = GetActionParameters();
			object actionResult = null;

			TriggerBeforeExecuteAction(parameters);

			// 调用方法
			if( this.InvokeInfo.Action.HasReturn )
				actionResult = this.InvokeInfo.Action.MethodInfo.FastInvoke(this.InvokeInfo.Instance, parameters);

			else
				this.InvokeInfo.Action.MethodInfo.FastInvoke(this.InvokeInfo.Instance, parameters);

			TriggerAfterExecuteAction(parameters, actionResult);
			return actionResult;
		}
		
		private async Task<object> ExecuteActionAsync()
		{
			// 准备要传给调用方法的参数
			object[] parameters = GetActionParameters();
			object actionResult = null;

			TriggerBeforeExecuteAction(parameters);

			//this.HttpContext.WriteHeader("ExecuteActionAsync-before-await");
						

			// 调用方法
			// 说明：能进入这里的，只能二类返回类型： Task, Task<T>，因此如果Action有返回值，只能是Task<T>类型

			if( this.InvokeInfo.Action.HasReturn ) {
				Task task = (Task)this.InvokeInfo.Action.MethodInfo.FastInvoke(this.InvokeInfo.Instance, parameters);
				await task;

				//this.HttpContext.WriteHeader("ExecuteActionAsync-after-await-before-Result");

				// 从 Task<T> 中获取返回值
				PropertyInfo property = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
				actionResult = property.FastGetValue(task);
			}
			else {
				await (Task)this.InvokeInfo.Action.MethodInfo.FastInvoke(this.InvokeInfo.Instance, parameters);
			}

			//this.HttpContext.WriteHeader("ExecuteActionAsync-after-await-return");

			TriggerAfterExecuteAction(parameters, actionResult);
			return actionResult;
		}

		#endregion


		#region 阶段性执行过程（非关键性过程）

		private void BeginRequest()
		{
			this.HttpContext.Response.AppendHeader("X-ClownFish.Mvc-Version", STR_MvcVersion);

			EventHandler<BeginRequestEventArgs> eventHandler = this.OnBeginRequest;
			if( eventHandler != null )
				eventHandler(this, new BeginRequestEventArgs {
					HttpContext = this.HttpContext,
					ControllerInstance = this.InvokeInfo.Instance,
					ActionMethod = this.InvokeInfo.Action.MethodInfo
				});
		}

		private void CorsCheck()
		{
			// 通常情况下，没有必要限制跨域，
			// 如果是希望确保安全调用，可以增加参数检查，单纯检查请求头可能没有意义
			// 所以，在ClownFish.Mvc中，默认是允许跨域，会自动应答。

			string origin = this.HttpContext.Request.Headers["Origin"];

			if( string.IsNullOrEmpty(origin) )
				return;

			EventHandler<CorsCheckEventArgs> eventHandler = this.OnCorsCheck;
			if( eventHandler != null ) {
				CorsCheckEventArgs e = new CorsCheckEventArgs {
					Origin = origin,
					HttpContext = this.HttpContext,
					ActionMethod = this.InvokeInfo.Action.MethodInfo
				};

				// 如果希望限制跨域来源，可以在事件中检查，并将 e.IsForbidden 设置为 true
				eventHandler(this, e);

				if( e.IsForbidden )
					return;
			}


			this.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", origin);
			this.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "*");
			this.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
		}

		private void AuthorizeRequest()
		{
			// 验证请求是否允许访问（身份验证）
			AuthorizeAttribute authorize = this.InvokeInfo.GetAuthorize();
			if( authorize != null ) {
				if( authorize.AuthenticateRequest(this.HttpContext) == false )
					ExceptionHelper.Throw403Exception(this.HttpContext);
			}


			EventHandler<AuthorizeCheckEventArgs> eventHandler = this.OnAuthorizeRequest;
			if( eventHandler != null )
				eventHandler(this, new AuthorizeCheckEventArgs {
					HttpContext = this.HttpContext,
					Attribute = authorize,
					ActionMethod = this.InvokeInfo.Action.MethodInfo
				});
		}

		private void SecurityCheck()
		{
			// ASP.NET的安全检查
			if( this.InvokeInfo.Action.Attr.NeedValidateRequest() )
				this.HttpContext.Request.ValidateInput();


			// AJAX跨域许可检查
			CorsCheck();
		}

		private object[] GetActionParameters()
		{
			ActionDescription action = this.InvokeInfo.Action;

			if( action.Parameters == null || action.Parameters.Length == 0 )
				return null;



			IActionParametersProvider provider 
						= ActionParametersProviderFactory.Instance.CreateProvider(this.HttpContext);

			object[] parameters = null;

			IActionParametersProvider2 p2 = provider as IActionParametersProvider2;
			if( p2 != null )		// 优先使用内部接口版本
				parameters = p2.GetParameters(this.HttpContext, action);
			else
				parameters = provider.GetParameters(this.HttpContext, action.MethodInfo);


			EventHandler<GetActionParametersEventArgs> eventHandler = this.OnGetActionParameters;
			if( eventHandler != null ) {
				GetActionParametersEventArgs e = new GetActionParametersEventArgs {
					HttpContext = this.HttpContext,
					ActionMethod = this.InvokeInfo.Action.MethodInfo,
					Parameters = parameters
				};

				eventHandler(this, e);

				return e.Parameters;
			}

			return parameters;
		}

		private void TriggerBeforeExecuteAction(object[] parameters)
		{
			EventHandler<ExecuteActionEventArgs> beforeEvent = this.BeforeExecuteAction;
			if( beforeEvent != null ) {
				ExecuteActionEventArgs e = new ExecuteActionEventArgs {
					HttpContext = this.HttpContext,
					ControllerInstance = this.InvokeInfo.Instance,
					ActionMethod = this.InvokeInfo.Action.MethodInfo,
					Parameters = parameters
				};

				beforeEvent(this, e);
			}
		}

		private void TriggerAfterExecuteAction(object[] parameters, object actionResult)
		{
			EventHandler<ExecuteActionEventArgs> afterEvent = this.AfterExecuteAction;
			if( afterEvent != null ) {
				ExecuteActionEventArgs e = new ExecuteActionEventArgs {
					HttpContext = this.HttpContext,
					ControllerInstance = this.InvokeInfo.Instance,
					ActionMethod = this.InvokeInfo.Action.MethodInfo,
					Parameters = parameters
				};

				e.ExecuteResult = actionResult;
				afterEvent(this, e);
			}
		}
		

		private void SetOutputCache()
		{
			// 设置OutputCache
			OutputCacheAttribute outputCache = this.InvokeInfo.GetOutputCacheSetting();
			if( outputCache != null )
				outputCache.SetResponseCache(this.HttpContext);
		}

		internal void OutputResult(object result)
		{
			EventHandler<OutputResultEventArgs> eventHandler = this.OnOutputResult;
			if( eventHandler != null )
				eventHandler(this, new OutputResultEventArgs {
					HttpContext = this.HttpContext,
					ControllerInstance = this.InvokeInfo.Instance,
					ActionMethod = this.InvokeInfo.Action.MethodInfo,
					ExecuteResult = result
				});

			if( result == null )
				return;


			IActionResult actionResult = result as IActionResult;
			if( actionResult == null )
				actionResult = ObjectToActionResult(result, this.InvokeInfo.Action.Attr.OutFormat);
			

			actionResult.Ouput(this.HttpContext);
		}


		/// <summary>
		/// 尝试根据方法的修饰属性来构造IActionResult实例
		/// </summary>
		/// <param name="result"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		private IActionResult ObjectToActionResult(object result, SerializeFormat format)
		{
			IActionResult actionResult = null;

			if( format == SerializeFormat.AUTO ) {
				// 如果是自动响应，那么就根据请求头的指定的方式来决定
				string expectFormat = this.HttpContext.Request.Headers["Result-Format"];

				if( string.IsNullOrEmpty(expectFormat) == false ) {
					SerializeFormat f2;
					if( Enum.TryParse<SerializeFormat>(expectFormat.ToUpper(), out f2) )
						format = f2;
				}
			}


			if( format == SerializeFormat.JSON )
				actionResult = new JsonResult(result);

			else if( format == SerializeFormat.JSON2 )
				actionResult = new JsonResult(result, true);

			else if( format == SerializeFormat.XML )
				actionResult = new XmlResult(result);

			else if( format == SerializeFormat.FORM ) {
				string text = FormDataProvider.Serialize(result).ToString();
				actionResult = new TextResult(text);
			}


			// 无法构造出IActionResult实例，就按字符串形式输出
			if( actionResult == null )
				actionResult = new TextResult(result);

			return actionResult;
		}

		#endregion


	}
}
