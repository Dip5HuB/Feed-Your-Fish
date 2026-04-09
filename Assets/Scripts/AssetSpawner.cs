using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AssetSpawner : MonoBehaviour
{
    
    public BoxCollider2D spawnArea;
    
    public LayerMask objectLayer; 
    
    private string folderPath;
    private FileSystemWatcher watcher;
    private ConcurrentQueue<string> newFilesQueue = new ConcurrentQueue<string>();

    void Start()
    {
        // Menentukan path folder di dalam Assets
        folderPath = Path.Combine(Application.dataPath, "AquascapeAssets");
        folderPath = Path.GetFullPath(folderPath); 

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        LoadExistingAssets();

        StartFileWatcher();
    }

    private void LoadExistingAssets()
    {
        string[] existingFiles = Directory.GetFiles(folderPath, "*.png");
        foreach (string file in existingFiles)
        {
            newFilesQueue.Enqueue(file);
        }
    }

    private void StartFileWatcher()
    {
        // Memantau penambahan aset baru secara berkala
        watcher = new FileSystemWatcher(folderPath, "*.png");
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
        watcher.Created += OnFileCreated;
        watcher.Renamed += OnFileRenamed; 
        watcher.EnableRaisingEvents = true;
        
        Debug.Log("<color=green>Sistem File Watcher aktif di: </color>" + folderPath); 
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e) => newFilesQueue.Enqueue(e.FullPath);
    
    private void OnFileRenamed(object sender, RenamedEventArgs e) 
    {
        if (e.FullPath.EndsWith(".png")) newFilesQueue.Enqueue(e.FullPath);
    }

    void Update()
    {
        // Memproses antrean file dari thread background ke thread utama Unity
        if (newFilesQueue.TryDequeue(out string filePath))
        {
            StartCoroutine(LoadAndSpawnAssetAsync(filePath));
        }
    }

    private IEnumerator LoadAndSpawnAssetAsync(string filePath)
    {
        // Load asinkron sebagai tekstur tanpa re-compile
        yield return new WaitForSeconds(0.5f); 

        string url = "file:///" + filePath.Replace("\\", "/");
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                ProcessSpawn(filePath, texture);
            }
            else
            {
                Debug.LogError("Gagal memuat gambar: " + uwr.error);
            }
        }
    }

    // --- Memisahkan logika pengecekan nama dan perakitan objek ---

    private void ProcessSpawn(string filePath, Texture2D texture)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string[] nameParts = fileName.Split('_');

        // Validasi format nama file(CATEGORY_TYPE_TIMESTAMP)
        if (nameParts.Length < 3)
        {
            Debug.LogWarning($"<color=orange>Format nama file salah ({fileName}). Harus: CATEGORY_TYPE_TIMESTAMP.png</color>");
            return; 
        }

        string category = nameParts[0].ToUpper(); // FISH atau TRASH
        string type = nameParts[1];

        // Membuat GameObject baru
        GameObject newObj = new GameObject(fileName);
        
        // Tambahkan Visual (Sprite)
        SpriteRenderer sr = newObj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 10;

        // Tambahkan Collider agar bisa dideteksi oleh OverlapCircle
        BoxCollider2D collider = newObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // Pastikan objek berada di Layer yang benar untuk deteksi overlap
        newObj.layer = (int)Mathf.Log(objectLayer.value, 2); 

        InitializeBehavior(newObj, category, type);

        // Logika Overlap & Posisi
        if (spawnArea != null)
        {
            float radius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            newObj.transform.position = GetValidSpawnPosition(radius);
        }

        // Inisialisasi Perilaku (Modularity)
        InitializeBehavior(newObj, category, type);
    }

    private void InitializeBehavior(GameObject obj, string category, string type)
    {
        GameConfig config = ConfigManager.Instance != null ? ConfigManager.Instance.Config : new GameConfig {
            fishMinScale = 0.35f,
            fishMaxScale = 0.8f,
            trashMinScale = 0.2f,
            trashMaxScale = 0.6f
        };

        if (category == "FISH")
        {
            obj.tag = "Fish";

            float randomScale = Random.Range(config.fishMinScale, config.fishMaxScale);
            obj.transform.localScale = new Vector3(randomScale, randomScale, 1);

            Debug.Log($"<color=cyan>Ikan [{type}] berhasil di-spawn.</color>");
        }
        else if (category == "TRASH")
        {
            obj.tag = "Trash";
            
            float randomScale = Random.Range(config.trashMinScale, config.trashMaxScale);
            obj.transform.localScale = new Vector3(randomScale, randomScale, 1);
            
            Debug.Log($"<color=cyan>Sampah [{type}] berhasil di-spawn.</color>");
        }
        else
        {
            Debug.LogWarning("Kategori tidak dikenali, objek tetap dibuat tanpa script behavior.");
        }
    }

    private Vector3 GetValidSpawnPosition(float radius)
    {
        Bounds b = spawnArea.bounds;
        for (int i = 0; i < 50; i++)
        {
            Vector2 pos = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
            
            // Menggunakan objectLayer agar tidak overlap dengan objek sejenis 
            if (Physics2D.OverlapCircle(pos, radius, objectLayer) == null)
            {
                return new Vector3(pos.x, pos.y, 0);
            }
        }
        return new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0);
    }

    private void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }
}