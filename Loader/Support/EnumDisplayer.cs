using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Data;
using System.Windows.Markup;

namespace Elect.Loader.Support
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DisplayStringAttribute : Attribute
	{
		private readonly string _value;

		public DisplayStringAttribute(string v)
		{
			_value = v;
		}

		public DisplayStringAttribute()
		{
		}

		public string Value
		{
			get { return _value; }
		}

		public string ResourceKey { get; set; }
	}

	public class EnumDisplayEntry
	{
		public string EnumValue { get; set; }
		public string DisplayString { get; set; }
		public bool ExcludeFromDisplay { get; set; }
	}

	[ContentProperty("OverriddenDisplayEntries")]
	public class EnumDisplayer : IValueConverter
	{
		private IDictionary _displayValues;
		private List<EnumDisplayEntry> _overriddenDisplayEntries;
		private IDictionary _reverseValues;
		private Type _type;

		public EnumDisplayer() { }

		public EnumDisplayer(Type type)
		{
			Type = type;
			initializeDisplayValues();
		}

		public Type Type
		{
			get { return _type; }
			set
			{
				if (!value.IsEnum)
					throw new ArgumentException("Parameter is not an Enumermated type", "value");
				_type = value;
			}
		}

		private void initializeDisplayValues()
		{
			Type displayValuesType = typeof(Dictionary<,>)
				.GetGenericTypeDefinition().MakeGenericType(Type, typeof(string));
			_displayValues = (IDictionary)Activator.CreateInstance(displayValuesType);
		}

		public ReadOnlyCollection<string> DisplayNames
		{
			get
			{
				initializeDisplayValues();

				_reverseValues =
					(IDictionary) Activator.CreateInstance(typeof (Dictionary<,>)
					                                       	.GetGenericTypeDefinition()
					                                       	.MakeGenericType(typeof (string), _type));

				FieldInfo[] fields = _type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (FieldInfo field in fields)
				{
					var a = (DisplayStringAttribute[])
					        field.GetCustomAttributes(typeof (DisplayStringAttribute), false);

					string displayString = getDisplayStringValue(a);
					object enumValue = field.GetValue(null);

					if (displayString == null)
					{
						displayString = getBackupDisplayStringValue(enumValue);
					}
					if (displayString != null)
					{
						_displayValues.Add(enumValue, displayString);
						_reverseValues.Add(displayString, enumValue);
					}
				}
				return new List<string>((IEnumerable<string>) _displayValues.Values).AsReadOnly();
			}
		}

		public List<EnumDisplayEntry> OverriddenDisplayEntries
		{
			get { return _overriddenDisplayEntries ?? (_overriddenDisplayEntries = new List<EnumDisplayEntry>()); }
		}

		#region IValueConverter Members

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (_displayValues == null)
				return null;
			return _displayValues[value];
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (_reverseValues == null)
				return null;
			return _reverseValues[value];
		}

		#endregion

		private string getBackupDisplayStringValue(object enumValue)
		{
			if (_overriddenDisplayEntries != null && _overriddenDisplayEntries.Count > 0)
			{
				EnumDisplayEntry foundEntry = _overriddenDisplayEntries.Find(delegate(EnumDisplayEntry entry)
				                                                            	{
				                                                            		object e = Enum.Parse(_type, entry.EnumValue);
				                                                            		return enumValue.Equals(e);
				                                                            	});
				if (foundEntry != null)
				{
					if (foundEntry.ExcludeFromDisplay) return null;
					return foundEntry.DisplayString;
				}
			}
			return Enum.GetName(_type, enumValue);
		}

		private string getDisplayStringValue(DisplayStringAttribute[] a)
		{
			if (a == null || a.Length == 0) return null;
			DisplayStringAttribute dsa = a[0];
			if (!string.IsNullOrEmpty(dsa.ResourceKey))
			{
				var rm = new ResourceManager(_type);
				return rm.GetString(dsa.ResourceKey);
			}
			return dsa.Value;
		}
	}
}