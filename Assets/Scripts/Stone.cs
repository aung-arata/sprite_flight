using UnityEngine;

public class Stone : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 2.0f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;
    Rigidbody2D rb;
    private float speedMultiplier = 1f;
    private float baseMovementSpeed;
    private Vector2 lastMovementDirection = Vector2.right;
    private bool hasBaseMovementSpeed;

    private const float MinimumVelocitySqrMagnitude = 0.001f;
    private const float MinimumBaseMovementSpeed = 1f;

    public GameObject bounceEffectPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);

        rb = GetComponent<Rigidbody2D>();
        float sizeRatio = Mathf.InverseLerp(minSize, maxSize, randomSize);
        float launchForce = Mathf.Lerp(maxSpeed, minSpeed, sizeRatio);
        Vector2 randomDirection = Random.insideUnitCircle;

        if (randomDirection.sqrMagnitude < MinimumVelocitySqrMagnitude)
        {
            randomDirection = Vector2.right;
        }

        rb.AddForce(randomDirection.normalized * launchForce);

        // float randomTorque = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        // rb.AddTorque(randomTorque);
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;
        if (currentVelocity.sqrMagnitude > MinimumVelocitySqrMagnitude)
        {
            lastMovementDirection = currentVelocity.normalized;

            if (!hasBaseMovementSpeed)
            {
                baseMovementSpeed = Mathf.Max(
                    currentVelocity.magnitude / speedMultiplier,
                    MinimumBaseMovementSpeed);
                hasBaseMovementSpeed = true;
            }
        }

        if (hasBaseMovementSpeed)
        {
            rb.linearVelocity = lastMovementDirection * (baseMovementSpeed * speedMultiplier);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 contactPoint = collision.GetContact(0).point; 
        GameObject bounceEffect = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);

        // Destroy the effect after 1 second
        Destroy(bounceEffect, 1f);
    }

    public void SetSpeedMultiplier(float newMultiplier, Vector2 arenaCenter)
    {
        newMultiplier = Mathf.Max(1f, newMultiplier);

        if (rb != null && speedMultiplier > 0f)
        {
            Vector2 currentVelocity = rb.linearVelocity;

            if (!hasBaseMovementSpeed && currentVelocity.sqrMagnitude > MinimumVelocitySqrMagnitude)
            {
                baseMovementSpeed = Mathf.Max(
                    currentVelocity.magnitude / speedMultiplier,
                    MinimumBaseMovementSpeed);
                hasBaseMovementSpeed = true;
            }

            Vector2 currentDirection = currentVelocity.sqrMagnitude > 0.001f
                ? currentVelocity.normalized
                : lastMovementDirection;
            Vector2 directionToCenter = arenaCenter - (Vector2)transform.position;

            if (directionToCenter.sqrMagnitude > 4f)
            {
                currentDirection = Vector2.Lerp(
                    currentDirection,
                    directionToCenter.normalized,
                    0.6f).normalized;
                currentDirection = Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(-20f, 20f)) * currentDirection;
            }

            lastMovementDirection = currentDirection;
            float targetSpeed = hasBaseMovementSpeed
                ? baseMovementSpeed * newMultiplier
                : newMultiplier;
            rb.linearVelocity = currentDirection * targetSpeed;
            rb.WakeUp();
        }

        speedMultiplier = newMultiplier;
    }
}
