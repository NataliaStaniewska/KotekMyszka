using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using TMPro;

public class CatAgent : Agent
{
    public Transform mouse;
    public float moveSpeed = 6f;
    public float arenaSize = 5f;

    ///
    public LayerMask obstacleLayer;
    public float obstacleStuckTimeLimit = 1.0f;

    private float obstacleContactTimer = 0f;
    /// 

    //napisy
    public TextMeshProUGUI scoreText;

    private int roundNumber = 1;
    private int catWins = 0;
    private int mouseWins = 0;

    //limit czasu
    public float episodeTimeLimit = 15f;
    private float episodeTimer = 0f;



    private float previousDistance;
    private Collider2D catCollider;
    private Collider2D mouseCollider;

    public Collider2D arenaCollider;

    public override void Initialize()
{
    catCollider = GetComponent<Collider2D>();
    mouseCollider = mouse.GetComponent<Collider2D>();
}

    public override void OnEpisodeBegin()
    {

        // Losowe ustawienie kota
        transform.localPosition = new Vector3(
            Random.Range(-arenaSize+1, arenaSize-1),
            Random.Range(-arenaSize+1, arenaSize-1),
            0f
        );

        // Losowe ustawienie myszy
        mouse.localPosition = new Vector3(
            Random.Range(-arenaSize+1, arenaSize-1),
            Random.Range(-arenaSize+1, arenaSize-1),
            0f
        );

        previousDistance = Vector3.Distance(transform.localPosition, mouse.localPosition);
        // licznik czasu przy przeszkodach
        obstacleContactTimer = 0f;
        // licznik czasu rundy
        episodeTimer = 0f;

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Runda: {roundNumber} | Kot: {catWins} | Mysz: {mouseWins}";
        }
    }

    private void EndRound(bool catWon)
    {
        if (catWon)
        {
            catWins++;
        }
        else
        {
            mouseWins++;
        }

        roundNumber++;
        UpdateScoreText();
        EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Pozycja kota
        sensor.AddObservation(transform.localPosition.x / arenaSize);
        sensor.AddObservation(transform.localPosition.y / arenaSize);

        // Pozycja myszy
        sensor.AddObservation(mouse.localPosition.x / arenaSize);
        sensor.AddObservation(mouse.localPosition.y / arenaSize);

        // Różnica pozycji: gdzie jest mysz względem kota
        Vector3 directionToMouse = mouse.localPosition - transform.localPosition;
        sensor.AddObservation(directionToMouse.x / arenaSize);
        sensor.AddObservation(directionToMouse.y / arenaSize);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // sprawdzanie czasu
        episodeTimer += Time.deltaTime;

        if (episodeTimer >= episodeTimeLimit)
        {
            SetReward(-1.5f);
            EndRound(false);
            return;
        }

        Vector3 movement = new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
        transform.localPosition += movement;

        // sprawdzenie blokowania przy przeszkodzie
        if (catCollider.IsTouchingLayers(obstacleLayer))
        {
            obstacleContactTimer += Time.deltaTime;
            AddReward(-0.003f);

            if (obstacleContactTimer >= obstacleStuckTimeLimit)
            {
                SetReward(-1f);
                EndRound(false);
                return;
            }
        }
        else
        {
            obstacleContactTimer = 0f;
        }





        float currentDistance = Vector3.Distance(transform.localPosition, mouse.localPosition);

        // kara za każdy krok, ale niezbyt mocna
        AddReward(-0.002f);

        // nagroda za zmniejszanie odległości do myszy
        float distanceChange = previousDistance - currentDistance;
        AddReward(distanceChange * 0.05f);
        previousDistance = currentDistance;

        // Dodatkowa nagroda za ruch w kierunku myszy
        if (movement.sqrMagnitude > 0.0001f)
        {
            Vector3 directionToMouseNormalized =
                (mouse.localPosition - transform.localPosition).normalized;

            Vector3 movementDirection = movement.normalized;

            float chaseAlignment =
                Vector3.Dot(movementDirection, directionToMouseNormalized);

            AddReward(chaseAlignment * 0.006f);
        }







        // Kot złapał mysz
        ColliderDistance2D colliderDistance = catCollider.Distance(mouseCollider);

        if (colliderDistance.isOverlapped)
        {
            float timeBonus = 1f - (episodeTimer / episodeTimeLimit);
            timeBonus = Mathf.Clamp01(timeBonus);

            SetReward(2f + timeBonus);
            EndRound(true);
            return;
        }

        // Kot wyszedł poza planszę
        Bounds catBounds = catCollider.bounds;
        Bounds arenaBounds = arenaCollider.bounds;

        if (catBounds.min.x < arenaBounds.min.x ||
            catBounds.max.x > arenaBounds.max.x ||
            catBounds.min.y < arenaBounds.min.y ||
            catBounds.max.y > arenaBounds.max.y)
        {
            SetReward(-1f);
            EndRound(false);
            return;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions = actionsOut.ContinuousActions;

        float moveX = 0f;
        float moveY = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Ruch w lewo
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            moveX = -1f;
        }

        // Ruch w prawo
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            moveX = 1f;
        }

        // Ruch w górę
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            moveY = 1f;
        }

        // Ruch w dół
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            moveY = -1f;
        }

        actions[0] = moveX;
        actions[1] = moveY;
    }
}