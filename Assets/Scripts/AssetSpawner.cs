using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

// Struktur data untuk menyimpan informasi sampah yang di-spawn
[System.Serializable]
public class TrashAsset 
{
    public Sprite sprite;
    public string typeName;
}

public class AssetSpawner : MonoBehaviour
{
    public static AssetSpawner Instance;  // Singleton untuk akses global
    public BoxCollider2D spawnArea; // Area di mana objek akan di-spawn
    public LayerMask objectLayer; // Layer untuk deteksi tabrakan saat spawn
    
    public List<TrashAsset> availableTrashAssets = new List<TrashAsset>(); // List untuk menyimpan aset sampah yang tersedia untuk di-spawn
    
    private int activeTrashInstances = 0; 
    private bool isSpawningTrashPool = false; 
    private string folderPath;

    // FileSystemWatcher untuk memantau folder secara real-time
    private FileSystemWatcher watcher;
    private ConcurrentQueue<string> newFilesQueue = new ConcurrentQueue<string>();

    void Awake()
    {
        Instance = this;

        // Tentukan path ke folder "AquascapeAssets" di dalam folder Assets
        folderPath = Path.Combine(Application.dataPath, "AquascapeAssets");
        folderPath = Path.GetFullPath(folderPath); 

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
    }

    void Update()
    {
        // Dequeue memindahkan data dari thread FileWatcher ke main thread Unity
        if (newFilesQueue.TryDequeue(out string filePath))
        {
            StartCoroutine(LoadAndSpawnAssetAsync(filePath));
        }
    }

    private void OnDestroy()
    {
        // Pastikan FileSystemWatcher dimatikan dan dibersihkan saat objek ini dihancurkan
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    // Fungsi untuk memuat semua file yang sudah ada di folder saat game mulai
    private void LoadExistingAssets()
    {
        string[] existingFiles = Directory.GetFiles(folderPath, "*.png");
        foreach (string file in existingFiles) newFilesQueue.Enqueue(file);
    }

    // Fungsi untuk memulai FileSystemWatcher yang akan memantau folder secara real-time
    private void StartFileWatcher() 
    {
        watcher = new FileSystemWatcher(folderPath, "*.png");
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
        watcher.Created += OnFileCreated;
        watcher.Renamed += OnFileRenamed; 
        watcher.EnableRaisingEvents = true;
    }

    // Callback yang dipanggil ketika file baru dibuat di folder
    private void OnFileCreated(object sender, FileSystemEventArgs e) => newFilesQueue.Enqueue(e.FullPath);

    // Callback yang dipanggil ketika file di-rename di folder (untuk menangani kasus file baru yang langsung di-rename)
    private void OnFileRenamed(object sender, RenamedEventArgs e) 
    {
        if (e.FullPath.EndsWith(".png")) newFilesQueue.Enqueue(e.FullPath);
    }

    // Fungsi untuk memuat gambar dari file dan memprosesnya untuk di-spawn
    private IEnumerator LoadAndSpawnAssetAsync(string filePath)
    {
        yield return new WaitForSeconds(0.5f); // Delay kecil untuk memastikan file sudah siap dibaca

        string url = "file:///" + filePath.Replace("\\", "/"); // Ubah path ke format URL yang benar untuk UnityWebRequest

        // Gunakan UnityWebRequest untuk memuat gambar sebagai Texture2D
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                ProcessSpawn(filePath, texture);
            }
        }
    }

    // Fungsi untuk memproses gambar yang sudah dimuat dan menentukan kategori serta tipe objek untuk di-spawn CATEGORY_TYPE_TIMESTAMP.png
    private void ProcessSpawn(string filePath, Texture2D texture)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string[] nameParts = fileName.Split('_');

        if (nameParts.Length < 3) return; // Pastikan format nama file benar (minimal 3 bagian: CATEGORY_TYPE_TIMESTAMP)

        // Ekstrak kategori (FISH, TRASH, FOOD) dan tipe objek dari nama file
        string category = nameParts[0].ToUpper();
        string type = string.Join("_", nameParts, 1, nameParts.Length - 2); // Gabungkan kembali bagian tipe jika ada underscore di dalamnya

        // Regis Makanan 
        if (category == "FOOD")
        {
            Sprite foodSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            FoodManager.Instance.RegisterFoodSprite(foodSprite);
            return; 
        }

        // Regis Sampah
        if (category == "TRASH")
        {
            Sprite trashSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            availableTrashAssets.Add(new TrashAsset { sprite = trashSprite, typeName = type }); 
            
            // Jika belum ada proses spawn sampah yang berjalan, mulai proses spawn
            if (!isSpawningTrashPool)
            {
                StartCoroutine(TrashSpawnRoutine());
            }
            return; 
        }

        // --- LOGIKA IKAN ---
        GameObject newObj = new GameObject(fileName);
        SpriteRenderer sr = newObj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 10;

        BoxCollider2D collider = newObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        // Konversi nilai LayerMask ke Index Layer
        newObj.layer = (int)Mathf.Log(objectLayer.value, 2); 

        InitializeBehavior(newObj, category, type);

        if (spawnArea != null)
        {
            float radius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            
            // Kirimkan variabel 'type' ke dalam fungsi ini
            newObj.transform.position = GetValidSpawnPosition(radius, type);
        }
    }

    // Fungsi untuk menginisialisasi perilaku "behaviour" objek berdasarkan kategori dan tipenya
    private void InitializeBehavior(GameObject obj, string category, string type)
    {
        // Ambil parameter dari ConfigManager jika tersedia, jika tidak gunakan nilai default
        GameConfig config = ConfigManager.Instance != null ? ConfigManager.Instance.Config : new GameConfig {
            fishMinScale = 0.2f, fishMaxScale = 0.4f,
            trashMinScale = 0.3f, trashMaxScale = 0.5f
        };

        if (category == "FISH") // Kategori ikan, inisialisasi dengan FishBehavior
        {
            obj.tag = "Fish";
            float randomScale = Random.Range(config.fishMinScale, config.fishMaxScale);
            obj.transform.localScale = new Vector3(randomScale, randomScale, 1);
            obj.AddComponent<FishBehavior>().Setup(type);
            Debug.Log($"<color=cyan>Ikan [{type}] berhasil di-spawn.</color>");
        }
        
        else if (category == "TRASH") // Kategori sampah, inisialisasi dengan TrashBehavior
        {
            float randomScale = Random.Range(config.trashMinScale, config.trashMaxScale);
            obj.transform.localScale = new Vector3(randomScale, randomScale, 1);
            obj.AddComponent<TrashBehavior>().Setup(type);
            Debug.Log($"<color=cyan>Sampah [{type}] berhasil di-spawn.</color>"); 
        }
    }

    // Coroutine untuk mengatur spawn sampah secara berkala berdasarkan konfigurasi
    private IEnumerator TrashSpawnRoutine()
    {
        isSpawningTrashPool = true; 

        int maxTrash = ConfigManager.Instance != null ? ConfigManager.Instance.Config.maxTrashAmount : 10;
        float minInterval = ConfigManager.Instance != null ? ConfigManager.Instance.Config.trashSpawnIntervalMin : 10f;
        float maxInterval = ConfigManager.Instance != null ? ConfigManager.Instance.Config.trashSpawnIntervalMax : 20f;
        
        while (activeTrashInstances < maxTrash)
        {
            float randomWaitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(randomWaitTime); 

            int currentTrashCount = GameObject.FindGameObjectsWithTag("Trash").Length;

            if (availableTrashAssets.Count > 0)
            {
                SpawnSingleTrashInstance();
            }
        }

        isSpawningTrashPool = false; 
    }

    // encetak satu objek sampah ke layar (digunakan oleh siklus awal).
    private void SpawnSingleTrashInstance()
    {
        TrashAsset randomTrashData = GetRandomTrashAsset();
        
        GameObject newObj = new GameObject("TRASH_" + randomTrashData.typeName + "_Instance_" + activeTrashInstances);
        
        SpriteRenderer sr = newObj.AddComponent<SpriteRenderer>();
        sr.sprite = randomTrashData.sprite; 
        sr.sortingOrder = 9; // Pastikan sampah berada di bawah ikan (sortingOrder lebih rendah)

        BoxCollider2D collider = newObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        newObj.layer = (int)Mathf.Log(objectLayer.value, 2); 
        newObj.tag = "Trash";
        
        InitializeBehavior(newObj, "TRASH", randomTrashData.typeName);

        if (spawnArea != null)
        {
            float radius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            newObj.transform.position = GetValidSpawnPosition(radius);
        }
    }

    // Fungsi untuk mengambil data sampah secara acak dari daftar availableTrashAssets
    public TrashAsset GetRandomTrashAsset()
    {
        if (availableTrashAssets.Count == 0) return null;
        return availableTrashAssets[Random.Range(0, availableTrashAssets.Count)];
    }

    // Fungsi untuk mendapatkan posisi spawn yang valid di dalam spawnArea, dengan pengecualian khusus untuk kategori "STARFISH" yang hanya muncul di bagian bawah tank. 
    public Vector3 GetValidSpawnPosition(float radius, string objectType = "")
    {
        Bounds b = spawnArea.bounds;
        float minY = b.min.y;
        float maxY = b.max.y;

        // ---Bintang Laut hanya spawn di 25% area dasar akuarium---
        if (objectType.ToUpper() == "STARFISH")
        {
            float tankHeight = b.max.y - b.min.y;
            maxY = b.min.y + (tankHeight * 0.25f);
            if (maxY < minY) maxY = minY;
        }

        // Coba hingga 50 kali untuk menemukan posisi spawn yang valid (tidak bertabrakan dengan objek lain)
        for (int i = 0; i < 50; i++)
        {
            Vector2 pos = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(minY, maxY));

            // Jika radar kosong (tidak mendeteksi layer Spawnable), posisi tersebut aman
            if (Physics2D.OverlapCircle(pos, radius, objectLayer) == null)
            {
                return new Vector3(pos.x, pos.y, 0);
            }
        }
        
        // Return fallback jika tidak nemu tempat kosong
        return new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(minY, maxY), 0);
    }

    // Fungsi untuk membersihkan akuarium dengan menghancurkan semua ikan, sampah, dan makanan yang ada, serta mereset semua counter dan state terkait agar siap untuk sesi permainan baru.
    public void ClearAquarium()
    {
        // Hentikan semua proses bertahap (seperti TrashSpawnRoutine) yang sedang berjalan
        StopAllCoroutines();

        // Reset antrean file baru untuk memastikan tidak ada file lama yang tertinggal saat sesi baru dimulai
        newFilesQueue = new ConcurrentQueue<string>();

        // Hancurkan semua ikan
        GameObject[] fishes = GameObject.FindGameObjectsWithTag("Fish");
        foreach (GameObject fish in fishes)
        {
            Destroy(fish);
        }

        // Hancurkan semua sampah & reset counternya agar bisa mulai dari 0 lagi nanti
        GameObject[] trashes = GameObject.FindGameObjectsWithTag("Trash");
        foreach (GameObject trash in trashes)
        {
            Destroy(trash);
        }
        activeTrashInstances = 0;
        isSpawningTrashPool = false;
        availableTrashAssets.Clear(); // Bersihkan memori daftar sampah sebelumnya

        // Hancurkan semua makanan yang masih ada
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in foods)
        {
            food.SetActive(false);
        }
        
        Debug.Log("<color=yellow>Akuarium dibersihkan! Semua entitas dikembalikan ke state awal.</color>");
    }

    // Fungsi untuk merestart proses spawning setelah membersihkan akuarium, memastikan bahwa semua aset yang sudah ada di folder siap untuk di-spawn kembali.
    public void RestartSpawning()
    {
        StopAllCoroutines();
        
        // Pastikan semua proses spawn sebelumnya sudah dihentikan dan state terkait sudah di-reset
        newFilesQueue = new ConcurrentQueue<string>();

        if (watcher == null)
        {
            StartFileWatcher();
        }

        // Baca ulang semua gambar yang ada di folder dan masukkan ke antrean spawn
        LoadExistingAssets();

        // Jika sebelumnya sudah ada memori tentang sampah, mulai lagi rutinitas jatuhnya
        if (availableTrashAssets.Count > 0 && !isSpawningTrashPool)
        {
            StartCoroutine(TrashSpawnRoutine());
        }
    }
}