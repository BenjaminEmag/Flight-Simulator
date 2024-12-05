using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Integrator
{
    public static void Integrate(Particle2D particle, float dt)
    {
        particle.transform.position += (particle.velocity * dt).ToVector3(0);

        particle.acceleration = particle.accumulatedForces * particle.inverseMass + particle.gravity;

        particle.velocity += particle.acceleration * dt;
        particle.velocity *= Mathf.Pow(particle.damping, dt);
    }

    public static void Integrate(Particle3D particle, float dt)
    {
        particle.transform.position += particle.velocity * dt;
        particle.acceleration = particle.accumulatedForces * particle.inverseMass + particle.gravity;
        particle.velocity += particle.acceleration * dt;
        particle.velocity *= Mathf.Pow(particle.damping, dt);

        Vector3 angularAcceleration = particle.torque * particle.inverseInertia;
        particle.angularVelocity += angularAcceleration * dt;

        if (particle.angularVelocity.sqrMagnitude > 0)
        {
            Quaternion angularDisplacement = Quaternion.AngleAxis(
                particle.angularVelocity.magnitude * Mathf.Rad2Deg * dt,
                particle.angularVelocity.normalized
            );
            particle.orientation = angularDisplacement * particle.orientation;
        }

        particle.angularVelocity *= Mathf.Pow(particle.damping, dt);
        particle.transform.rotation = particle.orientation;
    }


}
