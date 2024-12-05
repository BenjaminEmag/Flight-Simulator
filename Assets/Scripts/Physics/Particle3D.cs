using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Particle3D : MonoBehaviour
{
    public Vector3 velocity;
    public float damping = 1f;
    public Vector3 acceleration;
    public Vector3 gravity = new Vector3(0, 0, 0);
    public float inverseMass = 1f;
    public Vector3 accumulatedForces { get; set; }

    public Vector3 angularVelocity;
    public Vector3 torque;
    public Quaternion orientation = Quaternion.identity;
    public float inertia = 1f;
    public float inverseInertia => inertia > 0 ? 1f / inertia : 0f;

    public Vector3 centerOfMass = Vector3.zero;
    public Vector3 centerOfMassOffset = Vector3.zero;

    public void Awake()
    {
        centerOfMass = transform.position + centerOfMassOffset;
        orientation = transform.rotation;
    }

    public void FixedUpdate()
    {
        centerOfMass = transform.position + transform.rotation * centerOfMassOffset;
        orientation = transform.rotation;
        Integrator.Integrate(this, Time.fixedDeltaTime);
        ClearForces();
    }

    public void ClearForces()
    {
        accumulatedForces = Vector3.zero;
        torque = Vector3.zero;
    }

    public void AddForce(Vector3 force)
    {
        accumulatedForces += force;
    }

    public void AddTorque(Vector3 appliedTorque)
    {
        torque += appliedTorque;
    }
}

