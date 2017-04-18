using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ProBuilder2.Common
{
	/**
	 * Store settings in a project-local manner.
	 */
	public class pb_PreferenceDictionary :
		ScriptableObject,
		IEnumerable,
		ISerializationCallbackReceiver,
		pb_IHasDefault
	{
		private Dictionary<string, bool> 		m_Bool 		= new Dictionary<string, bool>();
		private Dictionary<string, int> 		m_Int 		= new Dictionary<string, int>();
		private Dictionary<string, float> 		m_Float 	= new Dictionary<string, float>();
		private Dictionary<string, string> 		m_String 	= new Dictionary<string, string>();
		private Dictionary<string, Color> 		m_Color 	= new Dictionary<string, Color>();
		private Dictionary<string, Material> 	m_Material 	= new Dictionary<string, Material>();

		[SerializeField] List<string> 	m_Bool_keys;
		[SerializeField] List<string>	m_Int_keys;
		[SerializeField] List<string>	m_Float_keys;
		[SerializeField] List<string>	m_String_keys;
		[SerializeField] List<string>	m_Color_keys;
		[SerializeField] List<string>	m_Material_keys;

		[SerializeField] List<bool> 	m_Bool_values;
		[SerializeField] List<int> 		m_Int_values;
		[SerializeField] List<float> 	m_Float_values;
		[SerializeField] List<string> 	m_String_values;
		[SerializeField] List<Color> 	m_Color_values;
		[SerializeField] List<Material> m_Material_values;

		/**
		 *	Perform the ritual "Please Serialize My Dictionary" dance.
		 */
		public void OnBeforeSerialize()
		{
			m_Bool_keys 		= m_Bool.Keys.ToList();
			m_Int_keys 			= m_Int.Keys.ToList();
			m_Float_keys 		= m_Float.Keys.ToList();
			m_String_keys 		= m_String.Keys.ToList();
			m_Color_keys 		= m_Color.Keys.ToList();

			m_Bool_values 		= m_Bool.Values.ToList();
			m_Int_values 		= m_Int.Values.ToList();
			m_Float_values 		= m_Float.Values.ToList();
			m_String_values 	= m_String.Values.ToList();
			m_Color_values 		= m_Color.Values.ToList();
		}

		/**
		 *	Reconstruct preference dictionaries from serialized lists.
		 */
		public void OnAfterDeserialize()
		{
			for(int i = 0; i < m_Bool_keys.Count; i++)
				m_Bool.Add(m_Bool_keys[i], m_Bool_values[i]);

			for(int i = 0; i < m_Int_keys.Count; i++)
				m_Int.Add(m_Int_keys[i], m_Int_values[i]);

			for(int i = 0; i < m_Float_keys.Count; i++)
				m_Float.Add(m_Float_keys[i], m_Float_values[i]);

			for(int i = 0; i < m_String_keys.Count; i++)
				m_String.Add(m_String_keys[i], m_String_values[i]);

			for(int i = 0; i < m_Color_keys.Count; i++)
				m_Color.Add(m_Color_keys[i], m_Color_values[i]);
		}

		public int Length { get { return 6; } }

		// Implementation for the GetEnumerator method.
		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator) GetEnumerator();
		}

		public pb_PreferenceDictionaryEnumerator GetEnumerator()
		{
			return new pb_PreferenceDictionaryEnumerator(this);
		}

		/**
		 *	Clear dictionary values.
		 */
		public void SetDefaultValues()
		{
			m_Bool.Clear();
			m_Int.Clear();
			m_Float.Clear();
			m_String.Clear();
			m_Color.Clear();
			m_Material.Clear();
		}

		/**
		 *	Check if a key is contained within any type dictionary.
		 */
		public bool HasKey(string key)
		{
			return 	m_Bool.ContainsKey(key) ||
					m_Int.ContainsKey(key) ||
					m_Float.ContainsKey(key) ||
					m_String.ContainsKey(key) ||
					m_Color.ContainsKey(key) ||
					m_Material.ContainsKey(key);
		}

		/**
		 *	Generic Get preference for key function.
		 */
		public T Get<T>(string key, T fallback = default(T))
		{
			System.Type type = typeof(T);

			if(type == typeof(int))
			{
				if(m_Int.ContainsKey(key))
					return (T) (object) GetInt(key);
			}
			else if(type == typeof(float))
			{
				if(m_Float.ContainsKey(key))
					return (T) (object) GetFloat(key);
			}
			else if(type == typeof(bool))
			{
				if(m_Bool.ContainsKey(key))
					return (T) (object) GetBool(key);
			}
			else if(type == typeof(string))
			{
				if(m_String.ContainsKey(key))
					return (T) (object) GetString(key);
			}
			else if(type == typeof(Color))
			{
				if(m_Color.ContainsKey(key))
					return (T) (object) GetColor(key);
			}
			else if(type == typeof(Material))
			{
				if(m_Material.ContainsKey(key))
					return (T) (object) GetMaterial(key);
			}
			else
			{
				Debug.LogWarning(string.Format("Get<{0}>({1}) not valid preference type.",
					typeof(T).ToString(),
					key));
			}

			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public bool GetBool(string key, bool fallback = default(bool))
		{
			bool value;
			if(m_Bool.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public int GetInt(string key, int fallback = default(int))
		{
			int value;
			if(m_Int.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public float GetFloat(string key, float fallback = default(float))
		{
			float value;
			if(m_Float.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public string GetString(string key, string fallback = default(string))
		{
			string value;
			if(m_String.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public Color GetColor(string key, Color fallback = default(Color))
		{
			Color value;
			if(m_Color.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 * Fetch a value from the stored preferences.  If key is not found, a default value is returned.
		 */
		public Material GetMaterial(string key, Material fallback = default(Material))
		{
			Material value;
			if(m_Material.TryGetValue(key, out value))
				return value;
			return fallback;
		}

		/**
		 *	Generic set value.  Only accepts:
		 *	int,
		 *	float,
		 *	bool,
		 *	string,
		 *	Color,
		 *	Material
		 */
		public void Set<T>(string key, T value)
		{
			object o = (object) value;

			if(value is int)
				SetInt(key, (int) o);
			else if(value is float)
				SetFloat(key, (float) o);
			else if(value is bool)
				SetBool(key, (bool) o);
			else if(value is string)
				SetString(key, (string) o);
			else if(value is Color)
				SetColor(key, (Color) o);
			else if(value is Material)
				SetMaterial(key, (Material) o);
			else
				Debug.LogWarning(string.Format("Set<{0}>({1}, {2}) not valid preference type.",
					typeof(T).ToString(),
					key,
					value.ToString()));
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetBool(string key, bool value)
		{
			if(m_Bool.ContainsKey(key))
				m_Bool[key] = value;
			else
				m_Bool.Add(key, value);
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetInt(string key, int value)
		{
			if(m_Int.ContainsKey(key))
				m_Int[key] = value;
			else
				m_Int.Add(key, value);
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetFloat(string key, float value)
		{
			if(m_Float.ContainsKey(key))
				m_Float[key] = value;
			else
				m_Float.Add(key, value);
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetString(string key, string value)
		{
			if(m_String.ContainsKey(key))
				m_String[key] = value;
			else
				m_String.Add(key, value);
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetColor(string key, Color value)
		{
			if(m_Color.ContainsKey(key))
				m_Color[key] = value;
			else
				m_Color.Add(key, value);
		}

		/**
		 * Set a value for key in the saved prefs.
		 */
		public void SetMaterial(string key, Material value)
		{
			if(m_Material.ContainsKey(key))
				m_Material[key] = value;
			else
				m_Material.Add(key, value);
		}

		/**
		 *	Get the internal <key, bool> dictionary.
		 */
		public Dictionary<string, bool> GetBoolDictionary() { return m_Bool; }

		/**
		 *	Get the internal <key, int> dictionary.
		 */
		public Dictionary<string, int> GetIntDictionary() { return m_Int; }

		/**
		 *	Get the internal <key, float> dictionary.
		 */
		public Dictionary<string, float> GetFloatDictionary() { return m_Float; }

		/**
		 *	Get the internal <key, string> dictionary.
		 */
		public Dictionary<string, string> GetStringDictionary() { return m_String; }

		/**
		 *	Get the internal <key, Color> dictionary.
		 */
		public Dictionary<string, Color> GetColorDictionary() { return m_Color; }

		/**
		 *	Get the internal <key, Material> dictionary.
		 */
		public Dictionary<string, Material> GetMaterialDictionary() { return m_Material; }

		/**
		 *	Clear all stored preference key value pairs.
		 */
		public void Clear()
		{
			m_Bool.Clear();
			m_Int.Clear();
			m_Float.Clear();
			m_String.Clear();
			m_Color.Clear();
		}
	}
}
