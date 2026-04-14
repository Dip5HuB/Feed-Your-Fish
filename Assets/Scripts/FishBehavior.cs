using UnityEngine;
using System.Collections;

// Script utama untuk mengatur perilaku ikan: pergerakan, pencarian makanan, penghindaran tabrakan, dan reaksi terhadap ancaman (seperti sampah).
public class FishBehavior : MonoBehaviour
{
    [Header("Movement & AI Config")]
    private float minSpeed, maxSpeed, currentSpeed;
    private float detectionRadius, hungerCooldown;
    private float hungerMeter = 100f;
    private Vector2 targetPosition;

    [Header("State Flags")]
    private bool isFleeing = false;
    private bool isEating = false;
    private bool isCollisionAvoidanceActive = false;
    
    [Header("Avoidance Parameters")]
    private const float collisionCooldownDuration = 0.75f; // Durasi jeda menghindar (detik)
    private const float evasionPointOffset = 2.0f; // Seberapa jauh ikan berenang menyimpang saat menghindar
    
    [Header("Components & Cache")]
    private SpriteRenderer sr; 
    private Bounds bounds; 
    private string fishtype; // Menyimpan jenis ikan untuk specific behavior (misal: STARFISH)
    
    [Header("UI Elements")]
    private GameObject barObj;
    private SpriteRenderer barSr;

    // Fungsi Setup untuk menginisialisasi parameter ikan berdasarkan jenisnya dan konfigurasi global
    public void Setup(string type)
    {
        fishtype = type.ToUpper();
        sr = GetComponent<SpriteRenderer>(); 
        
        // Pastikan ikan memiliki Rigidbody2D untuk deteksi tabrakan, tapi tetap kinematic karena kita yang mengatur pergerakannya secara manual
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; 
        
        // Ambil parameter dari ConfigManager untuk jenis ikan ini
        var config = ConfigManager.Instance.Config;
        minSpeed = config.fishMinSpeed;
        maxSpeed = config.fishMaxSpeed;
        detectionRadius = config.detectionRadius;
        hungerCooldown = config.hungerCooldown;
        
        currentSpeed = Random.Range(minSpeed, maxSpeed);

        // Cache area batas akuarium untuk logika pergerakan dan penghindaran tabrakan
        var spawner = FindFirstObjectByType<AssetSpawner>();
        if (spawner != null && spawner.spawnArea != null)
        {
            bounds = spawner.spawnArea.bounds;
        }
        
        CreateHungerBar();
        SetNewRandomTarget();
        StartCoroutine(HungerDegradation());
    }

    void Update()
    {
        UpdateBarPosition();

        if (isFleeing) return; // Prioritaskan kabur dari pemain di atas segalanya

        // Jika ikan sangat lapar, cari makanan. Jika tidak ada makanan dalam radius deteksi, tetap berkeliaran secara acak.
        if (hungerMeter <= 0 && !isEating) FindFood();
        else Wander();
        
        // Balik sprite berdasarkan arah pergerakan horizontal
        if (Mathf.Abs(targetPosition.x - transform.position.x) > 0.1f)
        {
            sr.flipX = targetPosition.x < transform.position.x; 
        }   
        UpdateHungerBarVisual();   
        ClampToScreen(); // Pastikan ikan tidak keluar dari batas akuarium
    }

    // Berenang santai ke arah titik acak. Menggunakan radar OverlapCircle untuk menghindari tabrakan dengan ikan/sampah lain sebelum melangkah.
    void Wander()
    {

        if (isCollisionAvoidanceActive) return;

        Vector2 nextPos = Vector2.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        float radius = sr.bounds.extents.x;

        Collider2D hit = Physics2D.OverlapCircle(nextPos, radius, LayerMask.GetMask("Spawnable"));

        // Collider2D hit = Physics2D.OverlapCircle(nextPos, radius, LayerMask.GetMask(""));

        if (hit == null || hit.gameObject == this.gameObject)
        {
            transform.position = nextPos;
        }
        else
        {
            // Jalan terhalang, masuk ke mode menghindar
            //SetNewRandomTarget();
            StartCoroutine(CollisionAvoidanceRoutine(hit.transform.position));
        }

        if (Vector2.Distance(transform.position, targetPosition) < 0.2f) SetNewRandomTarget();
    }

    // Mengatur target baru secara acak di dalam batas akuarium. Untuk STARFISH, batasi area vertikalnya ke bagian bawah akuarium.
    void SetNewRandomTarget()
    {
        if (bounds.size != Vector3.zero && sr != null)
        {
            float halfWidth = sr.bounds.extents.x;
            float halfHeight = sr.bounds.extents.y;

            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            // Jika jenis ikan adalah STARFISH, batasi area vertikalnya ke bagian bawah akuarium (misal: 25% dari tinggi total)
            if (fishtype == "STARFISH")
            {
                float tankHeight = bounds.max.y - bounds.min.y;
                maxY = bounds.min.y + (tankHeight * 0.25f) - halfHeight;

                if (maxY < minY) maxY = minY;
            }
            
            // Pilih posisi target acak di dalam batas yang sudah dihitung
            targetPosition = new Vector2(
                Random.Range(bounds.min.x + halfWidth, bounds.max.x - halfWidth), 
                Random.Range(minY, maxY) 
            );
        }
    }

    // Coroutine untuk menghindari tabrakan dengan objek lain. Ikan akan berenang menyimpang menjauh dari titik tabrakan selama 0.5 detik, lalu diam sejenak untuk membiarkan ikan lain lewat, sebelum akhirnya mencari target baru.
    IEnumerator CollisionAvoidanceRoutine(Vector2 obstaclePos)
    {
        isCollisionAvoidanceActive = true;

        // Ikan memutar sedikit menjauh dari titik tengah rintangan.
        Vector2 directionAwayFromObstacle = ((Vector2)transform.position - obstaclePos).normalized;
        Vector2 temporarySafeTarget = (Vector2)transform.position + directionAwayFromObstacle * evasionPointOffset;

        float timer = collisionCooldownDuration; // Waktu jeda total 
        float activeAvoidanceTime = 0.5f; // Ikan akan *aktif berenang menyimpang* selama 0.5s awal

        // Berenang menyimpang menjauh dari tabrakan
        while (activeAvoidanceTime > 0)
        {
            // Ikan dipaksa berenang ke titik temporarySafeTarget tanpa menanyakan overlap lagi
            transform.position = Vector2.MoveTowards(transform.position, temporarySafeTarget, currentSpeed * Time.deltaTime);
            activeAvoidanceTime -= Time.deltaTime;
            timer -= Time.deltaTime;

            ClampToScreen();
            yield return null; 
        }

        //  Ikan berhenti di tempat untuk sisa cooldown 
        yield return new WaitForSeconds(timer);

        // Mode jeda selesai. Ikan diperbolehkan mencari target normal kembali.
        isCollisionAvoidanceActive = false;
        SetNewRandomTarget();
    } 

    // Fungsi untuk mencari makanan terdekat dalam radius deteksi. Jika ditemukan, ikan akan berenang ke arah makanan tersebut. Jika tidak ada makanan, ikan tetap berkeliaran secara acak.
    void FindFood()
    {
        GameObject closestFood = null;
        float minDist = detectionRadius;
        
        // Cari semua makanan yang ada di scene dan temukan yang paling dekat dalam radius deteksi
        foreach (GameObject food in GameObject.FindGameObjectsWithTag("Food"))
        {
            float dist = Vector2.Distance(transform.position, food.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestFood = food;
            }
        }

        // Jika ada makanan yang ditemukan dalam radius, atur targetPosition ke posisi makanan tersebut untuk mulai mengejar. Jika tidak, tetap berkeliaran secara acak.
        if (closestFood != null)
        {
            targetPosition = closestFood.transform.position; 
            transform.position = Vector2.MoveTowards(transform.position, closestFood.transform.position, maxSpeed * Time.deltaTime);
        }
        else
        {
            Wander(); // Tidak ada makanan dalam radius, tetap berkeliaran secara acak
        }
    }

    // Fungsi untuk mendeteksi tabrakan dengan makanan. Jika ikan menyentuh makanan dan sedang tidak dalam proses makan, maka makanan tersebut akan "dimakan" (disembunyikan kembali ke Pool) dan ikan akan masuk ke state makan untuk sementara waktu.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food") && hungerMeter <= 0)
        {
            other.gameObject.SetActive(false); // Kembalikan makanan ke Pool (tidak di-Destroy)
            StartCoroutine(EatingRoutine());
        }
    }

    // Coroutine untuk menangani proses makan. Ikan akan mengisi ulang hungerMeter ke 100 dan menampilkan bar, lalu menunggu selama durasi cooldown sebelum bisa makan lagi.
    IEnumerator EatingRoutine()
    {
        isEating = true;
        hungerMeter = 100f; 
        UpdateHungerBarVisual(); 
        
        yield return new WaitForSeconds(hungerCooldown); 
        isEating = false; 
    }

    // Coroutine untuk mengurangi hungerMeter secara berkala saat ikan tidak makan. Jika hungerMeter mencapai 0, ikan akan mulai mencari makanan.
    IEnumerator HungerDegradation()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!isEating) 
            {
                hungerMeter = Mathf.Max(0, hungerMeter - 10f);
                UpdateHungerBarVisual();
            }
        }
    }

    // Fungsi untuk memicu ikan kabur. Ikan akan berenang dengan kecepatan lebih tinggi menjauh dari posisi pemain selama 1.5 detik, lalu kembali ke perilaku normal.
    public void TriggerFlee()
    {
        if (!isFleeing) StartCoroutine(FleeRoutine());
    }

    // Coroutine untuk menangani proses kabur. Ikan akan berenang dengan kecepatan lebih tinggi menjauh dari posisi pemain selama 1.5 detik, lalu kembali ke perilaku normal.
    IEnumerator FleeRoutine()
    {
        isFleeing = true;
        SetNewRandomTarget(); 
        
        // Kecepatan kabur yang lebih tinggi dari kecepatan normal
        float boostSpeed = maxSpeed * 2.5f;
        float timer = 0;
        
        while (timer < 1.5f)
        {
            if (targetPosition.x < transform.position.x) sr.flipX = true; 
            else if (targetPosition.x > transform.position.x) sr.flipX = false;
            
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, boostSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            
            ClampToScreen();
            
            if (Vector2.Distance(transform.position, targetPosition) < 0.2f) SetNewRandomTarget();
            
            yield return null;
        }
        isFleeing = false;
        SetNewRandomTarget();
    }

    // Fungsi untuk membuat bar hunger di atas kepala ikan. Bar ini akan berubah warna dan ukuran berdasarkan level hungerMeter.
    void CreateHungerBar()
    {
        barObj = new GameObject("HungerBar_" + gameObject.name);
        barSr = barObj.AddComponent<SpriteRenderer>();
        
        Texture2D rectTex = new Texture2D(1, 1);
        rectTex.SetPixel(0, 0, Color.white);
        rectTex.Apply();
        barSr.sprite = Sprite.Create(rectTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        
        barSr.sortingOrder = 20; 
        UpdateHungerBarVisual();
    }

    // Fungsi untuk memperbarui tampilan bar hunger berdasarkan nilai hungerMeter. Bar akan berubah ukuran dan warna (hijau >60%, kuning 30-60%, merah <30%).
    void UpdateHungerBarVisual()
    {
        if (barObj == null || sr == null) return;

        float percent = hungerMeter / 100f;
        float uniformWidth = 150f;
        float minWidth = 10f;

        float currentWidth = Mathf.Max(uniformWidth * percent, minWidth);
        barObj.transform.localScale = new Vector3(currentWidth, 25f, 1f);

        if (percent > 0.6f) barSr.color = Color.green;
        else if (percent > 0.3f) barSr.color = Color.yellow;
        else barSr.color = Color.red;

        barSr.enabled = true;
    }

    // Fungsi untuk memperbarui posisi bar hunger agar selalu berada di atas kepala ikan, dengan sedikit padding.
    void UpdateBarPosition()
    {
        if (barObj == null || sr == null) return;

        float topOfFishY = sr.bounds.max.y;
        float padding = 0.25f;
        barObj.transform.position = new Vector3(
            transform.position.x,
            topOfFishY + padding,
            transform.position.z
        );
    }


    // Fungsi untuk membatasi pergerakan ikan di dalam layar. Ikan tidak akan bisa keluar dari batas yang sudah ditentukan oleh spawnArea di AssetSpawner. Untuk STARFISH, area vertikalnya dibatasi ke bagian bawah akuarium.
    void ClampToScreen()
    {
        if (bounds.size != Vector3.zero && sr != null)
        {
            // Ambil setengah ukuran lebar dan tinggi tubuh ikan
            float halfWidth = sr.bounds.extents.x;
            float halfHeight = sr.bounds.extents.y;

            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            if (fishtype == "STARFISH")
            {
                float tankHeight = bounds.max.y - bounds.min.y;
                maxY = bounds.min.y + (tankHeight * 0.25f) - halfHeight;

                if (maxY < minY) maxY = minY;
            }

            Vector3 clampedPos = transform.position;
            

            // Clamp posisi ikan agar tidak keluar dari batas spawnArea, dengan memperhitungkan ukuran tubuh ikan agar tidak setengah keluar layar
            clampedPos.x = Mathf.Clamp(clampedPos.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
            clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY); 
            
            transform.position = clampedPos;
        }
    }

    void OnDestroy()
    {
        if (barObj != null)
        {
            Destroy(barObj);
        }
    }
}