using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClownFish.Mvc
{
	/// <summary>
	/// ClownFish.Mvc在运行时与底层交互的类型封装，重写这些方法可支持友好的单元测试
	/// </summary>
	public class MvcRuntime
	{
		private static readonly SingletonObject<MvcRuntime> _instance = new SingletonObject<MvcRuntime>();

		/// <summary>
		/// MvcRuntime的实例
		/// </summary>
		public static MvcRuntime Instance
		{
			get { return _instance.Value; }
		}

		/// <summary>
		/// 获取网站部署目录
		/// 等效于：HttpRuntime.AppDomainAppPath;
		/// </summary>
		/// <returns></returns>
		public virtual string GetWebSitePath()
		{
			return WebConfig.IsAspnetApp
						? System.Web.HttpRuntime.AppDomainAppPath
						: System.Environment.CurrentDirectory;
		}


		/// <summary>
		/// 根据指定的站内相对路径，计算文件在磁盘中的物理存放路径（可用于替代 Server.MapPath）
		/// 等效于：Path.Combine(HttpRuntime.AppDomainAppPath, filePath);
		/// </summary>
		/// <param name="filePath">相对网站根目录的文件名，不能以 / 开头</param>
		/// <returns></returns>
		public virtual string GetPhysicalPath(string filePath)
		{
			return Path.Combine(GetWebSitePath(), filePath);
		}


		
	}
}
