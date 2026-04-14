using UnityEngine;
using System.Collections.Generic;

public class FoodManager : MonoBehaviour
{
    public static FoodManager Instance;
    public Sprite currentFoodSprite;

    private List<GameObject> foodPool = new List<GameObject>();
    public int maxFoodPoolSize = 20; 

    void Awake()
    {
        Instance = this;
    }

    public void RegisterFoodSprite(Sprite s)
    {
        currentFoodSprite = s;
        Debug.Log("Aset makanan berhasil didaftarkan.");
    }

    // --- Daur Ulang Tanpa Henti ---
    public GameObject GetPooledFood()
    {
        // Cari makanan yang sedang tersembunyi
        foreach (GameObject food in foodPool)
        {
            if (!food.activeInHierarchy)
            {
                // Pindahkan makanan ini ke urutan paling belakang (akhir List)
                // agar rotasi daur ulangnya adil dan teratur
                foodPool.Remove(food);
                foodPool.Add(food);
                return food;
            }
        }

        // Jika belum mencapai batas maksimal (20), buat makanan baru
        if (foodPool.Count < maxFoodPoolSize)
        {
            GameObject newFood = CreateNewFoodObject();
            foodPool.Add(newFood); // Masuk otomatis di urutan belakang
            return newFood;
        }

        // JIKA SUDAH PENUH (20 aktif semua)
        // Ambil paksa makanan yang usianya paling lama di layar (selalu berada di indeks 0)
        GameObject oldestFood = foodPool[0];
        
        // Pindahkan makanan tua ini ke urutan paling belakang list
        foodPool.RemoveAt(0);
        foodPool.Add(oldestFood);
        
        // Kembalikan makanan yang paling lama ini untuk di-teleport ke posisi klik mouse baru
        return oldestFood; 
    }

    private GameObject CreateNewFoodObject()
    {
        GameObject food = new GameObject("Food_Instance");
        food.tag = "Food";
        food.SetActive(false); 
        
        food.AddComponent<SpriteRenderer>().sprite = currentFoodSprite;
        
        CircleCollider2D collider = food.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f; 
        
        food.transform.localScale = new Vector3(0.2f, 0.2f, 1); 

        var spawner = FindFirstObjectByType<AssetSpawner>();
        float bottomLimit = -5f; 
        if (spawner != null && spawner.spawnArea != null)
        {
            bottomLimit = spawner.spawnArea.bounds.min.y; 

            // Samakan layer makanan dengan layer objek lainnya
            food.layer = (int)Mathf.Log(spawner.objectLayer.value, 2);
        }

        food.AddComponent<FoodBehavior>().Setup(bottomLimit);

        return food;
    }
}