using System.Collections;
using UnityEngine;

public class ArrowAttack : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float lifetime = 1.5f;  // Đạn tự hủy sau X giây
    [SerializeField] private GameObject arrowVFX;

    private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private bool isFired = false; // Kiểm tra xem đã bắn chưa

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateArrowSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        ChangeVFXColor();
    }

    private void ChangeVFXColor()
    {
        if (arrowVFX != null && spriteRenderer.sprite != null)
        {
            Color pivotColor = GetColorAtPivot(spriteRenderer.sprite);
            pivotColor.a = 1f; // Đảm bảo màu không bị mờ
            ParticleSystem ps = arrowVFX.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var main = ps.main;
                main.startColor = pivotColor; 
                //ps.Play();
            }
        }
    }

    private Color GetColorAtPivot(Sprite sprite)
    {
        Texture2D texture = sprite.texture;

        // Kiểm tra nếu texture không thể đọc
        if (!texture.isReadable)
        {
            Debug.LogWarning("Texture không thể đọc, vui lòng bật Read/Write Enabled trong Import Settings.");
            return Color.white;
        }

        Vector2 normalizedPivot = sprite.pivot / sprite.rect.size; // Đưa pivot về dạng Normalized
        int pixelX = Mathf.RoundToInt(normalizedPivot.x * texture.width);
        int pixelY = Mathf.RoundToInt(normalizedPivot.y * texture.height);

        // Đảm bảo pivot nằm trong giới hạn hợp lệ của texture
        pixelX = Mathf.Clamp(pixelX, 0, texture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texture.height - 1);

        // Lấy màu tại vị trí pivot chính xác
        Color pivotColor = texture.GetPixel(pixelX, pixelY);

        // Lấy màu tại vị trí pivot chính xác
        return texture.GetPixel(pixelX, pixelY);
    }




    public void Shoot(Vector2 firePosition, Vector2 targetPosition)
    {
        Debug.Log("ArrowAttack.Shoot()");
        transform.position = firePosition;

        Vector2 direction = (targetPosition - firePosition).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        rb.linearVelocity = direction * bulletSpeed;

        gameObject.SetActive(true);

        Destroy(gameObject, lifetime);

        isFired = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy")) 
        {
            LizzerController lizzer = other.GetComponent<LizzerController>();
            Instantiate(arrowVFX, transform.position, Quaternion.identity);
            if (lizzer != null)
            {
                lizzer.TakeDamage(10); 
            }

            Destroy(gameObject);
        }
    }

}
