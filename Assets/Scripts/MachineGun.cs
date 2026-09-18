using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

public class MachineGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Bullet bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;

    [Header("Game Feel - Visuals & Feedback")]
    public Transform gunVisual;
    public ParticleSystem muzzleFlash;

    [Header("Recoil Settings")]
    public float recoilStrength = 0.2f;
    public float recoilDuration = 0.1f;

    [Header("Pool Settings")]
    public int defaultCapacity = 20;
    public int maxSize = 50;

    private IObjectPool<Bullet> bulletPool;
    private float nextFireTime;
    private Vector3 originalVisualPosition;

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

        if (gunVisual != null)
            originalVisualPosition = gunVisual.localPosition;
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

        ApplyGameFeelEffects();
    }

    private void ApplyGameFeelEffects()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (gunVisual != null)
        {
            gunVisual.DOKill();
            gunVisual.localPosition = originalVisualPosition;

            gunVisual.DOLocalMoveX(originalVisualPosition.x - recoilStrength, recoilDuration / 2f)
                     .SetEase(Ease.OutBack)
                     .OnComplete(() => gunVisual.DOLocalMoveX(originalVisualPosition.x, recoilDuration / 2f));
        }
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