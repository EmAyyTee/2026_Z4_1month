using System;
using System.IO;
using UnityEngine;

namespace Game
{
    [Serializable] public sealed class CheckpointData
    {
        public string sceneName;
        public int coins;
        public int score;
    }

    public static class CheckpointSave
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "checkpoint.json");

        public static bool TrySave(string sceneName, int coins, int score)
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Nie można załadować sceny: {sceneName}");
                return false;
            }

            var data = new CheckpointData
            {
                sceneName = sceneName,
                coins = coins,
                score = score
            };

            try
            {
                string tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(SavePath))
                    File.Replace(tempPath, SavePath, null);
                else
                    File.Move(tempPath, SavePath);

                Debug.Log($"Zapisano checkpoint: {SavePath}");
                return true;
            }
            catch (Exception error) when (
                error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogError($"Nie udało się zapisać checkpointu: {error.Message}");
                return false;
            }
        }

        public static bool TryLoad(out CheckpointData data)
            {
                data = null;

                try
                {
                    if (!File.Exists(SavePath))
                        return false;

                    var saved = JsonUtility.FromJson<CheckpointData>(
                        File.ReadAllText(SavePath));

                    if (saved == null || string.IsNullOrWhiteSpace(saved.sceneName) ||
                        !Application.CanStreamedLevelBeLoaded(saved.sceneName) ||
                        saved.coins < 0 || saved.score < 0)
                        return false;

                    data = saved;
                    return true;
                }
                catch (Exception error) when (
                    error is IOException || error is UnauthorizedAccessException ||
                    error is ArgumentException)
                {
                    Debug.LogWarning($"Nie udało się odczytać checkpointu: {error.Message}");
                    return false;
                }
            }
    }
}