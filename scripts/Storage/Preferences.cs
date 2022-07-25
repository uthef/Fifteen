using Godot.Collections;
using Godot;

namespace Fifteen.Scripts.Storage
{
    public class Preferences
    {
        public PreferenceSection RootSection { get; private set; } = new PreferenceSection();
        public readonly string FilePath;
        public readonly bool Encrypted;

        public Preferences(out Error error, bool encrypted = false, string filePath = "prefs.json")
        {
            Encrypted = encrypted;
            FilePath = "user://" + filePath;
            error = Load();
        }

        public Error Load() 
        {
            File file = new File();
            var res = Encrypted ? file.OpenEncryptedWithPass(FilePath, File.ModeFlags.Read, OS.GetUniqueId()) : file.Open(FilePath, File.ModeFlags.Read);
            if (res == Error.Ok)
            {
                var parseResult = JSON.Parse(file.GetAsText()).Result;
                if (parseResult is Dictionary dict) RootSection = new PreferenceSection(dict);
            }
            file.Close();
            return res;
        }

        public Error Save()
        {
            File file = new File();
            var res = Encrypted ? file.OpenEncryptedWithPass(FilePath, File.ModeFlags.Write, OS.GetUniqueId()) : file.Open(FilePath, File.ModeFlags.Write);
            
            if (res == Error.Ok) file.StoreString(RootSection.ToJsonString());
            
            file.Close();
            return res;
        }
        
    }
}