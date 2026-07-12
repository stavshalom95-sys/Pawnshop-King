using System;
using System.IO;
using PawnshopKing.Data.Runtime;
using UnityEngine;

namespace PawnshopKing.Systems.SaveLoad
{
    /// <summary>
    /// Versioned envelope around the persisted GameState, so old files can be
    /// migrated (or refused) instead of half-deserializing into a new schema.
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        public int version;
        public GameState state;
    }

    /// <summary>
    /// Persists the campaign as JSON in Application.persistentDataPath (GDD 35).
    /// One slot. The autosave is written when a day completes, so a loaded state
    /// always means "day N is done" — the caller resumes at the morning of N+1.
    /// Writes go through a temp file and an atomic replace, so a crash mid-write
    /// can never corrupt the only save.
    /// </summary>
    public static class SaveLoadSystem
    {
        public const int CurrentVersion = 1;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "pawnshop_king_save.json");

        public static bool HasSave() => File.Exists(SavePath);

        public static bool Save(GameState state)
        {
            try
            {
                string json = JsonUtility.ToJson(new SaveFile { version = CurrentVersion, state = state });
                string tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(SavePath)) File.Replace(tempPath, SavePath, null);
                else File.Move(tempPath, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to write save: {e.Message}");
                return false;
            }
        }

        /// <summary>The saved campaign, or null when there is no readable, compatible save.</summary>
        public static GameState Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;

                var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
                if (file?.state == null) return null;

                // Files from a newer build than this one are refused, not guessed at.
                if (file.version > CurrentVersion) return null;

                // version < CurrentVersion: migration steps slot in here as the schema grows.
                return file.state;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to read save: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to delete save: {e.Message}");
            }
        }
    }
}
