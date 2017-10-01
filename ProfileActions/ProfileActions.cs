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
		// блокировка при сохранении/загрузке,
		// чтобы не попортить данные
		private static Object FileLock = new Object();
		
		// store data
		[ThreadStatic] private static Dictionary<string, string> properties;
		[ThreadStatic] private static Dictionary<string, string> headers;

		// extensions
		private const string property_ext = "property";
		private const string header_ext = "header";
		
		
		/// <summary>
		/// инициализация свойств профиля для многопотчного режима
		/// (из-за ThreadStatic: http://stackoverflow.com/questions/18086235/initializing-threadstatic-field-still-causes-nullreferenceexception )
		/// </summary>
		///
		private static void InitProperies() {
			if (ProfileActions.properties == null) {
				ProfileActions.properties = new Dictionary<string, string>();
			}
		}
		
		/// <summary>
		/// инициализация заголовков инстанса для многопотчного режима
		/// (из-за ThreadStatic: http://stackoverflow.com/questions/18086235/initializing-threadstatic-field-still-causes-nullreferenceexception )
		/// </summary>
		///
		private static void InitHeaders() {
			if (ProfileActions.headers == null) {
				ProfileActions.headers = new Dictionary<string, string>();
			}
		}

		/// <summary>
		/// установка значения свойства профиля
		/// </summary>
		public static void SetProperty(IZennoPosterProjectModel project, string propname, string value) {
			ProfileActions.InitProperies();
			
			if (!ProfileActions.properties.ContainsKey(propname)) {
				ProfileActions.properties.Add(propname, value);
			} else {
				ProfileActions.properties[propname] = value;
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
		public static string GetProperty(string propname) {
			ProfileActions.InitProperies();
			
			if (!ProfileActions.properties.ContainsKey(propname)) {
				throw new Exception(String.Format("[ProfileActions.GetProperty]: property '{0}' not found!", propname));
			}
			return ProfileActions.properties[propname];
		}

		/// <summary>
		/// установка значения заголовка инстанса
		/// </summary>
		/// <remarks>
		/// The method call is made before calling any other methods of an Instance object.
		/// </remarks>
		public static void SetHeader(Instance instance, string headername, string value, bool is_navigator_field=true) {
			ProfileActions.InitHeaders();
			
			string headername_key = headername;
			if (!is_navigator_field) {
				headername_key = String.Concat(headername, "+"); // small hack: define *string* header names (example: "HTTP_USER_AGENT")
			}
			
			if (!ProfileActions.headers.ContainsKey(headername_key)) {
				ProfileActions.headers.Add(headername_key, value);
			} else {
				ProfileActions.headers[headername_key] = value;
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
		private static string GetHeader(string headername) {
			ProfileActions.InitHeaders();
			
			if (!ProfileActions.headers.ContainsKey(headername)) {
				throw new Exception(String.Format("[ProfileActions.GetHeader]: header '{0}' not found!", headername));
			}
			return ProfileActions.headers[headername];
		}

		/// <summary>
		/// сохранения профиля со свойствами и заголовками инстанса
		/// </summary>
		public static void Save(
			IZennoPosterProjectModel project,
			string path,
			bool saveProxy=false,
			bool savePlugins=false,
			bool saveLocalStorage=false,
			bool saveTimezone=false,
			bool saveGeoposition=false,
			bool saveSuperCookie=false,
			bool saveFonts=false,
			bool saveWebRtc=false,
			bool saveIndexedDb=false
		) {
			lock (ProfileActions.FileLock) {
				// сохраняем профиль стандартным методом (чтоб сохранить куки и т.д.)
				project.Profile.Save(path, saveProxy, savePlugins, saveLocalStorage, saveTimezone, saveGeoposition, saveSuperCookie, saveFonts, saveWebRtc, saveIndexedDb);
	
				// сохраняем свойства профиля и заголовки инстанса - каждый в свой файл
				if ((ProfileActions.properties.Count > 0) || (ProfileActions.headers.Count > 0)) {
					using (var zipToOpen = new FileStream(path, FileMode.Open)) {
						using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)) {
							// свойства профиля
							foreach(var propname in ProfileActions.properties.Keys) {
								var entry_name = String.Concat(propname, ".", ProfileActions.property_ext);
								ZipArchiveEntry entry = archive.CreateEntry(entry_name);
								using (var writer = new StreamWriter(entry.Open())) {
									var propvalue = ProfileActions.GetProperty(propname);
									writer.Write(propvalue);
								}
							}
							
							// заголовки инстанса
							foreach(var headername in ProfileActions.headers.Keys) {
								var entry_name = String.Concat(headername, ".", ProfileActions.header_ext);
								ZipArchiveEntry entry = archive.CreateEntry(entry_name);
								using (var writer = new StreamWriter(entry.Open())) {
									var headervalue = ProfileActions.GetHeader(headername);
									writer.Write(headervalue);
								}
							}
						}
					}
				}
			}
		}
		
		/// <summary>
		/// загрузка профиля с простановкой свойств и заголовков инстанса
		/// </summary>
		public static void Load(
			IZennoPosterProjectModel project,
			Instance instance,
			string path
		) {
			if (!File.Exists(path)) {
				throw new Exception(String.Format("[ProfileActions.Load]: path '{0}' not exists!", path));
			}
			
			lock (ProfileActions.FileLock) {
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
										ProfileActions.SetProperty(project, name, value);
										
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
										ProfileActions.SetHeader(instance, name, value, is_navigator_field);
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
		}
	}
}