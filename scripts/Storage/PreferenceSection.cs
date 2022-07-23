using Godot;
using Godot.Collections;

namespace Fifteen.Scripts.Storage
{
    public class PreferenceSection
    {
        private Dictionary _prefs;
        public PreferenceSection()
        {
            _prefs = new Dictionary();
        }
        
        public PreferenceSection(Dictionary dictionary)
        {
            _prefs = dictionary;
        }
        
        public string GetString(string key, string defaultValue)
        {
            return !_prefs.Contains(key) ? defaultValue : _prefs[key].ToString();
        }
        public float GetFloat(string key, float defaultValue, float max = float.MaxValue, float min = float.MinValue) =>
            _prefs.Contains(key) && _prefs[key] is float value && (value <= max || value >= min) ? value : defaultValue;

        public int GetInt32(string key, int defaultValue, int max = int.MaxValue, int min = int.MinValue)
        {
            return (int)GetFloat(key, defaultValue, max, min);
        }

        public void SetString(string key, string value)
        {
            if (!_prefs.Contains(key)) _prefs.Add(key, value);
            else _prefs[key] = value;
        }

        public bool GetBool(string key, bool defaultValue) 
        {
            return _prefs.Contains(key) && bool.TryParse(_prefs[key].ToString(), out bool value) ? value : defaultValue;
        }

        public void SetBool(string key, bool value) 
        {
            if (!_prefs.Contains(key)) _prefs.Add(key, value);
            else _prefs[key] = value;
        }
        
        public void SetFloat(string key, float value)
        {
            if (!_prefs.Contains(key)) _prefs.Add(key, value);
            else _prefs[key] = value;
        }

        public PreferenceSection GetSection(string key, PreferenceSection defaultValue)
        {
            return _prefs.Contains(key) && _prefs[key] is Dictionary value ? new PreferenceSection(value) : defaultValue;
        }

        public PreferenceSection CreateSection(string key)
        {
            if (!_prefs.Contains(key)) _prefs.Add(key, new Dictionary());
            else _prefs[key] = new Dictionary();

            return new PreferenceSection((Dictionary) _prefs[key]);
        }

        public bool IsSection(string key)
        {
            return _prefs.Contains(key) && _prefs[key] is Dictionary;
        }
    
        public void RemoveKey(string key)
        {
            if (_prefs.Contains(key)) _prefs.Remove(key);
        }

        public string ToJsonString()
        {
            return JSON.Print(_prefs);
        }
    }
}