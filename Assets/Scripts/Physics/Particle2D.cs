using UnityEngine;

public class Particle2D : MonoBehaviour
{
    public Vector2 velocity;
    public float damping;
    public Vector2 acceleration;
    public Vector2 gravity = new Vector2(0, -9.8f);
    public float inverseMass;
    public Vector2 accumulatedForces { get; private set; }

    public void FixedUpdate()
    {
        DoFixedUpdate(Time.fixedDeltaTime);
    }

    public void DoFixedUpdate(float dt)
    {
        Integrator.Integrate(this, dt);
        ClearForces();
    }

    public void ClearForces()
    {
        accumulatedForces = Vector2.zero;
    }

    public void AddForce(Vector2 force)
    {
        accumulatedForces += force;
    }
}
