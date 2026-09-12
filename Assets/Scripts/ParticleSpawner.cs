using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private float playerHieght = .5f;
    public void SpawnParticle(GameObject particlePrefab)
    {
        Instantiate(particlePrefab, transform.position + new Vector3(0,playerHieght,0), Quaternion.identity);
    }
}
