using Godot;
using Godot.Collections;

using Path = System.IO.Path;

namespace Fifteen.Storage
{
    public class PreferenceManager
    {
        public readonly string FilePath;
        Dictionary _prefs;
        public PreferenceManager(string path = null)
        {
            if (path is null) path = Path.Combine(OS.GetUserDataDir(), "prefs.json");
            FilePath = path;
            LoadData();
        }

        public void LoadData()
        {
            File file = new File();
            Error error = file.Open(FilePath, File.ModeFlags.Read);

            if (error is Error.Ok)
            {
                string jsonString = file.GetAsText();
                JSONParseResult parseResult = JSON.Parse(jsonString);
                if (parseResult.Result is Dictionary dictionary)_prefs = dictionary;
                else _prefs = new Dictionary();
            }
            else _prefs = new Dictionary();

            file.Close();
        }

        public void SaveData()
        {
            File file = new File();
            Error error = file.Open(FilePath, File.ModeFlags.Write);
            if (error is Error.Ok)
                file.StoreString(JSON.Print(_prefs));
            file.Close();
        }

        public void SetString(string key, string value)
        {
            _prefs[key] = value;
        }

        public float GetFloat(string key, float defaultValue, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            return _prefs.Contains(key) && _prefs[key] is float value && value <= maxValue && value >= minValue ? value : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            _prefs[key] = value;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            return _prefs.Contains(key) && _prefs[key] is bool value ? value : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            _prefs[key] = value;
        }

        public Array GetArray(string key, Array defaultValue)
        {
            return _prefs.Contains(key) && _prefs[key] is Array value ? value : defaultValue;
        }

        public void Set2DIntArray(string key, int[,] array)
        {
            Array<Array<float>> jsonArray = new Array<Array<float>>();

            for (int i = 0; i < array.GetLength(0); i++)
            {
                jsonArray.Add(new Array<float>());
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    jsonArray[i].Add((float) array[i, j]);
                }
            }

            _prefs[key] = jsonArray;
        }

        public T GetUnsafe<T>(string key)
        {
            return (T) _prefs[key];
        }

        public int[,] Get2DIntArray(string key, int[,] defaultValue)
        {
            if (!_prefs.Contains(key) || !(_prefs[key] is Array)) return defaultValue;

            Array array = (Array) _prefs[key];
            if (array.Count < 1 || !(array[0] is Array)) return defaultValue;

            int[,] outArray = new int[array.Count, (array[0] as Array).Count];

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is Array floatArray && floatArray.Count == outArray.GetLength(1))
                {
                    for (int j = 0; j < floatArray.Count; j++)
                    {
                        if (floatArray[j] is float)
                        {
                            outArray[i, j] = System.Convert.ToInt32(floatArray[j]);
                        }
                        else  return defaultValue;
                        
                    }
                } 
                else return defaultValue;
            }

            return outArray;
        }

        public void RemoveKey(string key)
        {
            _prefs.Remove(key);
        }

        public int GetInt32(string key, float defaultValue, int minValue = int.MinValue, float maxValue = int.MaxValue)
        {
            return (int) GetFloat(key, defaultValue, minValue, maxValue);
        }

        public void SetInt32(string key, int value)
        {
            _prefs[key] = (float) value;
        }

        public string GetString(string key, string defaultValue)
        {
            return _prefs.Contains(key) ? _prefs[key].ToString() : defaultValue;
        }

        public string ToJsonString()
        {
            return JSON.Print(_prefs);
        }
    }
}