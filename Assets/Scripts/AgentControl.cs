using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;

public class AgentControl : Agent
{
    [SerializeField] private Vector3 SpawnAreaMin = new Vector3(-5, 92, -5);
    [SerializeField] private Vector3 SpawnAreaMax = new Vector3(5, 92, 5);

    [SerializeField] private Vector3 MinBounds = new Vector3(-500, 80f, -500);
    [SerializeField] private Vector3 MaxBounds = new Vector3(500, 500f, 500);

    [SerializeField] private Vector2 RotationYRange = new Vector2(-30, 30);
    [SerializeField] private Vector2 ThrustRange = new Vector2(50, 100);

    public Particle3D root;
    public AircraftController controller;
    public GameObject runway;

    private float timer = 0f;
    private float maxTime = 30f; // Max time for episode (seconds)

    float accumReward = 0f;

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
    public int GetRandomThrust()
    {
        return (int)Random.Range(ThrustRange.x, ThrustRange.y);
    }

    public override void OnEpisodeBegin()
    {
        accumReward = 0f;
        ResetPlane();
        controller.thrustPercentage = GetRandomThrust();
        timer = Time.time + maxTime;

        transform.position = GetRandomPosition(SpawnAreaMin, SpawnAreaMax);
        transform.rotation = GetRandomRotation();
    }

    public void OnCrash()
    {
        AddReward(-1f);
        EndEpisode();
        ResetPlane();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float thrust = actions.ContinuousActions[0] + actions.ContinuousActions[1];
        float pitch = actions.ContinuousActions[2] + actions.ContinuousActions[3];
        float roll = actions.ContinuousActions[4] + actions.ContinuousActions[5];
        float yaw = actions.ContinuousActions[6] + actions.ContinuousActions[7];

        controller.SetInput(pitch, yaw, roll, thrust);

        // hard coded offset yuck
        Vector3 runwayPoint = runway.transform.position + new Vector3(0f, 0f, 155f);

        Vector3 runwayDirection = (runwayPoint - transform.position).normalized;
        float alignmentReward = Vector3.Dot(transform.forward, runwayDirection);
        float distFromRunway = transform.position.y - runway.transform.position.y;

        float heightReward = Mathf.Lerp(0f, 1f, distFromRunway);

        if (alignmentReward > 0.95f)
        {
            AddReward(alignmentReward * 0.01f);
            AddRewardd(alignmentReward * 0.01f);
        }


        ApplyTimePenalty();
        CheckOutOfRange();
        CheckLand();
    }

    private void CheckLand()
    {
        if (controller.isLanded)
        {
            AddReward(5f);
            AddRewardd(5f);

            float distFromCenter = Mathf.Abs(transform.position.x);

            float distReward = Mathf.Lerp(5f, 0f, distFromCenter / 30f);

            float alignment = Vector3.Dot(transform.forward, transform.forward);


            AddReward(distReward * alignment);
            AddRewardd(distReward * alignment);

            Debug.Log(accumReward);
            EndEpisode();
            ResetPlane();
        }
    }

    public override void Heuristic(in ActionBuffers actions)
    {
        ActionSegment<float> continousAction = actions.ContinuousActions;
        ActionSegment<int> discreteActions = actions.DiscreteActions;

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
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localRotation.eulerAngles);

        sensor.AddObservation(root.velocity);
        sensor.AddObservation(root.angularVelocity);

        Vector3 toRunway = runway.transform.position - transform.position;
        sensor.AddObservation(toRunway);
        sensor.AddObservation(Vector3.Dot(transform.forward, toRunway.normalized));

        sensor.AddObservation(transform.position.y);

        sensor.AddObservation(controller.thrust);
    }

    public void ApplyTimePenalty()
    {
        if (Time.time >= timer)
        {
            AddReward(-1f);
            AddRewardd(-1f);
            EndEpisode();
        }
    }
    private void CheckOutOfRange()
    {
        Vector3 position = transform.position;

        if (position.x < MinBounds.x || position.x > MaxBounds.x ||
            position.y < MinBounds.y || position.y > MaxBounds.y ||
            position.z < MinBounds.z || position.z > MaxBounds.z)
        {
            AddReward(-2f);
            AddRewardd(-2f);
            EndEpisode();
        }
    }

    public void ResetPlane()
    {
        controller.isLanded = false;
        controller.isOnRunway = false;

        root.velocity = Vector3.zero;
        root.angularVelocity = Vector3.zero;
        root.accumulatedForces = Vector3.zero;
        root.torque = Vector3.zero;
    }

    internal void OnCollisionEvent(PhysicsCollider envCollider)
    {
        GameObject collision = envCollider.gameObject;
        if (collision != runway)
            EndEpisode();
    }


    void AddRewardd(float amount)
    {
        accumReward += amount;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube((MinBounds + MaxBounds) / 2, MaxBounds - MinBounds);

        }

        {
            Gizmos.color = Color.blue;
            Gizmos.DrawCube((SpawnAreaMin + SpawnAreaMax) / 2, SpawnAreaMax - SpawnAreaMin);
        }
    }
#endif
}