using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    private List<PhysicsCollider> planePartColliders = new List<PhysicsCollider>();
    private List<PhysicsCollider> environmentColliders = new List<PhysicsCollider>();
    private List<PhysicsCollider> criticalPlanePart = new List<PhysicsCollider>();
    private PhysicsCollider rootCollider;

    Vector3 startPos = new Vector3(0f, 91.5f, 0f);
    private void Awake()
    {
        rootCollider = GameObject.FindGameObjectWithTag("PlaneRoot").GetComponent<PhysicsCollider>();

        // Gather colliders for all parts
        foreach (var obj in GameObject.FindGameObjectsWithTag("PlanePart"))
        {
            if (obj.TryGetComponent(out PhysicsCollider collider))
            {
                planePartColliders.Add(collider);
            }
        }

        foreach (var obj in GameObject.FindGameObjectsWithTag("Environment"))
        {
            if (obj.TryGetComponent(out PhysicsCollider collider))
            {
                environmentColliders.Add(collider);
            }
        }

        foreach (var obj in GameObject.FindGameObjectsWithTag("CriticalPlanePart"))
        {
            if (obj.TryGetComponent(out PhysicsCollider collider))
            {
                criticalPlanePart.Add(collider);
            }
        }

    }

    private void FixedUpdate()
    {
        AggregateAndResolveCollisions();
        CheckCritPlane();
    }
    private void AggregateAndResolveCollisions()
    {
        Vector3 totalNormal = Vector3.zero;
        float maxPenetration = 0f;
        Vector3 torqueAtCoM = Vector3.zero;
        Vector3 objectCenterOfMass = rootCollider.GetComponent<Particle3D>().centerOfMass;

        foreach (var part in planePartColliders)
        {
            foreach (var envCollider in environmentColliders)
            {
                var collisionInfo = CollisionDetection.GetCollisionInfo(part, envCollider);
                if (collisionInfo.IsColliding)
                {
                    
                    Vector3 resolutionForce = collisionInfo.normal * collisionInfo.penetration;
                    totalNormal += resolutionForce;

                    maxPenetration = Mathf.Max(maxPenetration, collisionInfo.penetration);

                    Vector3 partPosition = part.transform.position;
                    Vector3 contactPoint = partPosition - collisionInfo.normal * collisionInfo.penetration;

                    Vector3 displacement = contactPoint - objectCenterOfMass;

                    Vector3 angularVelocity = rootCollider.GetComponent<Particle3D>().angularVelocity;

                    Vector3 relativeVelocityAtCollisionPoint = Vector3.Cross(angularVelocity, displacement);
                    torqueAtCoM += Vector3.Cross(displacement, collisionInfo.normal * (collisionInfo.penetration + relativeVelocityAtCollisionPoint.magnitude));
                }
            }
        }

        if (maxPenetration > 0f)
        {
            Vector3 averageNormal = totalNormal.normalized;
            CollisionDetection.ApplyCollisionResolution(rootCollider, averageNormal, torqueAtCoM, maxPenetration);
        }
    }

    private void CheckCritPlane()
    {
        foreach (var env in environmentColliders)
        {
            foreach (var crit in criticalPlanePart)
            {
                var collisionInfo = CollisionDetection.GetCollisionInfo(env, crit);
                if (collisionInfo.IsColliding)
                {
                    ResetPlane();
                }
            }
        }
    }

    private void ResetPlane()
    {
        rootCollider.transform.SetPositionAndRotation(startPos, Quaternion.identity);
        Particle3D particle = rootCollider.GetComponent<Particle3D>();
        if (particle != null)
        {
            particle.velocity = Vector3.zero;
            particle.angularVelocity = Vector3.zero;
            particle.accumulatedForces = Vector3.zero;
            particle.torque = Vector3.zero;
        }

        rootCollider.GetComponent<AircraftController>().thrustPercentage = 0f;
    }

}
