using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Movement Settings")]
    [Tooltip("Time in seconds it takes to travel from one point to the other.")]
    [SerializeField] private float journeyTime = 3f;

    public Vector2 Velocity { get; private set; }

    private Rigidbody2D rb;
    private float timer = 0f;
    private bool movingToEnd = true;

    // Store the pre-calculated velocities
    private Vector2 velocityTowardsEnd;
    private Vector2 velocityTowardsStart;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (startPoint != null && endPoint != null && journeyTime > 0)
        {
            transform.position = startPoint.position;

            // Calculate constant velocity: Velocity = Distance / Time
            Vector2 distance = endPoint.position - startPoint.position;
            velocityTowardsEnd = distance / journeyTime;
            velocityTowardsStart = -velocityTowardsEnd;
        }
    }

    void FixedUpdate()
    {
        if (startPoint == null || endPoint == null || journeyTime <= 0f) return;

        timer += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(timer / journeyTime);

        Vector3 currentPos = movingToEnd
            ? Vector3.Lerp(startPoint.position, endPoint.position, progress)
            : Vector3.Lerp(endPoint.position, startPoint.position, progress);

        rb.MovePosition(currentPos);

        // Apply the pre-calculated velocity based on our current direction
        Velocity = movingToEnd ? velocityTowardsEnd : velocityTowardsStart;

        if (progress >= 1f)
        {
            timer = 0f;
            movingToEnd = !movingToEnd;
        }
    }
}