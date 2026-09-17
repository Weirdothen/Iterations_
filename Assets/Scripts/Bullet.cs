using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;

    private IObjectPool<Bullet> managedPool;
    private float timer;

    private Rigidbody2D rb;
   
    public void SetPool(IObjectPool<Bullet> pool)
    {
        managedPool = pool;
    }

    void OnEnable()
    {
        timer = lifeTime;
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
    }

    void Update()
    {
       // transform.Translate(Vector2.up * speed * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ReturnToPool();
        }
    }

    

    private void OnTriggerEnter2D(Collider2D collision)
    {

        ReturnToPool();
    }
    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;

        managedPool?.Release(this);

    }
}