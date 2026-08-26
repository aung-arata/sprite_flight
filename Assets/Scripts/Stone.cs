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

    public GameObject bounceEffectPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);

        rb = GetComponent<Rigidbody2D>();
        // float randomSpeed = Random.Range(minSpeed, maxSpeed);
        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        Vector2 randomDirection = Random.insideUnitCircle;
        rb.AddForce(randomDirection * randomSpeed);

        // float randomTorque = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        // rb.AddTorque(randomTorque);
    }

    // Update is called once per frame
    void Update()
    {
        
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
            float newSpeed = currentVelocity.magnitude * (newMultiplier / speedMultiplier);
            Vector2 currentDirection = currentVelocity.sqrMagnitude > 0.001f
                ? currentVelocity.normalized
                : Random.insideUnitCircle.normalized;
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

            rb.linearVelocity = currentDirection * Mathf.Max(newSpeed, newMultiplier);
            rb.WakeUp();
        }

        speedMultiplier = newMultiplier;
    }
}
