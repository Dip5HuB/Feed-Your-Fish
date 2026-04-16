
using UnityEngine;

// Script untuk mengelola audio dalam game, termasuk musik latar dan efek suara.
public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Komponen AudioSource untuk musik latar")]
    public AudioSource bgmSource;
    
    [Tooltip("Komponen AudioSource untuk efek suara singkat")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    [Tooltip("Suara saat menekan tombol UI (Play, Exit, dll)")]
    public AudioClip buttonClickSFX;
    
    [Tooltip("Suara saat interaksi di dalam air (Klik sampah, ikan, atau sebar makanan)")]
    public AudioClip interactionSFX; 


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (bgmSource != null)
        {
            bgmSource.volume = PlayerPrefs.GetFloat("BGM_Volume", 1.0f);
        }
    }

    // --- FUNGSI UNTUK MENGATUR VOLUME BGM DAN SFX ---
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
            PlayerPrefs.SetFloat("BGM_Volume", volume);
            PlayerPrefs.Save();
        }
    }

    // Fungsi dinamis (Dynamic Float) untuk dihubungkan ke UI Slider.
    public void PlayButtonSFX()
    {
        if (sfxSource != null && buttonClickSFX != null)
        {
            sfxSource.PlayOneShot(buttonClickSFX);
        }
    }

   // Fungsi untuk memainkan SFX interaksi di dalam air (klik sampah, ikan, atau sebar makanan).
    public void PlayInteractionSFX()
    {
        if (sfxSource != null && interactionSFX != null)
        {
            sfxSource.PlayOneShot(interactionSFX);
        }
    }
}