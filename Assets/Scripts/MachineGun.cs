using UnityEngine;
using UnityEngine.Pool;

public class MachineGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Bullet bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Header("Pool Settings")]
    public int defaultCapacity = 20;
    public int maxSize = 50;

    private IObjectPool<Bullet> bulletPool;
    private float nextFireTime;

    void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            CreateBullet,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            true,
            defaultCapacity,
            maxSize
        );
    }

    void Update()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        Bullet bullet = bulletPool.Get();

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;
    }

    #region Pool Callbacks

    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab);
        bullet.SetPool(bulletPool);
        return bullet;
    }

    private void OnTakeFromPool(Bullet bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(Bullet bullet)
    {
        Destroy(bullet.gameObject);
    }

    #endregion
}