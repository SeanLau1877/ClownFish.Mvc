﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Caching;


/*  使用方法
		private static FileDependencyManager<List<User>>
					s_cacheItem = new FileDependencyManager<List<User>>(
							files => XmlHelper.XmlDeserializeFromFile<List<User>>(files[0], Encoding.UTF8),
							Path.Combine(HttpRuntime.AppDomainAppPath, @"App_Data\Users.config"));

		public static List<User> Users
		{
			get { return s_cacheItem.Result; }
		}
*/ 

namespace ClownFish.Mvc
{

	/// <summary>
	/// 文件缓存依赖的管理类
	/// </summary>
	/// <typeparam name="T">缓存的数据类型</typeparam>
	public sealed class FileDependencyManager<T>
	{
		private string[] _files;
		private Func<string[], T> _func;

		private readonly string CacheKey = Guid.NewGuid().ToString();

		private CacheResult<T> _cacheResult;
		/// <summary>
		/// 缓存结果
		/// </summary>
		public T Result
		{
			get { return _cacheResult.Result; }
		}
		
		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="func"></param>
		/// <param name="files"></param>
		public FileDependencyManager(Func<string[], T> func, params string[] files)
		{
			if( func == null )
				throw new ArgumentNullException("func");

			if( files == null || files.Length == 0 )
				throw new ArgumentNullException("files");

			_func = func;
			_files = files;

			this.GetObject();
		}

		private void GetObject()
		{
			Exception ex = null;
			T result = default(T);

			try {
				result = _func(_files);
			}
			catch( Exception e ) {
				ex = e;
			}

			//if( ex == null ) {

			// 让Cache帮我们盯住这个配置文件。
			CacheDependency dep = new CacheDependency(_files);
			HttpRuntime.Cache.Insert(CacheKey, "Fish Li", dep,
							System.Web.Caching.Cache.NoAbsoluteExpiration,
							System.Web.Caching.Cache.NoSlidingExpiration,
							CacheItemPriority.NotRemovable, RemovedCallback);

			//}

			_cacheResult = new CacheResult<T>(result, ex);
		}


		private void RemovedCallback(string key, object value, CacheItemRemovedReason reason)
		{
			System.Threading.Thread.Sleep(3000);		// 等待文件关闭

			this.GetObject();
		}
	}
}
