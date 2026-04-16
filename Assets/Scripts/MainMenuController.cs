using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameUIPanel; 

    [Header("UI Controls")]
    public Slider bgmSlider;

    [Header("Core Systems to Activate")]
    public AssetSpawner assetSpawner;
    public AquascapePlayerController playerController;
    public FoodManager foodManager;

    void Start()
    {
        ShowMainMenu();

        if (bgmSlider != null)
        {
            // Ambil data dari PlayerPrefs (sama seperti di AudioManager)
            bgmSlider.value = PlayerPrefs.GetFloat("BGM_Volume", 1.0f);
        }
    }

    // --- FUNGSI UNTUK TOMBOL PLAY ---
    public void StartGame()
    {
        // Sembunyikan Menu Utama
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Tampilkan UI Game
        if (gameUIPanel != null) gameUIPanel.SetActive(true);

        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();

        // Nyalakan semua Core System agar game mulai berjalan
        if (assetSpawner != null) 
        {
            assetSpawner.enabled = true;
            assetSpawner.RestartSpawning();
        }
        
        if (playerController != null) playerController.enabled = true;
        if (foodManager != null) foodManager.enabled = true;

        Debug.Log("<color=green>Game Mulai!.</color>");
    }

    // --- FUNGSI UNTUK TOMBOL EXIT ---
    public void ExitGame()
    {
        Debug.Log("<color=red>Game Exiting...</color>");

        Application.Quit();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        // Matikan sistem input dan food manager
        if (playerController != null) playerController.enabled = false;
        if (foodManager != null) foodManager.enabled = false;

        // --- Bersihkan akuarium lalu matikan Spawner ---
        if (assetSpawner != null) 
        {
            assetSpawner.ClearAquarium(); 
            assetSpawner.enabled = false;
        }
    }

}