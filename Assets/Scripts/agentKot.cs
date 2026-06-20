using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class CatAgent : Agent
{
    public Transform mouse;
    public float moveSpeed = 5f;
    public float arenaSize = 5f;

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

        Vector3 movement = new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
        transform.localPosition += movement;

        float currentDistance = Vector3.Distance(transform.localPosition, mouse.localPosition);

        // Mała kara za każdy krok, żeby agent nie tracił czasu
        AddReward(-0.001f);

        // Nagroda za zbliżanie się do myszy
        if (currentDistance < previousDistance)
        {
            AddReward(0.005f);
        }
        else
        {
            AddReward(-0.005f);
        }

        previousDistance = currentDistance;

        // Kot złapał mysz
        ColliderDistance2D colliderDistance = catCollider.Distance(mouseCollider);

        if (colliderDistance.isOverlapped)
        {
            SetReward(1f);
            EndEpisode();
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
            EndEpisode();
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