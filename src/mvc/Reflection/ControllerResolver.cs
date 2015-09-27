using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Web.Compilation;
using System.Text.RegularExpressions;
using System.Web;
using ClownFish.Mvc.OptimizeReflection;
using System.Xml;
using ClownFish.Mvc.TypeExtend;
using ClownFish.Mvc.Debug404;

namespace ClownFish.Mvc.Reflection
{


	internal class ControllerResolver
	{
		internal ControllerResolver(HttpContext context)
		{
			if( context == null )
				throw new ArgumentNullException("context");

			_context = context;
			DiagnoseResult = Http404DebugModule.TryGetDiagnoseResult(context);

			s_metadata.Init();
		}

		#region 变量定义

		private HttpContext _context;

		private static readonly MetadataCache s_metadata = new MetadataCache();

		/// <summary>
		/// 收集404错误的诊断结果对象。
		/// 注意：如果为NULL，表示不进行诊断测试，如果不空，表示要执行诊断测试。
		/// </summary>
		public DiagnoseResult DiagnoseResult { get; private set; }
		
		
		#endregion

		private static readonly ControllerRecognizer s_recognizer = ObjectFactory.New<ControllerRecognizer>();


		private List<TestResult> CreateTestResultList<T>(Dictionary<string, T> dict)
		{
			return (from x in dict
					select new TestResult {
						Text = x.Key,
						IsPass = false
					}).ToList();
		}

		public string GetNamespaceMap(string name)
		{
			if( string.IsNullOrEmpty(name) )
				return name;

			if( name.IndexOf('.') >= 0 )	// 已经包含命名空间就不需要再处理
				return name;

			NamespaceMapAttribute attr;
			if( s_metadata.NamespaceMapDict.TryGetValue(name, out attr) == false ) {
				// 无效的命名空间别名

				if( this.DiagnoseResult != null ) {
					this.DiagnoseResult.ErrorMessages.Add("找不到匹配的NamespaceMapAttribute命名空间映射：" + name);
					this.DiagnoseResult.NamespaceMapTestResult = CreateTestResultList(s_metadata.NamespaceMapDict);
				}

				return name;
			}

			return attr.Namespace;
		}
		

		#region 根据 UrlActionInfo 查找 Controller, Action， 用于ServiceController

		/// <summary>
		/// 根据一个Action的调用信息（类名与方法名），返回内部表示的调用信息。
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public InvokeInfo GetActionInvokeInfo(UrlActionInfo info)
		{
			if( info == null )
				throw new ArgumentNullException("info");

			HttpRequest request = _context.Request;

			if( info.Action == null )
				info.Action = info.MethodName;

			if( info.Controller == null ) {
				info.Namesapce = GetNamespaceMap(info.Namesapce);
				info.Controller = s_recognizer.GetServiceFullName(info);
			}

			InvokeInfo vkInfo = new InvokeInfo();

			vkInfo.Controller = info.ControllerDescription ?? GetServiceController(info.Controller);
			if( vkInfo.Controller == null ) 
				return null;
			

			vkInfo.Action = info.ActionDescription ?? GetServiceAction(vkInfo.Controller.ControllerType, info.Action);
			if( vkInfo.Action == null )
				return null;

			if( vkInfo.Action.MethodInfo.IsStatic == false )
				vkInfo.Instance = vkInfo.Controller.ControllerType.New();

			vkInfo.UrlActionInfo = info;
			return vkInfo;
		}

		private ControllerDescription GetServiceController(string controller)
		{
			if( string.IsNullOrEmpty(controller) )
				throw new ArgumentNullException("controller");

			ControllerDescription description = null;

			// 查找类型的方式：按全名来查找(包含命名空间)。
			// 如果有多个类型的名称相同，必须用完整的命名空间来调用，否则不能定位Controller

			if( controller.IndexOf('.') > 0 ) {
				if( s_metadata.ServiceFullNameDict.TryGetValue(controller, out description) == false ) {
					if( this.DiagnoseResult != null ) {
						this.DiagnoseResult.ErrorMessages.Add("找不到匹配的Controller类型，请检查类型不是抽象类型：" + controller);
						this.DiagnoseResult.ControllerTestResult = CreateTestResultList(s_metadata.ServiceFullNameDict);
					}
				}
			}
			else {
				if( this.DiagnoseResult != null )
					this.DiagnoseResult.ErrorMessages.Add("URL中未指定命名空间，导致类型查找失败。");
			}

			return description;
		}

		private ActionDescription GetServiceAction(Type controller, string action)
		{
			if( controller == null )
				throw new ArgumentNullException("controller");
			if( string.IsNullOrEmpty(action) )
				throw new ArgumentNullException("action");


			// 首先尝试从缓存中读取
			string cacheKey = "01_" + _context.Request.HttpMethod + "#" + controller.FullName + "#" + action;
			ActionDescription mi = (ActionDescription)s_metadata.ServiceActionTable[cacheKey];

			if( mi == null ) {
				bool saveToCache = true;
				
				MethodInfo method = FindAction(action, controller);

				if( method == null ) {
					// 如果Action的名字是submit并且是POST提交，则需要自动寻找Action
					// 例如：多个提交都采用一样的方式：POST /ajax/ns/Product/submit
					if( action.IsSame("submit") && _context.Request.HttpMethod.IsSame("POST") ) {
						// 自动寻找Action
						method = FindSubmitAction(controller);
						saveToCache = false;
					}
				}

				if( method == null ) {
					// 执行诊断测试
					if( this.DiagnoseResult != null ) {
						TestActions(action, controller);

						if( action.IsSame("submit") && _context.Request.HttpMethod.IsSame("POST") == false )
							this.DiagnoseResult.ErrorMessages.Add("如果希望使用 submit 做为Action名称自动匹配，HttpMethod必须要求是POST");
					}

					return null;
				}


				var attr = method.GetMyAttribute<ActionAttribute>() ?? new ActionAttribute();	
				mi = new ActionDescription(method, attr);

				if( saveToCache )
					s_metadata.ServiceActionTable[cacheKey] = mi;
			}

			return mi;
		}

		private MethodInfo FindAction(string action, Type controller)
		{
			foreach( MethodInfo method in controller.GetMethods(MetadataCache.ActionFindBindingFlags) ) {
				if( method.Name.IsSame(action) ) {
					if( ActionIsMatch(method) )		// 方法名匹配之后，还要检查HttpMethod是否匹配
						return method;
				}
			}

			return null;
		}

		private MethodInfo FindSubmitAction(Type controller)
		{
			string[] keys = _context.Request.Form.AllKeys;

			foreach( MethodInfo method in controller.GetMethods(MetadataCache.ActionFindBindingFlags) ) {
				string key = keys.FirstOrDefault(x => method.Name.IsSame(x));
				if( key != null ) {
					if( ActionIsMatch(method) )		// 方法名匹配之后，还要检查HttpMethod是否匹配
						return method;
					else
						return null;
				}
			}

			return null;
		}

		private bool ActionIsMatch(MethodInfo method)
		{
			var attr = method.GetMyAttribute<ActionAttribute>();
			if( attr == null )
				return true;	// 不指定[Action]，默认就认为是有效，因为方法已经是public

			if( attr.AllowExecute(_context.Request.HttpMethod) == false )
				return false;

			return true;
		}



		private void TestActions(string action, Type controller)
		{
			// 能执行到这里，表示Controller查找是成功的。
			this.DiagnoseResult.ControllerType = controller.FullName;
			this.DiagnoseResult.ErrorMessages.Add("找不到匹配的Action名称：" + action);

			this.DiagnoseResult.ActionTestResult = new List<TestResult>();

			foreach( MethodInfo method in controller.GetMethods(MetadataCache.ActionFindBindingFlags) ) {
				if( method.Name.IsSame(action) == false )
					this.DiagnoseResult.ActionTestResult.Add(new TestResult { Text = method.Name, IsPass = false, Reason = "名字不匹配" });
				else 
					this.DiagnoseResult.ActionTestResult.Add(new TestResult { Text = method.Name, IsPass = false, Reason = method.Name + "方法被限制访问，请检查Verb属性" });
			}
		}

		#endregion


		#region 根据 URL 查找 Controller，Action， 用于 PageController

		/// <summary>
		/// 根据一个页面请求路径，返回内部表示的调用信息。
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public InvokeInfo GetActionInvokeInfo(string url)
		{
			if( string.IsNullOrEmpty(url) )
				throw new ArgumentNullException("url");


			MatchActionDescription md = null;
			ActionDescription action = null;

			// 先直接用URL从字典中查找匹配项。
			if( s_metadata.PageActionDict.TryGetValue(url, out action) == false ) {

				// 如果不能直接匹配URL，就用正则表达式再逐个匹配。
				md = GetActionByRegex(url);
				if( md == null ) {
					if( this.DiagnoseResult != null ) 
						// 填充诊断结果
						SetPageUrlDiagnoseResult(url);
					

					return null;
				}
				else
					action = md.ActionDescription;
			}

			InvokeInfo vkInfo = new InvokeInfo();
			vkInfo.Controller = action.PageController;
			vkInfo.Action = action;

			if( md != null )
				vkInfo.RegexMatch = md.Match;

			if( vkInfo.Action.MethodInfo.IsStatic == false )
				//vkInfo.Instance = Activator.CreateInstance(vkInfo.Controller.ControllerType);
				vkInfo.Instance = vkInfo.Controller.ControllerType.New();

			return vkInfo;
		}


		private MatchActionDescription GetActionByRegex(string url)
		{
			if( s_metadata.PageUrlRegexArray == null || s_metadata.PageUrlRegexArray.Length == 0 )
				return null;

			Match match = null;

			for( int i = 0; i < s_metadata.PageUrlRegexArray.Length; i++ ) {
				RegexActionDescription rd = s_metadata.PageUrlRegexArray[i];
				match = rd.Regex.Match(url);
				if( match.Success )
					return new MatchActionDescription { Match = match, ActionDescription = rd.ActionDescription };
			}

			return null;
		}


		private void SetPageUrlDiagnoseResult(string url)
		{
			// 设置所有 IsPass 属性为 false, 因为如果有匹配项就不会出现404错误。

			this.DiagnoseResult.ErrorMessages.Add("URL不能与任何路由配置(PageUrlAttribute)匹配：" + url);

			this.DiagnoseResult.PageUrlTestResult = CreateTestResultList(s_metadata.PageActionDict);

			this.DiagnoseResult.PageRegexUrlTestResult = (from x in s_metadata.PageUrlRegexArray
											 select new TestResult {
												 Text = x.Regex.ToString(),
												 IsPass = false
											 }).ToList();
		}


		#endregion


		#region 用于 REST 查找

		//private static readonly ControllerRecognizer s_recognizer = ObjectFactory.New<ControllerRecognizer>();


		

		///// <summary>
		///// 填充UrlActionInfo对象中的Action，Controller属性，用于供RestServiceModule调用。
		///// 在RestServiceModule中，解析出来的命名空间可能是一个别名，因此不能转换成完整的Controller
		///// methodname也有可能是一个参数值，因此Action属性也需要重新计算。
		///// </summary>
		///// <param name="info"></param>
		///// <returns></returns>
		//public bool FillRestControllerAction(UrlActionInfo info)
		//{
		//	if( info == null )
		//		throw new ArgumentNullException("info");


		//	if( string.IsNullOrEmpty(info.Namesapce) ) {
		//		if( this.DiagnoseResult != null )
		//			this.DiagnoseResult.ErrorMessages.Add("没有在URL中指定命名空间。");

		//		return false;
		//	}



		//	// 首先尝试从缓存中读取
		//	string cacheKey = "02_" + _context.Request.HttpMethod + "#" + info.Namesapce + "#" + info.ClassName + "#" + info.MethodName;
		//	ActionDescription mi = (ActionDescription)s_metadata.ServiceActionTable[cacheKey];

		//	if( mi != null ) {
		//		// 缓存有效就直接从缓存中获取
		//		info.Controller = mi.MethodInfo.DeclaringType.FullName;
		//		info.Action = mi.MethodInfo.Name;
		//		return true;
		//	}


		//	// 计算Controller的类型名称
		//	info.Namesapce = this.GetNamespaceMap(info.Namesapce);
		//	string controllerFullName = s_recognizer.GetServiceFullName(info);

		//	ControllerDescription controllerDescription = GetServiceController(controllerFullName);
		//	if( controllerDescription == null )
		//		return false;

		//	info.ControllerDescription = controllerDescription;
		//	Type controllerType = controllerDescription.ControllerType;


		//	MethodInfo actionMethod = null;
		//	MethodInfo[] actionMethods = controllerType.GetMethods(MetadataCache.ActionFindBindingFlags);

		//	if( string.IsNullOrEmpty(info.MethodName) == false ) {
		//		// 按info.MethodName匹配方法名的方式查找
		//		foreach( MethodInfo method in actionMethods ) {
		//			if( method.Name.IsSame(info.MethodName) ) {
		//				if( ActionIsMatch(method) ) {
		//					// 查找成功。
		//					actionMethod = method;

		//					// 将查找结果放入缓存
		//					var attr = method.GetMyAttribute<ActionAttribute>() ?? new ActionAttribute();
		//					s_metadata.ServiceActionTable[cacheKey] = new ActionDescription(method, attr);
		//					break;
		//				}
		//			}
		//		}
		//	}

		//	if( actionMethod == null ) {
		//		// 再按HttpMethod匹配方法名的方式查找一次。
		//		foreach( MethodInfo method in actionMethods ) {
		//			if( method.Name.IsSame(_context.Request.HttpMethod) ) {
		//				if( ActionIsMatch(method) ) {

		//					// 如果按HttpMethod匹配成功，则说明info.MethodName不是有效方法名称，反而可能是一个参数值
		//					if( string.IsNullOrEmpty(info.MethodName) == false ) {
		//						ParameterInfo[] parameters = method.GetParameters();
		//						if( parameters.Length > 0 )
		//							// 将info.MethodName作为参数值保存起来
		//							info.AddParam(parameters[0].Name, info.MethodName);
		//					}
							
		//					actionMethod = method;
		//					// 注意：有可能URL最后一节是参数，所以不能缓存查找结果。
		//					break;
		//				}
		//			}
		//		}
		//	}

		//	if( actionMethod != null ) {
		//		info.Controller = controllerFullName;
		//		info.Action = actionMethod.Name;
		//		info.ActionDescription = new ActionDescription(actionMethod);
		//		return true;
		//	}
		//	else {
		//		if( this.DiagnoseResult != null ) {
		//			// 执行诊断测试
		//			if( string.IsNullOrEmpty(info.MethodName) == false )
		//				this.DiagnoseResult.ErrorMessages.Add("没有在URL中指定Action名字。");
		//			else
		//				TestActions(info.MethodName, controllerType);
		//		}

		//		return false;
		//	}
		//}

		#endregion




	}
}
