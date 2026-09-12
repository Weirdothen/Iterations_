using UnityEngine;

public class DestroyParticle : MonoBehaviour
{

    [SerializeField] private float destoryTime = .5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        Destroy(gameObject, destoryTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
