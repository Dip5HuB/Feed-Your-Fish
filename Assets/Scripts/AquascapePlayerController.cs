using UnityEngine;
using UnityEngine.InputSystem;

// Controller utama untuk menangani input pemain (klik mouse) dan menentukan aksi yang sesuai (memberi makan, membersihkan sampah, mengejutkan ikan)
public class AquascapePlayerController : MonoBehaviour
{
    private void Update()
    {
        HandleMouseInput();
    }
    

    // --- INPUT HANDLING ---
    private void HandleMouseInput()
    {
        // Pastikan mouse terhubung dan klik kiri baru saja ditekan pada frame ini
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Konversi posisi mouse di layar menjadi koordinat dunia (World Space) 2D
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(screenPosition);
            
            // Menggunakan OverlapPoint karena lebih presisi dan efisien untuk klik 2D
            // Hanya mendeteksi objek yang berada di layer "Spawnable" (mengabaikan background/batas layar)
            Collider2D hit = Physics2D.OverlapPoint(mousePos, LayerMask.GetMask("Spawnable"));

            if (hit != null)
            {
                ProcessObjectInteraction(hit, mousePos);
            }
            else
            {
                // Jika klik meleset (mengenai ruang air kosong), jatuhkan makanan
                SpawnFood(mousePos);
            }
        }
    }

    // --- INTERACTION LOGIC ---
    private void ProcessObjectInteraction(Collider2D hit, Vector2 mousePos)
    {
        if (hit.CompareTag("Trash"))
        {
            
            TrashBehavior trash = hit.GetComponent<TrashBehavior>();
            if (trash != null)
            {
                // Pemain membersihkan sampah
                ScoreManager.Instance.AddScore(1); // Tambahkan skor saat sampah dibersihkan
                Destroy(hit.gameObject);
            }
        }
        else if (hit.CompareTag("Fish"))
        {
            // Pemain mengejutkan ikan
            FishBehavior fish = hit.GetComponent<FishBehavior>();
            if (fish != null) fish.TriggerFlee();
        }
        else
        {
            // Fallback jika objek di layer Spawnable tidak memiliki tag yang sesuai
            SpawnFood(mousePos);
        }
    }

    // --- FOOD SPAWNING ---
    private void SpawnFood(Vector2 pos)
    {
        // Cegah error jika file gambar makanan belum selesai dimuat oleh AssetSpawner
        if (FoodManager.Instance == null || FoodManager.Instance.currentFoodSprite == null) return;
        
        // Ambil makanan dari gudang (Pool) alih-alih melakukan Instantiate/Destroy berulang kali
        GameObject food = FoodManager.Instance.GetPooledFood();
        
        if (food != null)
        {
            food.transform.position = pos; 
            
            // Panggil fungsi Reset agar fisika jatuhnya diulang dari awal
            FoodBehavior foodBehavior = food.GetComponent<FoodBehavior>();
            if (foodBehavior != null) foodBehavior.ResetFood(); 
            
            food.SetActive(true); 
        }
        else
        {
            Debug.LogWarning("Batas maksimal makanan di akuarium tercapai!");
        }
    }
    
}