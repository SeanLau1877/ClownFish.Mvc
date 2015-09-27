using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ClownFish.Mvc.Reflection;
using System.Reflection;
using ClownFish.Mvc.OptimizeReflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ClownFish.Mvc.Serializer
{
	internal class NewtonsoftJsonDataProvider : IActionParametersProvider2
	{
		#region IActionParametersProvider 成员

		public object[] GetParameters(HttpContext context, MethodInfo method)
		{
			// 不需要实现这个接口，使用另一个重载版本更高效。
			throw new NotImplementedException();
		}

		#endregion


		private static JsonSerializerSettings DefaultJsonSerializerSettings
		{
			get { return new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }; }
		}


		public object[] GetParameters(HttpContext context, ActionDescription action)
		{
			string input = context.Request.ReadInputStream();

			if( action.Parameters.Length == 1 ) {
				object value = GetObjectFromString(input, action);
				return new object[1] { value };
			}
			else
				return GetMultiObjectsFormString(input, action);
		}


		private object GetObjectFromString(string input, ActionDescription action)
		{
			if( action.Parameters[0].ParameterType == typeof(string) )
				return input;

			return JsonConvert.DeserializeObject(input, action.Parameters[0].ParameterType, DefaultJsonSerializerSettings);
		}
		

		private object[] GetMultiObjectsFormString(string input, ActionDescription action)
		{
			JObject jsonObj = JObject.Parse(input);

			Newtonsoft.Json.JsonSerializer jsonSerializer 
									= Newtonsoft.Json.JsonSerializer.CreateDefault(DefaultJsonSerializerSettings);

			object[] parameters = new object[action.Parameters.Length];

			for( int i = 0; i < parameters.Length; i++ ) {
				string name = action.Parameters[i].Name;

				//http://stackoverflow.com/questions/12055743/json-net-jobject-key-comparison-case-insensitive
				JToken childObj = jsonObj.GetValue(name, StringComparison.OrdinalIgnoreCase);
				if( childObj != null ) {
					Type pType = action.Parameters[i].ParameterType;
					parameters[i] = childObj.ToObject(pType, jsonSerializer);
				}
			}

			return parameters;
		}



		


		internal static string Serialize(object obj, bool keepTypeName)
		{
			if( obj == null )
				throw new ArgumentNullException("obj");

			if( WebConfig.IsDebugMode ) {
				if( keepTypeName )
					return JsonConvert.SerializeObject(obj, Formatting.Indented, DefaultJsonSerializerSettings);

				else
					return JsonConvert.SerializeObject(obj, Formatting.Indented);
			}
			else {

				if( keepTypeName )
					return JsonConvert.SerializeObject(obj, DefaultJsonSerializerSettings);

				else
					return JsonConvert.SerializeObject(obj);
			}
		}

		internal static T Deserialize<T>(string json)
		{
			if( string.IsNullOrEmpty(json) )
				throw new ArgumentNullException("json");

			return JsonConvert.DeserializeObject<T>(json, DefaultJsonSerializerSettings);
		}





		
	}
}
