using UnityEngine;
using System.Collections;

// Skrip ini mengatur perilaku sampah yang mengapung di akuarium. Sampah akan bergerak secara acak, memantul saat bertabrakan dengan objek lain, dan memantul kembali saat menyentuh tepi layar. Saat pemain mengklik sampah, sampah akan "dibersihkan" (disembunyikan) dan kemudian muncul kembali setelah beberapa detik dengan posisi dan bentuk baru (Object Pooling).
public class TrashBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    private float speed;
    private Vector2 direction;
    private float floatOffset; 

    [Header("Respawn & Pool State")]
    public bool isHidden = false;
    public float respawnTime = 5.0f; 

    [Header("Cache")]
    private Bounds bounds;
    private SpriteRenderer sr;

    // Mengatur nilai kecepatan awal dan arah acak saat sampah pertama kali dibuat.
    public void Setup(string type)
    {
        sr = GetComponent<SpriteRenderer>();

        var config = ConfigManager.Instance.Config;
        speed = Random.Range(config.trashMinSpeed, config.trashMaxSpeed);
        
        // Menentukan arah gerak acak (X dominan, Y sedikit agar tidak terlalu miring)
        direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f)).normalized;
        
        var spawner = FindFirstObjectByType<AssetSpawner>();
        if (spawner != null && spawner.spawnArea != null)
        {
            bounds = spawner.spawnArea.bounds;
        }

        // Offset acak agar gerakan naik-turun (sinusoidal) antar sampah tidak serempak
        floatOffset = Random.Range(0f, 100f); 
    }

    // Update dipanggil setiap frame untuk mengatur gerakan sampah, deteksi tabrakan, dan penjaga batas layar.
    private void Update()
    {
        // Jika sedang "dibersihkan" (menunggu respawn), hentikan semua proses fisika
        if (isHidden || sr == null) return;

        // Kalkulasi Gerakan Natural (Floating)
        Vector3 move = direction * speed * Time.deltaTime;
        
        // Menambahkan efek gelombang (naik-turun halus) menggunakan fungsi Sinus
        move.y += Mathf.Sin(Time.time * 2f + floatOffset) * 0.003f;
        Vector2 nextPos = (Vector2)transform.position + (Vector2)move;

        // Deteksi Tabrakan (Anti-Overlap)
        float radius = sr.bounds.extents.x;
        Collider2D hit = Physics2D.OverlapCircle(nextPos, radius, LayerMask.GetMask("Spawnable"));

        if (hit == null || hit.gameObject == this.gameObject)
        {
            transform.position += move; // Jalan aman
        }
        else
        {
            // Memantul (berbalik arah) jika menabrak objek lain
            direction *= -1;
        }

        // Penjaga Batas Layar (Screen Bouncing)
        HandleScreenBounds();
    }

    // Fungsi untuk mendeteksi jika sampah menyentuh tepi layar dan memantulkannya kembali ke dalam area yang valid.
    private void HandleScreenBounds()
    {
        if (bounds.size != Vector3.zero)
        {
            Vector3 pos = transform.position;
            
            // Hitung radius agar ujung gambar sampah yang memantul, bukan titik tengahnya
            float halfWidth = sr.bounds.extents.x;
            float halfHeight = sr.bounds.extents.y;

            // Pantulan sumbu X (Kiri & Kanan)
            if (pos.x < bounds.min.x + halfWidth || pos.x > bounds.max.x - halfWidth) 
            { 
                direction.x *= -1; 
                pos.x = Mathf.Clamp(pos.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth); 
            }
            
            // Pantulan sumbu Y (Atas & Bawah)
            if (pos.y < bounds.min.y + halfHeight || pos.y > bounds.max.y - halfHeight) 
            { 
                direction.y *= -1; 
                pos.y = Mathf.Clamp(pos.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight); 
            }

            transform.position = pos;
        }
    }
    
    // Fungsi yang dipanggil saat pemain mengklik sampah. Sampah akan disembunyikan dan dijadwalkan untuk respawn dengan wujud dan posisi baru setelah beberapa detik.
    public void CleanTrash()
    {
        Destroy(gameObject);
        // if (!isHidden)
        // {
        //     StartCoroutine(RespawnRoutine());
        // }
    }

    // --- RESPWAN & POOLING ---
    private IEnumerator RespawnRoutine()
    {
        // Sembunyikan secara visual dan matikan collider agar tidak bisa diklik lagi
        isHidden = true;
        sr.enabled = false;
        GetComponent<BoxCollider2D>().enabled = false; 

        yield return new WaitForSeconds(respawnTime);

        // Dapatkan data sampah baru dari AssetSpawner untuk mengubah sprite dan nama objek sesuai jenis sampah yang muncul kembali
        if (AssetSpawner.Instance != null)
        {
            TrashAsset newTrashData = AssetSpawner.Instance.GetRandomTrashAsset();
            if (newTrashData != null)
            {
                sr.sprite = newTrashData.sprite;
                gameObject.name = "TRASH_" + newTrashData.typeName + "_Respawned";
                
                // Acak ulang skala agar bentuk barunya proporsional
                GameConfig config = ConfigManager.Instance.Config;
                float randomScale = Random.Range(config.trashMinScale, config.trashMaxScale);
                transform.localScale = new Vector3(randomScale, randomScale, 1);
            }

            // Cari lokasi baru yang aman dari tumpukan (Overlap)
            if (AssetSpawner.Instance.spawnArea != null)
            {
                float radius = sr.bounds.extents.x;
                transform.position = AssetSpawner.Instance.GetValidSpawnPosition(radius); 
            }
        }

        // Berikan rotasi acak agar terlihat natural
        float randomRotation = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0, 0, randomRotation);

        // Munculkan kembali
        isHidden = false;
        sr.enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;
    }
    
}