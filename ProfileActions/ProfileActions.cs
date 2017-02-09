/*
 * Created by SharpDevelop.
 * Author: Lord Alfred ( vk.com/lord.alfred )
 * Date: 08.02.2017
 * Time: 4:49
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.ProjectModel.Collections;

namespace ZPProfileActions
{
	public static class ProfileActions
	{	
		// store data
		private static List<string> threads = new List<string>();
		private static Dictionary<string, Dictionary<string, string>> properties = new Dictionary<string, Dictionary<string, string>>();
		private static Dictionary<string, Dictionary<string, string>> headers = new Dictionary<string, Dictionary<string, string>>();
		
		// extensions
		private const string property_ext = "property";
		private const string header_ext = "header";


		/// <summary>
		/// генерация уникального id для потока
		/// </summary>
		public static string GenerateThreadID() {
			return Guid.NewGuid().ToString("N");
		}
		
		/// <summary>
		/// установка значения свойства профиля
		/// </summary>
		public static void SetProperty(IZennoPosterProjectModel project, string thread_id, string propname, string value) {
			ProfileActions.CheckThreadID(thread_id, true);

			if (!ProfileActions.properties.ContainsKey(thread_id)) {
				ProfileActions.properties[thread_id] = new Dictionary<string, string>();
			}
			if (!ProfileActions.properties[thread_id].ContainsKey(propname)) {
				ProfileActions.properties[thread_id].Add(propname, value);
			} else {
				ProfileActions.properties[thread_id][propname] = value;
			}
			
			try {
				PropertyInfo PI_prop = typeof(IProfile).GetProperty(propname);
				PI_prop.SetValue(project.Profile, Convert.ChangeType(value, PI_prop.PropertyType), null);
			} catch {
				throw new Exception(String.Format(
					"[ProfileActions.SetProperty]: error set property '{0}' with value '{1}'!",
					propname,
					value
				));
			}
		}
		
		/// <summary>
		/// получения значения свойства профиля
		/// </summary>
		public static string GetProperty(string thread_id, string propname) {
			ProfileActions.CheckThreadID(thread_id);

			if (!ProfileActions.properties[thread_id].ContainsKey(propname)) {
				throw new Exception(String.Format("[ProfileActions.GetProperty]: property '{0}' not found!", propname));
			}
			return ProfileActions.properties[thread_id][propname];
		}

		/// <summary>
		/// установка значения заголовка инстанса
		/// </summary>
		/// <remarks>
		/// The method call is made before calling any other methods of an Instance object.
		/// </remarks>
		public static void SetHeader(Instance instance, string thread_id, string headername, string value, bool is_navigator_field=true) {
			ProfileActions.CheckThreadID(thread_id, true);

			if (!ProfileActions.headers.ContainsKey(thread_id)) {
				ProfileActions.headers[thread_id] = new Dictionary<string, string>();
			}
			
			string headername_key = headername;
			if (!is_navigator_field) {
				headername_key = String.Concat(headername, "+"); // small hack: define *string* header names (example: "HTTP_USER_AGENT")
			}
			
			if (!ProfileActions.headers[thread_id].ContainsKey(headername_key)) {
				ProfileActions.headers[thread_id].Add(headername_key, value);
			} else {
				ProfileActions.headers[thread_id][headername_key] = value;
			}
			
			if (!is_navigator_field) {
				instance.SetHeader(headername, value);
			} else {
				try {
					var field = (ZennoLab.InterfacesLibrary.Enums.Browser.NavigatorField) Enum.Parse(
						typeof(ZennoLab.InterfacesLibrary.Enums.Browser.NavigatorField),
						headername
					);
					instance.SetHeader(field, value);
				} catch {
					throw new Exception(String.Format(
						"[ProfileActions.SetHeader]: error set header '{0}' with value '{1}'!",
						headername,
						value
					));
				}
			}
		}
		
		/// <summary>
		/// получения значения заголовка инстанса (приватный метод, т.к. в ZP нельзя получить значение через C#)
		/// </summary>
		private static string GetHeader(string thread_id, string headername) {
			ProfileActions.CheckThreadID(thread_id);

			if (!ProfileActions.headers[thread_id].ContainsKey(headername)) {
				throw new Exception(String.Format("[ProfileActions.GetHeader]: header '{0}' not found!", headername));
			}
			return ProfileActions.headers[thread_id][headername];
		}
		
		/// <summary>
		/// сохранения профиля со свойствами и заголовками инстанса
		/// </summary>
		/// <remarks>
		/// destroy_thread param default set to false. Be careful!
		/// </remarks>
		public static void Save(
			IZennoPosterProjectModel project,
			string thread_id,
			string path,
			bool saveProxy=false,
			bool savePlugins=false,
			bool saveLocalStorage=false,
			bool saveTimezone=false,
			bool saveGeoposition=false,
			bool destroy_thread=false
		) {
			// закомментирую это, т.к. до конца не ясно - нужно ли вызывать этот метод тут
			// если не задавать свойств и заголовков, то будет валиться ошибка
			// хотя по сути проверка идет дальше в getter'ах
			// ProfileActions.CheckThreadID(thread_id);
			
			// сохраняем профиль стандартным методом (чтоб сохранить куки и т.д.)
			project.Profile.Save(path, saveProxy, savePlugins, saveLocalStorage, saveTimezone, saveGeoposition);

			// сохраняем свойства профиля и заголовки инстанса - каждый в свой файл
			if ((ProfileActions.properties.Count > 0) || (ProfileActions.headers.Count > 0)) {
				using (var zipToOpen = new FileStream(path, FileMode.Open)) {
					using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)) {
						// свойства профиля
						foreach(var propname in ProfileActions.properties[thread_id].Keys) {
							var entry_name = String.Concat(propname, ".", ProfileActions.property_ext);
							ZipArchiveEntry entry = archive.CreateEntry(entry_name);
							using (var writer = new StreamWriter(entry.Open())) {
								var propvalue = ProfileActions.GetProperty(thread_id, propname);
								writer.Write(propvalue);
							}
						}
						
						// заголовки инстанса
						foreach(var headername in ProfileActions.headers[thread_id].Keys) {
							var entry_name = String.Concat(headername, ".", ProfileActions.header_ext);
							ZipArchiveEntry entry = archive.CreateEntry(entry_name);
							using (var writer = new StreamWriter(entry.Open())) {
								var headervalue = ProfileActions.GetHeader(thread_id, headername);
								writer.Write(headervalue);
							}
						}
					}
				}
			}
	
			// можно прострелить себе ногу, т.к. при повторном сохранении с удалением данных о потоке
			// НЕ сохранятся заголовки инстанса и свойства профиля, т.к. будут удалены
			if (destroy_thread) {
				ProfileActions.DestroyThreadID(thread_id);
			}
		}
		
		/// <summary>
		/// загрузка профиля с простановкой свойств и заголовков инстанса
		/// </summary>
		public static void Load(
			IZennoPosterProjectModel project,
			Instance instance,
			string thread_id,
			string path
		) {
			// закомментирую это, т.к. до конца не ясно - нужно ли вызывать этот метод тут
			// хотя по сути проверка идет дальше в setter'ах
			// ProfileActions.CheckThreadID(thread_id, true);
			
			if (!File.Exists(path)) {
				throw new Exception(String.Format("[ProfileActions.Load]: path '{0}' not exists!", path));
			}
			
			// загрузка профиля
			project.Profile.Load(path);
			
			// for window size
			int ScreenSizeWidth = 0;
			int ScreenSizeHeight = 0;
			
			// чтение и установка свойств профиля и заголовков инстанса
			using (var zipToOpen = new FileStream(path, FileMode.Open)) {
				using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read)) {
					foreach (ZipArchiveEntry entry in archive.Entries) {
						var filename = entry.FullName.Split(new char[] {'.'}); // TODO: fix this
						string name = filename[0];
						string file_ext = filename[1];

						switch (file_ext) {
							case ProfileActions.property_ext: // свойства профиля
								using (var reader = new StreamReader(entry.Open())) {
									string value = reader.ReadLine();
									ProfileActions.SetProperty(project, thread_id, name, value);
									
									if (name == "ScreenSizeWidth") {
										ScreenSizeWidth = Convert.ToInt32(value);
									}
									if (name == "ScreenSizeHeight") {
										ScreenSizeHeight = Convert.ToInt32(value);
									}
								}
								break;
							case ProfileActions.header_ext: // заголовки инстанса
								using (var reader = new StreamReader(entry.Open())) {
									string value = reader.ReadLine();
									bool is_navigator_field = (name[name.Length-1] != '+');
									if (!is_navigator_field) {
										name = name.Substring(0, name.Length-1); // small hack
									}
									ProfileActions.SetHeader(instance, thread_id, name, value, is_navigator_field);
								}
								break;
							default:
								break;
						}
					}
				}
			}
			
			// установка размеров окна инстанса (вроде бы работает только в ZP)
			if ((ScreenSizeWidth > 0) && (ScreenSizeHeight > 0)) {
				instance.SetWindowSize(ScreenSizeWidth, ScreenSizeHeight);
			}
		}
		
		/// <summary>
		/// проверка на присутствие id потока
		/// </summary>
		private static void CheckThreadID(string thread_id, bool create_if_not_exists=false) {
			if (String.IsNullOrEmpty(thread_id)) {
				throw new Exception("[ProfileActions]: thread_id is empty!");
			}
			if (!ProfileActions.threads.Contains(thread_id)) {
				if (!create_if_not_exists) {
					throw new Exception(String.Format("[ProfileActions]: thread_id with value '{0}' not found!", thread_id));
				} else {
					ProfileActions.threads.Add(thread_id);
				}
			}
		}
		
		/// <summary>
		/// Garbage Collector (чтоб не засрать память и ничего не потекло)
		/// </summary>
		public static void DestroyThreadID(string thread_id) {
			// пока что не ясно нужно ли это тут, скорее всего не нужно
			// ProfileActions.CheckThreadID(thread_id);

			ProfileActions.threads.Remove(thread_id);		// false if not exists
			ProfileActions.properties.Remove(thread_id);	// false if not exists
			ProfileActions.headers.Remove(thread_id);		// false if not exists
		}
	}
}