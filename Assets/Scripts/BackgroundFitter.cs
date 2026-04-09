using UnityEngine;

public class BackgroundFitter : MonoBehaviour
{
    void Start()
    {
        FitToScreen();
    }

    private void FitToScreen()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        Vector3 cameraPos = Camera.main.transform.position;
        transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);


        float cameraHeight = Camera.main.orthographicSize * 2;
        Vector2 cameraSize = new Vector2(Camera.main.aspect * cameraHeight, cameraHeight);

        Vector2 spriteSize = sr.sprite.bounds.size;

        float scaleX = cameraSize.x / spriteSize.x;
        float scaleY = cameraSize.y / spriteSize.y;

        float finalScale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(finalScale, finalScale, 1f);
    }
}
