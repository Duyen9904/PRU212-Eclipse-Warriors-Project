using System.Collections;
using UnityEngine;

public class ArrowAttack : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float lifetime = 3f;  // Đạn tự hủy sau X giây
    [SerializeField] private GameObject arrowVFX;

    private Rigidbody2D rb;
    private bool isFired = false; // Kiểm tra xem đã bắn chưa

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Shoot(Vector2 firePosition, Vector2 targetPosition)
    {
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
        if (other.CompareTag("Lizzer")) 
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
