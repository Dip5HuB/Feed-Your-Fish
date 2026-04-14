using UnityEngine;
using TMPro;

// Script Untuk mengelola skor pemain, yaitu jumlah sampah yang berhasil dibersihkan. 
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; 

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText; 

    private int currentScore = 0;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Pastikan teks direset ke 0 saat game pertama kali dijalankan
        UpdateScoreUI();
    }
    
    // --- FUNGSI UNTUK MENAMBAH SKOR ---
    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        Debug.Log($"Skor bertambah! Skor saat ini: {currentScore}");
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
        Debug.Log("Skor direset ke 0.");
    }
    
    // Fungsi untuk memperbarui teks skor di UI setiap kali skor berubah.
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

}