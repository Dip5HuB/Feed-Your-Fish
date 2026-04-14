using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameUIPanel; 

    [Header("Core Systems to Activate")]
    public AssetSpawner assetSpawner;
    public AquascapePlayerController playerController;
    public FoodManager foodManager;

    void Start()
    {
        ShowMainMenu();
    }

    // --- FUNGSI UNTUK TOMBOL PLAY ---
    public void StartGame()
    {
        // Sembunyikan Menu Utama
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Tampilkan UI Game
        if (gameUIPanel != null) gameUIPanel.SetActive(true);

        // Nyalakan semua Core System agar game mulai berjalan
        if (assetSpawner != null) assetSpawner.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (foodManager != null) foodManager.enabled = true;

        Debug.Log("<color=green>Game Started! Core systems activated.</color>");
    }

    // --- FUNGSI UNTUK TOMBOL EXIT ---
    public void ExitGame()
    {
        Debug.Log("<color=red>Game Exiting...</color>");

        Application.Quit();
    }

    public void ShowMainMenu()
    {
        // Kembalikan ke state awal (Menu nyala, Game mati)
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        if (assetSpawner != null) assetSpawner.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (foodManager != null) foodManager.enabled = false;
    }
}