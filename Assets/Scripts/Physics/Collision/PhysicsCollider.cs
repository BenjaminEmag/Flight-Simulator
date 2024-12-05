using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsCollider : MonoBehaviour
{
    public enum Shape
    {
        Sphere,
        Plane,
        AABB,
        OBB,
        Count
    }

    public abstract Shape shape { get; }

    public float invMass
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.inverseMass;
            }
            return 0;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.inverseMass = value;
            }
        }
    }

    public Vector3 velocity
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.velocity;
            }
            return Vector3.zero;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.velocity = value;
            }
        }
    }

    public Vector3 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public Quaternion rotation
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.orientation;
            }
            return transform.rotation;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.orientation = value;
            }
        }
    }

    public Vector3 angularVelocity
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.angularVelocity;
            }
            return Vector3.zero;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.angularVelocity = value;
            }
        }
    }

    public float invInertia
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.inverseInertia;
            }
            return 0f;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.inertia = value;
            }
        }
    }

    public Vector3 centerOfMass
    {
        get
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                return particle.centerOfMass;
            }
            return Vector3.zero;
        }
        set
        {
            Particle3D particle;
            if (TryGetComponent(out particle))
            {
                particle.centerOfMass = value;
            }
        }

    }
}
