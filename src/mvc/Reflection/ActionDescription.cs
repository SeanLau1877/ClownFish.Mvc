﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ClownFish.Mvc.Reflection
{
	/// <summary>
	/// 用于描述【能从URL中提取Controller，Action】的Action
	/// </summary>
	internal sealed class ActionDescription : BaseDescription
	{
		public ControllerDescription PageController { get; set; } //为PageAction保留
		public MethodInfo MethodInfo { get; private set; }
		public ActionAttribute Attr { get; private set; }
		public ParameterInfo[] Parameters { get; private set; }
		public bool HasReturn { get; private set; }

		public ActionDescription(MethodInfo m)
			: this(m, m.GetMyAttribute<ActionAttribute>())
		{
		}

		public ActionDescription(MethodInfo m, ActionAttribute atrr)
			: base(m)
		{
			this.MethodInfo = m;
			this.Attr = atrr;
			this.Parameters = m.GetParameters();

			if( m.IsTaskMethod() )
				this.HasReturn = m.GetTaskMethodResultType() != null;
			else
				this.HasReturn = m.ReturnType != ReflectionHelper.VoidType;
		}
	}

	/// <summary>
	/// 用于描述【正则表达式URL】的Action
	/// </summary>
	internal sealed class RegexActionDescription
	{
		public Regex Regex { get; set; }

		public ActionDescription ActionDescription { get; set; }
	}


	/// <summary>
	/// 用于描述【与固定URL匹配】的Action
	/// </summary>
	internal sealed class MatchActionDescription
	{
		public Match Match { get; set; }

		public ActionDescription ActionDescription { get; set; }
	}


}
