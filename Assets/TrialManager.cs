using UnityEngine;
using TMPro;

public class TrialManager : MonoBehaviour
{
    [Header("References")]
    public Rigidbody carRigidbody;
    public Transform playerCar;
    public TextMeshProUGUI aiDisplay;

    [Header("Settings")]
    public float resetZ = 300f; 
    public float downForce = 10000f; // power of the downward force applied to the car

    private int trialCount = 1;

    void FixedUpdate()
    {
        // power of the downward force applied to the car, to keep it grounded at high speeds
        // using playerCar.up to apply the force downwards
        carRigidbody.AddForce(-playerCar.up * downForce);

    }

    void Update()
    {
        // if the car's Z position exceeds the reset threshold, perform a reset
        if (playerCar.position.z >= resetZ)
        {
            PerformReset();
        }
    }

    void PerformReset()
{
    trialCount++;

    // for the reset, we set the car's position to Z=0 and Y=-1.6
    playerCar.position = new Vector3(playerCar.position.x, -1.6f, 0f);

    // zero out the car's velocity to prevent it from carrying over any momentum from the previous trial
    carRigidbody.linearVelocity = Vector3.zero;
    carRigidbody.angularVelocity = Vector3.zero;

    Debug.Log("Trial " + trialCount + " started! Reset to Z=0 and Y=-1.6");
}

    void UpdateUI()
    {
        if (aiDisplay != null)
        {
            aiDisplay.text = "TRIAL " + trialCount + "\nSYSTEM STABLE";
        }
    }
}