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
    public float brakeForce = 1f;

    [Header("Aerodynamics")]
    public float liftCoefficient = 0.5f;
    public float dragCoefficient = 0.02f;

    private float pitchInput = 0f;
    private float yawInput = 0f;
    private float rollInput = 0f;
    private float thrustInput = 0f;
    private bool braking = false;

    public bool isOnRunway = false;
    public bool isLanded = false;


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
        ApplyControls();

        ApplyAerodynamics();
        ApplyForces();
        UpdateUI();

        totalForce = Vector3.zero;
        totalTorque = Vector3.zero;
        zeroController.thrustPercentage = thrustPercentage;
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

    public void ApplyControls()
    {
        totalTorque = Vector3.zero;
        totalTorque += transform.right * pitchInput * pitchTorque;
        totalTorque += transform.up * yawInput * yawTorque;
        totalTorque += transform.forward * rollTorque * rollInput;

        thrustPercentage = Mathf.Clamp(thrustPercentage + thrustInput, 0f, 100f);
        float thrustCoefficent = (thrustPercentage / 100f);
        totalForce += transform.forward * thrustCoefficent * thrust;
    }

    public void SetInput(float pitch, float yaw, float roll, float thrust, bool brake)
    {
        pitchInput = pitch;
        yawInput = yaw;
        rollInput = roll;
        thrustInput = thrust;
        braking = brake && isOnRunway;
    }
    private void ApplyBrakes()
    {
        if (!isOnRunway || !braking) return;

        Vector3 velocity = rootParticle.velocity;

        Vector3 brakeForceVector = -velocity.normalized * brakeForce;

        totalForce += brakeForceVector;

        if (velocity.magnitude < 1f)
        {
            rootParticle.velocity = Vector3.zero;
            braking = false;
            isLanded = true;
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
