using Klareh;
using TMPro;
using UnityEngine;

public class AircraftController : MonoBehaviour
{
    private Particle3D rootParticle;
    public ZeroController zeroController;

    private Vector3 totalTorque = Vector3.zero;
    private Vector3 totalForce = Vector3.zero;

    [Header("Aircraft Settings")]
    public float thrustPercentage = 0f;
    public float thrust = 100f;
    public float pitchTorque = 2f;
    public float rollTorque = 2f;
    public float yawTorque = 1f;

    [Header("Aerodynamics")]
    public float liftCoefficient = 0.5f;
    public float dragCoefficient = 0.02f;


    public TMP_Text thrustText;
    public TMP_Text HeightText;

    private void Awake()
    {
        zeroController = GetComponentInChildren<ZeroController>();
        rootParticle = GetComponent<Particle3D>();
        zeroController.thrustPercentage = thrustPercentage;
    }

    private void FixedUpdate()
    {
        HandleInput();
        ApplyAerodynamics();
        ApplyForces();

        UpdateUI();

        totalForce = Vector3.zero;
        totalTorque = Vector3.zero;
    }

    private void HandleInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            thrustPercentage = Mathf.Min(thrustPercentage + 1f, 100f);
            zeroController.thrustPercentage = thrustPercentage;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            thrustPercentage = Mathf.Max(thrustPercentage - 1f, 0f);
            zeroController.thrustPercentage = thrustPercentage;
        }

        float thrustCoefficent = (thrustPercentage / 100f) * thrust;

        totalForce += transform.forward * thrustCoefficent;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            totalTorque += transform.right * pitchTorque;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            totalTorque -= transform.right * pitchTorque;
        }

        if (Input.GetKey(KeyCode.A))
        {
            totalTorque += transform.forward * rollTorque;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            totalTorque -= transform.forward * rollTorque;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            totalTorque -= transform.up * yawTorque;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            totalTorque += transform.up * yawTorque;
        }
    }

    private void ApplyAerodynamics()
    {
        Vector3 velocity = rootParticle.velocity;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);

        float lift = liftCoefficient * Mathf.Max(forwardSpeed, 0);
        Vector3 liftVector = transform.up * lift;

        Vector3 drag = -dragCoefficient * velocity.magnitude * velocity;

        totalForce += liftVector + drag;
    }

    private void ApplyForces()
    {
        rootParticle.AddForce(totalForce);
        rootParticle.AddTorque(totalTorque);

        rootParticle.angularVelocity *= 0.99f;
    }

    private void UpdateUI()
    {
        if (thrustText != null)
        {
            thrustText.text = $"Thrust: {thrustPercentage:F0}%";
        }

        if (HeightText != null)
        {
            HeightText.text = $"Height: {rootParticle.transform.position.y - 91.5f:F0}";
        }

    }

    private void OnDrawGizmos()
    {
        if (rootParticle == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rootParticle.centerOfMass, 0.2f);

        if (totalForce != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rootParticle.centerOfMass, rootParticle.centerOfMass + totalForce.normalized * 2f);
            Gizmos.DrawWireSphere(rootParticle.centerOfMass + totalForce.normalized * 2f, 0.1f);
        }

        if (totalTorque != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(rootParticle.centerOfMass, rootParticle.centerOfMass + totalTorque.normalized * 2f);
            Gizmos.DrawWireSphere(rootParticle.centerOfMass + totalTorque.normalized * 2f, 0.1f);
        }
    }
}
