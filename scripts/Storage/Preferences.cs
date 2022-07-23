using Godot.Collections;
using Godot;

namespace Fifteen.Scripts.Storage
{
    public static class Preferences
    {
        public static PreferenceSection RootSection { get; private set; } = new PreferenceSection();
        public const string FilePath = "user://userdata.bin";

        public static Error LoadData()
        {
            File file = new File();
            var res = file.Open(FilePath, File.ModeFlags.Read);
            if (res == Error.Ok)
            {
                var parseResult = JSON.Parse(file.GetAsText()).Result;
                if (parseResult is Dictionary dict) RootSection = new PreferenceSection(dict);
            }

            file.Close();
            return res;
        }

        public static Error SaveData()
        {
            File file = new File();
            var res = file.Open(FilePath, File.ModeFlags.Write);
            
            if (res == Error.Ok) file.StoreString(RootSection.ToJsonString());
            
            file.Close();
            return res;
        }
        
    }
}