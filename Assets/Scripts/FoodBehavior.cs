using UnityEngine;
using System.Collections;

// mengatur perilaku makanan yang jatuh di akuarium. 
public class FoodBehavior : MonoBehaviour
{
    private float fallSpeed = 1.0f; 
    private float bottomY;
    private bool hasLanded = false;

    public void Setup(float bottomBound)
    {
        bottomY = bottomBound;
    }

    // Fungsi untuk mereset makanan saat ditarik dari Pool
    public void ResetFood()
    {
        hasLanded = false;
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        StopAllCoroutines(); // Batalkan timer hitung mundur sebelumnya
    }

    void Update()
    {
        if (hasLanded) return;

        // Gerakan jatuh alami
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        if (transform.position.y <= bottomY)
        {
            hasLanded = true;
            transform.position = new Vector3(transform.position.x, bottomY, transform.position.z);
            
            // --- Mulai timer untuk menyembunyikan makanan ---
            StartCoroutine(DeactivateAfterDelay(5f));
        }
    }

    // Fungsi untuk menyembunyikan makanan setelah beberapa detik di lantai akuarium, sehingga bisa digunakan kembali oleh Pool.
    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false); // Kembalikan makanan ke Pool (tidak di-Destroy)
    }
}