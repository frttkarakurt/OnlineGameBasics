using System.IO;
using UnityEngine;
using Mirror;
using System;

[Serializable]
public class GamePlayerStatss
{
    public string name;
    public int kills;
    public int deaths;
    public int assists;
}
public class PlayerDataDemo : NetworkBehaviour
{
    private GamePlayerStatss stats;
    private string savePath;

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "playerStats.json");

        if (!isLocalPlayer) return; // sadece local player yükler

        LoadOrCreateStats();
    }

    private void LoadOrCreateStats()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            try
            {
                stats = JsonUtility.FromJson<GamePlayerStatss>(json);
            }
            catch
            {
                CreateDefaultStats();
            }
        }
        else
        {
            CreateDefaultStats();
        }
    }

    private void CreateDefaultStats()
    {
        stats = new GamePlayerStatss()
        {
            name = "player00001",
            kills = 0,
            deaths = 0,
            assists = 0
        };
        SaveStats();
    }

    public void SaveStats()
    {
        if (stats == null) return;
        string json = JsonUtility.ToJson(stats, true);
        File.WriteAllText(savePath, json);
    }

    public string Name()
    {
        LoadOrCreateStats();
        return stats != null ? stats.name : "Unknown";
    }

    public int Kills()
    {
        return stats != null ? stats.kills : 0;
    }

    public int Deaths()
    {
        return stats != null ? stats.deaths : 0;
    }

    public int Assists()
    {
        return stats != null ? stats.assists : 0;
    }
}
