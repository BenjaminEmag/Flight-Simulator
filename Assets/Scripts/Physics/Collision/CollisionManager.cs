using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    private class AirplaneColliders
    {
        public PhysicsCollider RootCollider { get; set; }
        public List<PhysicsCollider> PlaneParts { get; set; } = new List<PhysicsCollider>();
        public List<PhysicsCollider> CriticalParts { get; set; } = new List<PhysicsCollider>();
    }

    private readonly List<AirplaneColliders> airplanes = new List<AirplaneColliders>();
    private readonly List<PhysicsCollider> environmentColliders = new List<PhysicsCollider>();
    private readonly List<PhysicsCollider> goalColliders = new List<PhysicsCollider>();

    private void Awake()
    {
        InitializeEnvironmentColliders("Environment");
        InitializeGoalColliders("Goal");
        InitializeAirplanes();
    }

    private void FixedUpdate()
    {

        foreach (var airplane in airplanes)
        {
            if (airplane.RootCollider.transform.position.y < 95f)
            {
                ResolveAirplaneCollisions(airplane);
                CheckCriticalPartCollisions(airplane);
            }
        }
    }

    private void InitializeEnvironmentColliders(string tag)
    {
        environmentColliders.AddRange(
            GameObject.FindGameObjectsWithTag(tag)
                      .Select(obj => obj.GetComponent<PhysicsCollider>())
                      .Where(collider => collider != null)
        );
    }

    private void InitializeGoalColliders(string tag)
    {
        goalColliders.AddRange(
            GameObject.FindGameObjectsWithTag(tag)
                      .Select(obj => obj.GetComponent<PhysicsCollider>())
                      .Where(collider => collider != null)
        );
    }

    private void InitializeAirplanes()
    {
        foreach (var rootObj in GameObject.FindGameObjectsWithTag("PlaneRoot"))
        {
            var rootCollider = rootObj.GetComponent<PhysicsCollider>();
            if (rootCollider == null || airplanes.Any(a => a.RootCollider == rootCollider))
                continue;

            var airplane = new AirplaneColliders
            {
                RootCollider = rootCollider,
                PlaneParts = GetChildColliders(rootObj, "PlanePart"),
                CriticalParts = GetChildColliders(rootObj, "CriticalPlanePart"),
            };

            airplanes.Add(airplane);
        }
    }

    private List<PhysicsCollider> GetChildColliders(GameObject parent, string tag)
    {
        return GameObject.FindGameObjectsWithTag(tag)
                         .Where(obj => obj.transform.IsChildOf(parent.transform))
                         .Select(obj => obj.GetComponent<PhysicsCollider>())
                         .Where(collider => collider != null)
                         .ToList();
    }
    private void ResolveAirplaneCollisions(AirplaneColliders airplane)
    {
        Vector3 totalForce = Vector3.zero;
        float maxPenetration = 0f;
        Vector3 torqueAtCoM = Vector3.zero;
        var centerOfMass = airplane.RootCollider.GetComponent<Particle3D>().centerOfMass;

        foreach (var part in airplane.PlaneParts)
        {
            foreach (var envCollider in environmentColliders)
            {
                var collisionInfo = CollisionDetection.GetCollisionInfo(part, envCollider);
                if (!collisionInfo.IsColliding) continue;

                totalForce += collisionInfo.normal * collisionInfo.penetration;
                maxPenetration = Mathf.Max(maxPenetration, collisionInfo.penetration);

                var displacement = (part.transform.position - collisionInfo.normal * collisionInfo.penetration) - centerOfMass;
                var angularVelocity = airplane.RootCollider.GetComponent<Particle3D>().angularVelocity;
                var relativeVelocity = Vector3.Cross(angularVelocity, displacement);

                torqueAtCoM += Vector3.Cross(displacement, collisionInfo.normal * (collisionInfo.penetration + relativeVelocity.magnitude));

                var agentControl = airplane.RootCollider.GetComponent<AgentControl>();
                agentControl?.OnCollisionEvent(envCollider);
            }
        }

        if (maxPenetration > 0f)
        {
            CollisionDetection.ApplyCollisionResolution(airplane.RootCollider, totalForce.normalized, torqueAtCoM, maxPenetration);
        }
    }

    private void CheckCriticalPartCollisions(AirplaneColliders airplane)
    {
        foreach (var criticalPart in airplane.CriticalParts)
        {
            foreach (var envCollider in environmentColliders)
            {
                if (CollisionDetection.GetCollisionInfo(criticalPart, envCollider).IsColliding)
                {
                    ResetAirplane(airplane);
                    airplane.RootCollider.GetComponent<AgentControl>()?.OnCrash();
                    return;
                }
            }
        }
    }

    private void ResetAirplane(AirplaneColliders airplane)
    {
        var rootTransform = airplane.RootCollider.transform;
        rootTransform.SetPositionAndRotation(new Vector3(0f, 91.5f, 0f), Quaternion.identity);

        var particle = airplane.RootCollider.GetComponent<Particle3D>();
        if (particle != null)
        {
            particle.velocity = Vector3.zero;
            particle.angularVelocity = Vector3.zero;
            particle.accumulatedForces = Vector3.zero;
            particle.torque = Vector3.zero;
        }

        airplane.RootCollider.GetComponent<AircraftController>().thrustPercentage = 0f;
    }
}
