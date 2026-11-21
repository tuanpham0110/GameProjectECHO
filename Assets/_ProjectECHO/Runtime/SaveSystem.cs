using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectEcho
{
    [Serializable]
    public sealed class LevelRecord
    {
        public string id;
        public float bestRemaining;
    }

    [Serializable]
    public sealed class SaveProfile
    {
        public int version = 1;
        public List<LevelRecord> completed = new();
        public List<Skill> unlocked = new();
        public Skill lastSkill;
    }

    public sealed class SaveSystem
    {
        private readonly string path;
        public SaveSystem(string directory) => path = Path.Combine(directory, "echo-profile.json");

        public SaveProfile Load() => Read(path) ?? Read(path + ".bak") ?? new SaveProfile();

        public bool Save(SaveProfile profile)
        {
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
                if (File.Exists(path))
                {
                    if (Read(path) != null) File.Replace(temporary, path, path + ".bak");
                    else File.Replace(temporary, path, null);
                }
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Project ECHO: Không ghi được hồ sơ. {exception.Message}");
                return false;
            }
        }

        private static SaveProfile Read(string file)
        {
            if (!File.Exists(file)) return null;
            try
            {
                var profile = JsonUtility.FromJson<SaveProfile>(File.ReadAllText(file));
                if (profile == null || profile.version != 1) return null;
                profile.completed ??= new List<LevelRecord>();
                profile.unlocked ??= new List<Skill>();
                return profile;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }
    }
}
