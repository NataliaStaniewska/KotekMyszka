using UnityEngine;
using UnityEngine.UIElements;

public class MouseEscape : MonoBehaviour
{
    public Transform cat;

    public float moveSpeed = 3f;
    public float wanderSpeed = 1.2f;
    public float escapeDistance = 4f;
    public float arenaSize = 5f;
    public float margin = 0.5f;

    public float randomness = 0.5f;
    public float directionChangeInterval = 0.4f;

    public float wallAvoidanceDistance = 1.0f;
    public float wallAvoidanceStrength = 2.0f;

    private Vector3 currentDirection;
    private float directionTimer;



    void Update()
    {
        if (cat == null)
        {
            return;
        }

        Vector3 directionFromCat = transform.localPosition - cat.localPosition;
        float distance = directionFromCat.magnitude;

        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            Vector3 wallAvoidance = CalculateWallAvoidance();

            if (distance < escapeDistance && distance > 0.001f)
            {
                // Tryb ucieczki od kota
                Vector3 awayFromCat = directionFromCat.normalized;

                Vector3 randomOffset = new Vector3(
                    Random.Range(-randomness, randomness),
                    Random.Range(-randomness, randomness),
                    0f
                );

                currentDirection =
                    (awayFromCat + wallAvoidance * wallAvoidanceStrength + randomOffset).normalized;
            }
            else
            {
                // Tryb swobodnego poruszania się po planszy
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f
                ).normalized;

                currentDirection =
                    (randomDirection + wallAvoidance * wallAvoidanceStrength).normalized;
            }

            directionTimer = directionChangeInterval;
        }

        float speed = distance < escapeDistance ? moveSpeed : wanderSpeed;

        Vector3 newPosition = transform.localPosition +
                              currentDirection * speed * Time.deltaTime;

        float min = -arenaSize + margin;
        float max = arenaSize - margin;

        newPosition.x = Mathf.Clamp(newPosition.x, min, max);
        newPosition.y = Mathf.Clamp(newPosition.y, min, max);
        newPosition.z = 0f;

        transform.localPosition = newPosition;
    }

    private Vector3 CalculateWallAvoidance()
    {
        Vector3 avoidance = Vector3.zero;

        float min = -arenaSize + margin;
        float max = arenaSize - margin;

        Vector3 position = transform.localPosition;

        if (position.x < min + wallAvoidanceDistance)
        {
            avoidance.x += 1f;
        }

        if (position.x > max - wallAvoidanceDistance)
        {
            avoidance.x -= 1f;
        }

        if (position.y < min + wallAvoidanceDistance)
        {
            avoidance.y += 1f;
        }

        if (position.y > max - wallAvoidanceDistance)
        {
            avoidance.y -= 1f;
        }

        return avoidance.normalized;
    }
}









