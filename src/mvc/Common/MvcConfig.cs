using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace ClownFish.Mvc.Config
{
	/// <summary>
	/// 表示ClownFish.Mvc的配置信息类型
	/// </summary>
	public sealed class MvcConfig : ConfigurationSection
	{
		/// <summary>
		/// Pipeline相关的配置节
		/// </summary>
		[ConfigurationProperty("pipeline", IsRequired = false)]
		public PipelineSectionElement Pipeline
		{
			get { return (PipelineSectionElement)this["pipeline"]; }
		}

		/// <summary>
		/// Action相关的配置节
		/// </summary>
		[ConfigurationProperty("action", IsRequired = false)]
		public ActionSectionElement Action
		{
			get { return (ActionSectionElement)this["action"]; }
		}

		

		private static FileDependencyManager<MvcConfig>
					s_instance = new FileDependencyManager<MvcConfig>(		// 基于文件修改通知的缓存实例
							LoadFromConfigFile,		// 读取文件的回调委托
							MvcRuntime.Instance.GetPhysicalPath("ClownFish.Mvc.config"));	// 获取配置文件的路径


		/// <summary>
		/// ClownFish.MvcConfiguration实例的引用（已缓存对象，具有文件更新后自动刷新功能）
		/// </summary>
		/// <returns></returns>
		public static MvcConfig Instance
		{
			get { return s_instance.Result; }
		}

		private static MvcConfig LoadFromConfigFile(string[] files)
		{
			ExeConfigurationFileMap filemap = new ExeConfigurationFileMap();
			filemap.ExeConfigFilename = files[0];

			Configuration config = ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);

			return (MvcConfig)(config.GetSection("mvcConfig")) ?? new MvcConfig();
		}
	}


	/// <summary>
	/// Pipeline相关的配置节
	/// </summary>
	public sealed class PipelineSectionElement : ConfigurationElement
	{
		/// <summary>
		/// 404错误页的模板页面路径
		/// </summary>
		[ConfigurationProperty("http404TemplatePagePath", IsRequired = false, DefaultValue = "/404DiagnoseResult.aspx")]
		public string Http404PagePath
		{
			get { return this["http404TemplatePagePath"].ToString(); }
		}
		/// <summary>
		/// 
		/// </summary>
		protected override void PostDeserialize()
		{
			try {
				string templatePath = UiHelper.AppRoot + this.Http404PagePath.Replace("/", "\\");

				if( System.IO.File.Exists(templatePath) == false )
					throw new System.IO.FileNotFoundException();
			}
			catch {
				throw new ConfigurationErrorsException("pipeline.http404TemplatePagePath 配置值无效。");
			}
		}
	}





	/// <summary>
	/// Action相关的配置节
	/// </summary>
	public sealed class ActionSectionElement : ConfigurationElement
	{
		/// <summary>
		/// JSONP的回调方法的参数名称，
		/// 如果不希望启用JSONP，请设置为 null ，
		/// 默认值："callback" （与 jQuery 保持一致）
		/// </summary>
		[ConfigurationProperty("jsonpCallback", IsRequired = false, DefaultValue="callback")]
		public string JsonpCallback
		{
			get { return this["jsonpCallback"].ToString(); }
		}

	}
}
