﻿///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// System settings handler class
///////////////////////////////////////////////////////////////////////////////
using CommonClassLibrary.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;


namespace CommonClassLibrary.Settings
{
	public class SettingsFileBase
	{
		#region · Constants ·
		private const string RootElementName = "Settings";
		private const string TypeAttributeName = "Type";
		#endregion

		#region · Types ·
		/// <summary>
		/// 
		/// </summary>
		public class ModuleInfo
		{
			public string SectionName;
			public string ModuleName;
			public bool Active;
			public int ModuleIndex;

			public 	ModuleInfo()
			{
				SectionName = "";
				ModuleName = "";
				Active = false;
				ModuleIndex = 0;
			}

			public 	ModuleInfo(string in_section_name)
			{
				SectionName = in_section_name;
				ModuleName = "";
				Active = false;
				ModuleIndex = 0;
			}

			public 	ModuleInfo(string in_secion_name, string in_module_name, bool in_active)
			{
				SectionName = in_secion_name;
				ModuleName = in_module_name;
				Active = in_active;
				ModuleIndex = 0;
			}

			public ModuleInfo(ModuleInfo in_module_info)
			{
				SectionName = in_module_info.SectionName;
				ModuleName = in_module_info.ModuleName;
				Active = in_module_info.Active;
				ModuleIndex = 0;
			}

			public override string ToString()
			{
				return SectionName;
			}

			public override bool Equals(object in_object)
			{
				// If this and obj do not refer to the same type, then they are not equal.
				if (in_object.GetType() != this.GetType())
					return false;

				return (SectionName == ((ModuleInfo)in_object).SectionName);
			}

			public override int GetHashCode()
			{
				return SectionName.GetHashCode();
			}
		}
		#endregion

		#region · Data members ·
		private XmlDocument m_xml_doc = null;
		private string m_config_filename;
		private Dictionary<string, SettingsBase> m_settings = new Dictionary<string, SettingsBase>();
		private List<ModuleInfo> m_modules = new List<ModuleInfo>();

		#endregion

		#region · Constructor ·
		// Default constructor
		public SettingsFileBase()
		{
			m_xml_doc = new XmlDocument();
		}
		#endregion

		#region · Properties ·

		/// <summary>
		/// Name of the current configuration file
		/// </summary>
		public string ConfigFileName
		{
			get { return m_config_filename; }
			set { m_config_filename = value; }
		}

		#endregion

		#region · Public members ·

		/// <summary>
		/// Adds content of a settings class to the settings file
		/// </summary>
		/// <param name="in_settings_data"></param>
		public void SetSettings(SettingsBase in_settings_data)
		{
			string key = GetSettigsKey(in_settings_data);

			// replace setings to the new class
			if(m_settings.ContainsKey(key))
				m_settings.Remove(key);

			m_settings.Add(key, in_settings_data);
		}

		/// <summary>
		/// Gets settings class
		/// </summary>
		/// <param name="in_settings_data"></param>
		/// <returns></returns>
		public T GetSettings<T>() where T:SettingsBase, new()
		{
			XmlNode root_node = null;
			bool success = true;
			XmlNode settings_node = null;
			T result = new T();

			// check if settings exists in the dictionary
			string key = GetSettigsKey(result);
			if (m_settings.ContainsKey(key))
			{
				result = (T)m_settings[key];
			}
			else
			{
				// check if the settings exists in the XML
				if (m_xml_doc == null)
					success = false;

				// check XML file
				if (success)
				{
					root_node = m_xml_doc.DocumentElement;

					if (root_node == null || root_node.Name != RootElementName)
						success = false;
				}

				// select settings node
				if (success)
				{
					settings_node = root_node.SelectSingleNode('/' + RootElementName + '/' + result.ModuleName + '/' + result.SectionName);
					if (settings_node == null)
						success = false;
				}

				// deserialize settings data
				if (success)
				{
					DeserializeEntry(settings_node, result);
				}

				// store settings or set to default values
				if (success)
					m_settings.Add(key, (SettingsBase)result);
				else
					result.SetDefaultValues();
			}

			return result;
		}

		/// <summary>
		/// Generates the list of the modules stored in config file
		/// </summary>
		/// <returns>List of the modules</returns>
		public List<ModuleInfo> GetModuleList()
		{
			return m_modules;
		}

		public void ModuleAdd(ModuleInfo in_module_info)
		{
			m_modules.Add(in_module_info);
		}

		public void ModuleRemove()
		{

		}
		/*
		public void ModuleActivate(ModuleInfo in_module)
		{
			try
			{
				XmlNode root = m_xml_doc.DocumentElement;
				XmlNodeList node_list = root.SelectNodes('/' + in_module.SectionName + "/*");

				if(in_module.ModuleIndex < node_list.Count)
				{
					node_list[in_module.ModuleIndex].Attributes["Active"].Value = "true";
				}
			}
			catch
			{
				// error occured
			}

		}
		*/
		/// <summary>
		/// Loads settings file
		/// </summary>
		public bool Load()
		{
			bool success = true;

			// get file name
			m_config_filename = GetConfigFileName();

			m_settings.Clear();
			m_modules.Clear();

			try
			{
				// load XML settings
				m_xml_doc = new XmlDocument();

				m_xml_doc.Load(m_config_filename);
			}
			catch
			{
				m_xml_doc = null;
					
				success = false;
			}

			if(m_xml_doc != null)
			{
				// cache module information
				try
				{
					XmlNode root = m_xml_doc.DocumentElement;
					XmlNodeList node_list = root.SelectNodes('/' + RootElementName + "/*");

					// Get names
					for (int i = 0; i < node_list.Count; i++)
					{
						// Skip module creation for "Main" module
						if (node_list[i].Name != "Main")
						{
							ModuleInfo entry = new ModuleInfo();

							entry.SectionName = node_list[i].Name;
							entry.ModuleName = node_list[i].Attributes["ModuleName"].InnerText;
							bool.TryParse(node_list[i].Attributes["Active"].InnerText, out entry.Active);

							int index = m_modules.LastIndexOf(entry);
							if(index == -1)
								entry.ModuleIndex = 0;
							else
								entry.ModuleIndex = index + 1;

							m_modules.Add(entry);
						}
					}
				}
				catch
				{
					success = false;
				}
			}

			// if loading was unsuccessfull create empty xml document
			if (!success)
			{
				// error occured -> create empty xml
				m_xml_doc = new XmlDocument();

				XmlNode xml_declaration = m_xml_doc.CreateXmlDeclaration("1.0", "UTF-8", "");
				m_xml_doc.AppendChild(xml_declaration);

				XmlNode top_level_parent_node = m_xml_doc.CreateElement(RootElementName);
				m_xml_doc.AppendChild(top_level_parent_node);

				XmlNode main_settings = m_xml_doc.CreateElement("Main");
				top_level_parent_node.AppendChild(main_settings);
			}

			return success;
		}

		/// <summary>
		/// Saves settings file
		/// </summary>
		public void Save()
		{
			XmlNode top_level_parent_node;

			// create an empty settings document
			if (m_xml_doc == null)
			{
				m_xml_doc = new XmlDocument();

				XmlNode xml_declaration = m_xml_doc.CreateXmlDeclaration("1.0", "UTF-8", "");
				m_xml_doc.AppendChild(xml_declaration);
			}

			// create root element if does not exists
			top_level_parent_node = m_xml_doc.LastChild;

			// create parent node if there is no parent
			if (top_level_parent_node == null)
			{
				top_level_parent_node = m_xml_doc.CreateElement(RootElementName);
				m_xml_doc.AppendChild(top_level_parent_node);
			}

			// create module objects
			foreach(ModuleInfo module in m_modules)
			{
				XmlNodeList node_list = m_xml_doc.DocumentElement.SelectNodes('/' + RootElementName + '/' + module.SectionName);

				if(module.ModuleIndex >= node_list.Count)
				{
					// create a new node
					XmlNode module_node = m_xml_doc.CreateElement(module.SectionName);

					// create 'Active' attribute
					XmlAttribute active = m_xml_doc.CreateAttribute("Active");
					active.Value = module.Active.ToString();
					module_node.Attributes.Append(active);

					// create 'ModuleName' attribute
					XmlAttribute module_name = m_xml_doc.CreateAttribute("ModuleName");
					module_name.Value = module.ModuleName;
					module_node.Attributes.Append(module_name);

					m_xml_doc.DocumentElement.AppendChild(module_node);
				}
				else
				{
					// update a node
					node_list[module.ModuleIndex].Attributes["Active"].InnerText = module.Active.ToString();
				}

			}

			// serialize settings objects
			foreach (KeyValuePair<string, SettingsBase> settings in m_settings)
			{
				XmlNode module_node = top_level_parent_node.SelectSingleNode(settings.Value.ModuleName);
				XmlNode section_node = top_level_parent_node.SelectSingleNode(settings.Value.ModuleName + "/" + settings.Value.SectionName);

				if (module_node != null && section_node != null)
					module_node.RemoveChild(section_node);

				WriteEntry(top_level_parent_node, settings.Value, null);
			}

			// save XML
			m_xml_doc.Save(m_config_filename);
		}

		/// <summary>
		/// Copies settings from another settings class
		/// </summary>
		/// <param name="in_settings"></param>
		public void CopySettingsFrom(SettingsFileBase in_settings)
		{
			// clone XML doc
			m_xml_doc = (XmlDocument)in_settings.m_xml_doc.Clone();

			// clone cached settings
			m_settings.Clear();
			foreach (KeyValuePair<string, SettingsBase> settings in in_settings.m_settings)
			{
				m_settings.Add(settings.Key, settings.Value);
			}

			// clone module info
			m_modules.Clear();
			for (int i = 0; i < in_settings.m_modules.Count; i++)
			{
				m_modules.Add(new ModuleInfo(in_settings.m_modules[i]));
			}
		}

		#endregion

		#region · Private members ·

		/// <summary>
		/// Generates settings key from module and section name
		/// </summary>
		/// <param name="in_settings"></param>
		/// <returns></returns>
		private string GetSettigsKey(SettingsBase in_settings)
		{
			return in_settings.ModuleName + '+' + in_settings.SectionName;
		}

		/// <summary>
		/// Gets object's member value based on member info
		/// </summary>
		/// <param name="in_info"></param>
		/// <param name="in_object"></param>
		/// <returns></returns>
		private object GetObjectMember(MemberInfo in_info, object in_object)
		{
			switch (in_info.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo)in_info).GetValue(in_object);

				case MemberTypes.Property:
					return ((PropertyInfo)in_info).GetValue(in_object, null);
			}

			return null;
		}

		/// <summary>
		/// Sets object"s member value
		/// </summary>
		/// <param name="in_info"></param>
		/// <param name="in_object"></param>
		/// <param name="in_value"></param>
		/// <param name="in_index"></param>
		private void SetObjectMember(MemberInfo in_info, object in_object, object in_value, int? in_index = null)
		{
			// check if array element
			if (in_index != null)
			{
				// set array element
				Type type = ((Array)in_object).GetType().GetElementType();

				((Array)in_object).SetValue(Convert.ChangeType(in_value, type, CultureInfo.InvariantCulture), (int)in_index);
			}
			else
			{
				// set if field
				if (in_info is FieldInfo)
				{
					if (((FieldInfo)in_info).FieldType.IsEnum)
					{
						((FieldInfo)in_info).SetValue(in_object, Enum.Parse(((FieldInfo)in_info).FieldType, in_value as string));
					}
					else
					{
						((FieldInfo)in_info).SetValue(in_object, Convert.ChangeType(in_value, ((FieldInfo)in_info).FieldType, CultureInfo.InvariantCulture));
					}
				}
				else
				{
					// set if property
					if (in_info is PropertyInfo)
					{
						((PropertyInfo)in_info).SetValue(in_object, Convert.ChangeType(in_value, ((PropertyInfo)in_info).PropertyType, CultureInfo.InvariantCulture), null);
					}
				}
			}
		}

		/// <summary>
		/// Deserializes enty
		/// </summary>
		/// <param name="in_retval"></param>
		private void DeserializeEntry(XmlNode in_node, object in_object, int? in_index = null)
		{
			XmlNode node;
			Type type = in_object.GetType();

			MemberInfo[] member_info = GetSerializableMembers(type);

			// deserialize all members
			foreach (MemberInfo member in member_info)
			{
				node = in_node.SelectSingleNode(member.Name);
				if (node != null)
				{
					object value;

					value = GetObjectMember(member, in_object);
					if (value != null)
					{
						if (value.GetType().IsPrimitive || value is string || value is Enum)
						{
							SetObjectMember(member, in_object, node.InnerText);
						}
						else
						{
							DeserializeEntry(node, value);
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets settings class members to serialize
		/// </summary>
		/// <param name="in_type"></param>
		/// <returns></returns>
		protected MemberInfo[] GetSerializableMembers(Type in_type)
		{
			MemberInfo[] member_info;

			if (in_type.IsSerializable)
			{
				// if 'Serializable' attribute is defined -> select all 'serializable' members
				member_info = FormatterServices.GetSerializableMembers(in_type);
			}
			else
			{
				// if 'DataContract' attribute is specified -> select all members which has 'DataMember' attribute
				DataContractAttribute[] attributes = (DataContractAttribute[])in_type.GetCustomAttributes(typeof(DataContractAttribute), true);
				if (attributes != null && attributes.Length > 0)
				{
					// get members with 'DataMember' attribute
					MemberInfo[] field_info = in_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.GetField).Where(p => p.GetCustomAttributes(typeof(DataMemberAttribute), true).Count() != 0).ToArray();
					MemberInfo[] property_info = in_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty).Where(p => p.GetCustomAttributes(typeof(DataMemberAttribute), true).Count() != 0).ToArray();

					member_info = field_info.Concat(property_info).ToArray();
				}
				else
				{
					// no attribute specified -> select public members
					member_info = in_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField ).Where(p => p.GetCustomAttributes(typeof(NonSerializedAttribute), true).Count() == 0).ToArray(); 
					member_info = member_info.Concat(in_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty)).ToArray().Where(p => p.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Count() == 0).ToArray();
				}
			}

			// retrun member info
			return member_info;
		}

		/// <summary>
		/// Writes entry to the binary file and recursivelly calls itself for all elements of a collection types
		/// </summary>
		/// <param name="in_object"></param>
		/// <param name="in_name"></param>
		private void WriteEntry(XmlNode in_parent, object in_object, string in_name = null)
		{
			XmlNode child = null;

			if (in_object == null)
			{
				WriteValue(in_parent, in_name, "null");
			}
			else if (in_object is sbyte || in_object is byte || in_object is short || in_object is ushort || in_object is int || in_object is uint || in_object is long || in_object is ulong || in_object is decimal || in_object is double || in_object is float)
			{
				WriteValue(in_parent, in_name, Convert.ToString(in_object, NumberFormatInfo.InvariantInfo));
			}
			else if (in_object is bool)
			{
				WriteValue(in_parent, in_name, in_object.ToString().ToLower());
			}
			else if (in_object is char || in_object is Enum || in_object is Guid)
			{
				WriteValue(in_parent, in_name, "" + in_object);
			}
			else if (in_object is DateTime)
			{
				WriteValue(in_parent, in_name, ((DateTime)in_object - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString("0"));
			}
			else if (in_object is string)
			{
				WriteValue(in_parent, in_name, (string)in_object);
			}
			else if (in_object is IPAddress)
			{
				WriteValue(in_parent, in_name, ((IPAddress)in_object).ToString());
			}
			else if (in_object is Array)
			{
				// get item type
				string type_name = in_object.GetType().FullName;

				// create array element
				child = CreateXMLNode(type_name, in_name);
				in_parent.AppendChild(child);

				// deserialize array elements
				foreach (object obj in (in_object as Array))
				{
					WriteEntry(child, obj);
				}
			}
			else
			{
				// get item name
				string type_name = in_object.GetType().FullName;

				// determine element name
				string name = in_name;
				XmlNode parent_node = in_parent;

				if (in_object is SettingsBase)
				{
					// ISystemSettings shoud be the top level settings object
					if (parent_node.Name != RootElementName)
						throw new ArgumentException("Invalid XML parent for IsystemSettings object");

					string module_name = ((SettingsBase)in_object).ModuleName;

					// check if module is exists
					XmlNode module_node = m_xml_doc.SelectSingleNode('/' + RootElementName + '/' + module_name);
					if (module_node == null)
					{
						// create module node
						module_node = m_xml_doc.CreateElement(module_name);
						in_parent.AppendChild(module_node);
					}

					parent_node = module_node;

					name = ((SettingsBase)in_object).SectionName;
				}

				// create child element
				child = CreateXMLNode(type_name, name);
				parent_node.AppendChild(child);

				// store fields fields
				MemberInfo[] members = GetSerializableMembers(in_object.GetType());
				foreach (MemberInfo member in members)
				{

					switch (member.MemberType)
					{
						case MemberTypes.Field:
							WriteEntry(child, ((FieldInfo)member).GetValue(in_object), member.Name);
							break;

						case MemberTypes.Property:
							WriteEntry(child, ((PropertyInfo)member).GetValue(in_object, null), member.Name);
							break;
					}
				}
			}
		}


		/// <summary>
		/// Creates an XML node
		/// </summary>
		/// <param name="in_type">Type of the node (stored as an attribute)</param>
		/// <param name="in_name">Name of the node</param>
		/// <returns>The newly created node</returns>
		private XmlNode CreateXMLNode(string in_type, string in_name)
		{
			XmlNode node;
			string element_name;

			// if name is not specified use type
			if (string.IsNullOrEmpty(in_name))
			{

				int pos = in_type.LastIndexOf('+');

				if (pos != -1)
				{
					element_name = in_type.Substring(pos + 1, in_type.Length - pos - 1);
				}
				else
				{
					element_name = in_type;
				}
			}
			else
			{
				element_name = in_name;
			}

			// create node
			node = m_xml_doc.CreateNode(XmlNodeType.Element, element_name, null);

			// append type as an attribute
			if (!string.IsNullOrEmpty(in_type))
			{
				//((XmlElement)node).SetAttribute(XMLTypeAttributeName, in_type);
			}

			return node;
		}

		/// <summary>
		/// Writes value to the output stream
		/// </summary>
		/// <param name="in_type">Type string</param>
		/// <param name="in_name">Name string</param>
		/// <param name="in_value">Value string</param>
		private void WriteValue(XmlNode in_parent, string in_name, string in_value)
		{
			XmlNode child = null;

			// create value element
			child = in_parent.OwnerDocument.CreateNode(XmlNodeType.Element, in_name, null);
			child.InnerText = in_value;
			in_parent.AppendChild(child);
		}


		/// <summary>
		/// Gets configuration file name
		/// </summary>
		/// <returns>File name</returns>
		static public string GetConfigFileName()
		{
			string application_name = SystemPaths.GetExecutableName();
			RegistryKey software = Registry.CurrentUser.OpenSubKey("Software");
			RegistryKey cygnus_ground_station = software.OpenSubKey(application_name);

			if (cygnus_ground_station != null && cygnus_ground_station.GetValue("SettingsFile") != null)
				return cygnus_ground_station.GetValue("SettingsFile").ToString();
			else
			{
				string cygnus_directory = SystemPaths.GetApplicationDataPath();

				return Path.Combine(cygnus_directory, application_name + ".config");
			}
		}
		#endregion
	}
}

