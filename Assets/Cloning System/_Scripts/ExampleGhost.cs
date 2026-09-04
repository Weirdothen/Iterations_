using UnityEngine;


public class ExampleGhost : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    private float _lastXPos;

    private void Awake() => _renderer = GetComponentInChildren<SpriteRenderer>();

    private void OnEnable()
    {
        _lastXPos = transform.position.x;
    }


    private void Update() => FaceCorrectDirection(transform.position.x);

    private void FaceCorrectDirection(float xPos)
    {
        if (xPos > _lastXPos)
        {
            _renderer.flipX = false;
            _lastXPos = xPos;
        }
        else if (xPos < _lastXPos)
        {
            _renderer.flipX = true;
            _lastXPos = xPos;
        }
    }
}
