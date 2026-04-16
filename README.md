# Aquascape Simulation Game
Sebuah simulasi akuarium interaktif 2D berbasis Unity. Game ini menampilkan ekosistem akuarium mandiri di mana ikan dapat berenang, merasa lapar, dan mencari makan. Dilengkapi dengan sistem *File-Watching* asinkron yang memungkinkan penambahan aset gambar baru tanpa perlu *re-compile* atau *restart* aplikasi.

## 🛠️ Persyaratan Sistem & Library yang Digunakan
Proyek ini dibangun menggunakan **Unity** (Versi Unity 6.3.12f1) dan memanfaatkan beberapa *library* bawaan C# dan Unity:
* **`UnityEngine.InputSystem`**: Menggunakan *New Input System* Unity untuk mendeteksi interaksi klik mouse (*Screen to World Point*).
* **`System.IO` (FileSystemWatcher)**: Digunakan dalam *AssetSpawner* untuk memantau perubahan folder lokal secara *real-time* dan menangkap *event* pembuatan file baru.
* **`System.Collections.Concurrent` (ConcurrentQueue)**: Digunakan untuk antrean file baru yang aman (*Thread-Safe*), memindahkan data dari *thread FileWatcher* latar belakang ke *Main Thread* Unity.
* **`UnityEngine.Networking` (UnityWebRequest)**: Digunakan untuk memuat file gambar `.png` lokal secara asinkron (*Coroutine*) menjadi tekstur tanpa menyebabkan *freeze* (jeda) pada aplikasi.

## 🎮 Cara Memainkan Game
1.  Buka aplikasi / *file executable* (`Aquascape.exe`).
2.  klik tombol **PLAY** untuk memulai simulasi.
3.  **Interaksi Pemain:**
    * **Memberi Makan:** Klik kiri pada area air yang kosong untuk menjatuhkan pelet makanan.
    * **Membersihkan Sampah:** Klik kiri pada objek sampah yang mengambang untuk membersihkannya. (Sampah akan muncul kembali secara acak setelah beberapa saat).
    * **Mengagetkan Ikan:** Klik kiri pada ikan untuk membuatnya terkejut dan berenang cepat menjauh dari titik tersebut.
4.  Klik **EXIT** untuk menutup permainan.

## 📁 Cara Menambah Aset Baru (Saat Runtime)
Game ini mendukung penambahan aset gambar secara langsung saat game sedang berjalan. 

1.  Buka folder **`AquascapeAssets`** yang berada di dalam direktori data game (sejajar dengan file `config.json`).
2.  Siapkan gambar dengan format **`.png`**.
3.  Ubah nama file gambar tersebut dengan aturan (*Naming Convention*) berikut:
    * **IKAN:** `FISH_[TipeIkan]_[Timestamp].png` 
        * *(Contoh: `FISH_MACKEREL_20260414100000.png`)*
        * *Catatan Khusus:* Gunakan nama `FISH_STARFISH_...` agar ikan tersebut secara spesifik memiliki *behavior* merayap di area dasar laut (25% layar bawah).
    * **SAMPAH:** `TRASH_[TipeSampah]_[Timestamp].png`
        * *(Contoh: `TRASH_PLASTIC_BAG_20260414100000.png`)*
    * **MAKANAN:** `FOOD_[TipeMakanan]_[Timestamp].png`
        * *(Contoh: `FOOD_PELLET_20260414100000.png`)*
4.  *Copy-paste* file gambar tersebut ke dalam folder `AquascapeAssets`. Game akan langsung memproses dan memunculkan objek tersebut di layar.

## ⚙️ Konfigurasi Game (`config.json`)
Variabel simulasi dapat diatur tanpa membuka Unity Editor. Buka file `config.json` di *root folder* menggunakan Notepad untuk mengatur:
* Kecepatan renang ikan dan sampah (`minSpeed`, `maxSpeed`).
* Batas ukuran (*scale*) acak objek (`minScale`, `maxScale`).
* Jarak deteksi makanan dan jeda lapar ikan.
* Jumlah maksimal sampah dan interval waktu kemunculannya.

---

## 🤖 Deklarasi Penggunaan Artificial Intelligence (AI)
Dalam penyelesaian ini, saya menggunakan bantuan *Artificial Intelligence* dengan rincian sebagai berikut:

* **Nama Model AI:** Google Gemini (Gemini Advanced)
* **Sejauh Mana Penggunaan AI:**
    1.  **Code Refactoring & Optimization:** Saya merancang arsitektur awal dan logika utama secara mandiri, kemudian menggunakan AI sebagai "teman diskusi" (Pair Programming) untuk mengulas (*review*) dan merapikan kode (*Clean Code*).
    2.  **Optimasi Algoritma:** AI membantu memberikan saran implementasi yang lebih hemat memori, seperti penerapan pola *Circular Object Pooling* pada manajer makanan, serta konversi deteksi klik dari *Raycast* konvensional ke *OverlapPoint* untuk efisiensi komputasi 2D.
    3.  **Problem Solving:** AI membantu mendiagnosa dan menyempurnakan kalkulasi matematika untuk *Screen Boundaries* (`Mathf.Clamp` dikombinasikan dengan luasan *Sprite Bounds*) serta penanganan *loop* tabrakan pada logika *Collision Avoidance* ikan.
    4.  **Dokumentasi:** AI membantu menyusun draf dokumentasi agar rapih.