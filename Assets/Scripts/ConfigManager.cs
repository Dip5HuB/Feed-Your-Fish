using UnityEngine;
using System.IO;


// Struktur data untuk menyimpan semua parameter yang bisa diatur melalui config.json
[System.Serializable]
public class GameConfig
{
    public float fishMinSpeed;
    public float fishMaxSpeed;
    public float fishMinScale;
    public float fishMaxScale;
    public float detectionRadius;
    public float hungerCooldown;
    public float trashMinSpeed;
    public float trashMaxSpeed;
    public float trashMinScale;
    public float trashMaxScale;
    public float trashSpawnIntervalMin; 
    public float trashSpawnIntervalMax;
    public int maxTrashAmount; 
}

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance;

    public GameConfig Config { get; private set; }

    // Path ke root folder project
    private string ConfigPath => Path.Combine(
        Path.GetDirectoryName(Application.dataPath), 
        "config.json"
    );

    void Awake()
    {
        // Singleton pattern untuk memastikan hanya ada satu instance ConfigManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfig();
    }

    // Fungsi untuk memuat config dari file JSON
    private void LoadConfig()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            Config = JsonUtility.FromJson<GameConfig>(json);
            Debug.Log("<color=green>Config berhasil dimuat dari: " + ConfigPath + "</color>");
        }
        else
        {
            // Jika tidak ada, buat config default
            Config = new GameConfig();
            SaveConfig();
            Debug.LogWarning("config.json tidak ditemukan, membuat file default di: " + ConfigPath);
        }
    }

    // Opsional: Simpan config (berguna saat pertama kali generate)
    public void SaveConfig()
    {
        string json = JsonUtility.ToJson(Config, true); // true = pretty print
        File.WriteAllText(ConfigPath, json);
    }
}