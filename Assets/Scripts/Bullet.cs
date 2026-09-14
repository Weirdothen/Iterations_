using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;

    private IObjectPool<Bullet> managedPool;
    private float timer;
    public void SetPool(IObjectPool<Bullet> pool)
    {
        managedPool = pool;
    }

    void OnEnable()
    {
        timer = lifeTime;
    }

    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        managedPool?.Release(this);
    }
}