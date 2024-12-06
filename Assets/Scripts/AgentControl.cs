using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentControl : Agent
{
    // Serialized Variables
    [SerializeField] private Vector3 SpawnAreaMin = new Vector3(-5, 92, -5);
    [SerializeField] private Vector3 SpawnAreaMax = new Vector3(5, 92, 5);

    [SerializeField] private Vector3 GoalSpawnAreaMin = new Vector3(-5, 92, -5);
    [SerializeField] private Vector3 GoalSpawnAreaMax = new Vector3(5, 92, 5);

    [SerializeField] private Vector3 MinBounds = new Vector3(-500, 80f, -500);
    [SerializeField] private Vector3 MaxBounds = new Vector3(500, 500f, 500);

    [SerializeField] private Vector2 RotationYRange = new Vector2(-30, 30);
    [SerializeField] private float MaxDistance = 2000f;

    public Particle3D root;
    public AircraftController controller;
    [SerializeField] GameObject GoalPrefab;
    private GameObject CurrentGoal;

    private float timer = 0f;
    private float maxTime = 200f; // Max time for episode (seconds)
    public bool isFlying = false;

    // Initialization Methodsp
    private void Awake()
    {
        if (CurrentGoal == null)
        {
            CurrentGoal = Instantiate(GoalPrefab, GetRandomPosition(GoalSpawnAreaMin, GoalSpawnAreaMax), Quaternion.identity);
        }
    }

    public Vector3 GetRandomPosition(Vector3 min, Vector3 max)
    {
        return new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }

    public Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, Random.Range(RotationYRange.x, RotationYRange.y), 0);
    }

    public PhysicsCollider getGoal()
    {
        if (CurrentGoal == null)
        {
            CurrentGoal = Instantiate(GoalPrefab, GetRandomPosition(GoalSpawnAreaMin, GoalSpawnAreaMax), Quaternion.identity);
        }

        return CurrentGoal.GetComponent<PhysicsCollider>();
    }
    public override void OnEpisodeBegin()
    {
        ResetPlane();
        timer = 0f;
        transform.position = GetRandomPosition(SpawnAreaMin, SpawnAreaMax);
        transform.rotation = GetRandomRotation();

        CurrentGoal.transform.position = GetRandomPosition(GoalSpawnAreaMin, GoalSpawnAreaMax);
    }

    public void OnGoalCollected()
    {
        CurrentGoal.transform.position = GetRandomPosition(GoalSpawnAreaMin, GoalSpawnAreaMax);
        AddReward(5f);
        timer = 0f;
    }

    public void OnCrash()
    {
        EndEpisode();
        ResetPlane();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        timer += Time.deltaTime;

        float thrust = actions.ContinuousActions[0] + actions.ContinuousActions[1];
        float pitch = actions.ContinuousActions[2] + actions.ContinuousActions[3];
        float roll = actions.ContinuousActions[4] + actions.ContinuousActions[5];
        float yaw = actions.ContinuousActions[6] + actions.ContinuousActions[7];

        controller.SetInput(pitch, yaw, roll, thrust);

        if (isFlying)
        {
            AddReward(0.01f);
        }

        ApplyTimePenalty();
        RewardOnPathToGoal();
        CheckOutOfRange();
    }

    public override void Heuristic(in ActionBuffers actions)
    {
        ActionSegment<float> continousAction = actions.ContinuousActions;

        continousAction[0] = Input.GetKey(KeyCode.W) ? 1 : 0;
        continousAction[1] = Input.GetKey(KeyCode.S) ? -1 : 0;
        continousAction[2] = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
        continousAction[3] = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
        continousAction[4] = Input.GetKey(KeyCode.A) ? 1 : 0;
        continousAction[5] = Input.GetKey(KeyCode.D) ? -1 : 0;
        continousAction[6] = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
        continousAction[7] = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ML prefers values between 0-1 this is my attempt at doing all that
        Quaternion rotation = transform.rotation;
        Vector3 normalizedRotation = rotation.eulerAngles / 360.0f;

        float normalizedThrust = controller.thrustPercentage / 100f;

        Vector3 goalRelativePosition = CurrentGoal.transform.position - transform.position;
        goalRelativePosition.Normalize();
        float distance = goalRelativePosition.magnitude;
        goalRelativePosition = goalRelativePosition * Mathf.Clamp01(distance / MaxDistance);

        sensor.AddObservation(transform.position);
        sensor.AddObservation(root.velocity);
        sensor.AddObservation(normalizedRotation);
        sensor.AddObservation(root.angularVelocity);
        sensor.AddObservation(root.acceleration);
        sensor.AddObservation(normalizedThrust);
        sensor.AddObservation(goalRelativePosition);
    }

    public void RewardOnPathToGoal()
    {
        Vector3 toGoal = CurrentGoal.transform.position - transform.position;
        toGoal.Normalize();

        Vector3 forwardDirection = transform.forward;

        float dotProduct = Vector3.Dot(toGoal, forwardDirection);

        float alignmentReward = Mathf.Clamp01(dotProduct);
        AddReward(alignmentReward * 0.05f);

    }

    public void ApplyTimePenalty()
    {
        if (timer < maxTime)
        {
            float timePenalty = timer * 0.01f;
            AddReward(-timePenalty);
        }
    }

    private void CheckOutOfRange()
    {
        Vector3 position = transform.position;

        // Trigger the reward/episode only if outside the bounds
        if (position.x < MinBounds.x || position.x > MaxBounds.x ||
            position.y < MinBounds.y || position.y > MaxBounds.y ||
            position.z < MinBounds.z || position.z > MaxBounds.z)
        {
            AddReward(-0.5f);
            EndEpisode();
            ResetPlane();
        }
    }

    public void ResetPlane()
    {

        root.velocity = Vector3.zero;
        root.angularVelocity = Vector3.zero;
        root.accumulatedForces = Vector3.zero;
        root.torque = Vector3.zero;

        controller.thrustPercentage = 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw a wireframe cube for the bounds
        Gizmos.color = Color.red; // Choose a color for the cube (red in this case)
        //Gizmos.DrawWireCube((MinBounds + MaxBounds) / 2, MaxBounds - MinBounds); // Draw the cube at the center with the correct size
    }
#endif
}