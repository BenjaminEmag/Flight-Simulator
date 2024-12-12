using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.UI;

public class Landing : Agent
{
    public Particle3D root;
    public AircraftController controller;
    public GameObject runway;

    private Vector3 SpawnAreaMin = new Vector3(-100, 150, -450);
    private Vector3 SpawnAreaMax = new Vector3(100, 250, -500);

    public bool shouldStartRandom = true;

    public void Start()
    {
        if (!shouldStartRandom)
            return; 

        transform.position = new Vector3(
            Random.Range(SpawnAreaMin.x, SpawnAreaMax.x),
            Random.Range(SpawnAreaMin.y, SpawnAreaMax.y),
            Random.Range(SpawnAreaMin.z, SpawnAreaMax.z)
        );
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        float thrust = actions.ContinuousActions[0] + actions.ContinuousActions[1];
        float pitch = actions.ContinuousActions[2] + actions.ContinuousActions[3];
        float roll = actions.ContinuousActions[4] + actions.ContinuousActions[5];
        float yaw = actions.ContinuousActions[6] + actions.ContinuousActions[7];

        controller.SetInput(pitch, yaw, roll, thrust);

        CheckLand();
    }

    private void CheckLand()
    {
        if (controller.isOnRunway)
        {
            EndEpisode();
            this.enabled = false;
        }
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

}
