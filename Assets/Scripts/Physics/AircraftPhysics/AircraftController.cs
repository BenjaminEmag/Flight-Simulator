using Klareh;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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

    private float pitchInput = 0f;
    private float yawInput = 0f;
    private float rollInput = 0f;
    private float thrustInput = 0f;

    public bool isOnRunway = false;
    public bool isLanded = false;

    public TMP_Text thrustText;
    public TMP_Text HeightText;

    [SerializeField] private Vector3 minRunway;
    [SerializeField] private Vector3 MaxRunway;

    private Vector3 minaAiAreaForLand = new Vector3(-100, 150, -450);
    private Vector3 maxaAiAreaForLand = new Vector3(100, 250, -500);

    private float[] actions = new float[8];

    [SerializeField] InsepectableDictionary Dictionary;
    private Dictionary<keys, Image> uiKeys;

    private Landing landingAi;
    private bool isAiLanding = true;

    private void Start()
    {
        zeroController = GetComponentInChildren<ZeroController>();
        rootParticle = GetComponent<Particle3D>();
        landingAi = GetComponent<Landing>();
        zeroController.thrustPercentage = thrustPercentage;

        uiKeys = Dictionary.ToDictionary();
    }

    private void FixedUpdate()
    {
        if (!isAiLanding)
            GetControls();

        ApplyControls();
        ApplyAerodynamics();
        ApplyForces();
        UpdateUI();
        ApplyBrakes();
        totalForce = Vector3.zero;
        totalTorque = Vector3.zero;
        zeroController.thrustPercentage = thrustPercentage;
    }

    private void GetControls()
    {
        actions[0] = Input.GetKey(KeyCode.W) ? 1 : 0;
        actions[1] = Input.GetKey(KeyCode.S) ? -1 : 0;
        actions[2] = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
        actions[3] = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
        actions[4] = Input.GetKey(KeyCode.A) ? 1 : 0;
        actions[5] = Input.GetKey(KeyCode.D) ? -1 : 0;
        actions[6] = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
        actions[7] = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;

        thrustInput = actions[0] + actions[1];
        pitchInput = actions[2] + actions[3];
        rollInput = actions[4] + actions[5];
        yawInput = actions[6] + actions[7];
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

    public void SetInput(float pitch, float yaw, float roll, float thrust)
    {

        foreach (KeyValuePair<keys, Image> entry in uiKeys)
        {
            Color newColor = entry.Value.color;

            switch (entry.Key)
            {
                case keys.W:
                    newColor.a = (thrust < 0) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.S:
                    newColor.a = (thrust > 0) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.UP:
                    newColor.a = (pitch > 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.Down:
                    newColor.a = (pitch < 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.A:
                    newColor.a = (roll > 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.D:
                    newColor.a = (roll < 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.Left:
                    newColor.a = (yaw < 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
                case keys.Right:
                    newColor.a = (yaw > 0f) ? 0.5f : 1f;
                    entry.Value.color = newColor;
                    break;
            }
        }

        pitchInput = pitch;
        yawInput = yaw;
        rollInput = roll;
        thrustInput = thrust;
    }
    private void ApplyBrakes()
    {
        if (transform.position.x >= minRunway.x && transform.position.x <= MaxRunway.x &&
            transform.position.y >= minRunway.y && transform.position.y <= MaxRunway.y &&
            transform.position.z >= minRunway.z && transform.position.z <= MaxRunway.z)
        {
            isOnRunway = true;
            rootParticle.damping = 0.2f;

            thrustPercentage = 0f;

            isAiLanding = false;
            if (rootParticle.velocity.sqrMagnitude < 0.2f)
            {
                isLanded = true;
                rootParticle.velocity = Vector3.zero;
                rootParticle.acceleration = Vector3.zero;
            }
            return;
        }

        rootParticle.damping = 0.9f;
    }

    private void OnDrawGizmos()
    {
        {
            Gizmos.color = Color.red;
            // Gizmos.DrawWireCube((minaAiAreaForLand + maxaAiAreaForLand) / 2, maxaAiAreaForLand - minaAiAreaForLand);

        }
    }

}

