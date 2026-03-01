using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TrialData
{
    public string aiMessage;      //  Text that the AI will display to the driver
    public bool shouldGoLeft;    // Correct direction for the driver to take (true for left, false for right)
    public bool aiIsLying;       // AI's honesty status for this trial (true if the AI is lying, false if it's telling the truth)
}

public class TrialManager : MonoBehaviour
{
    [Header("References")]
    public Rigidbody carRigidbody;
    public Transform playerCar;
    public TextMeshProUGUI aiDisplay;

    [Header("Experiment Settings")]
    public List<TrialData> trials; // List of trials to run in the experiment
    public float resetZ = 300f; 
    public float downForce = 10000f; // power of the downward force applied to the car

    private int currentTrialIndex = 0; // Index to keep track of the current trial

    void Start()
    {
        UpdateTrial(); // Initialize the first trial
    }
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
    // Reset the car's position to the starting point for the next trial
    playerCar.position = new Vector3(playerCar.position.x, 0.5f, 0f);
    
    // Stop any existing movement
    carRigidbody.linearVelocity = Vector3.zero;
    carRigidbody.angularVelocity = Vector3.zero;

    // Move to the next trial
    currentTrialIndex++;

     // If we've reached the end of the trials list, we can choose to loop back to the first trial or simply stop updating.
    if (currentTrialIndex >= trials.Count)
    {
        if (aiDisplay != null) aiDisplay.text = "EXPERIMENT COMPLETE\nENGINE STOPPED";
        Debug.Log("Experiment Finished. Car Locked.");

        // Kinematic mode keeps the car stationary and unaffected by physics
        carRigidbody.isKinematic = true; 

        // Disable this script to prevent further updates
        this.enabled = false;
        return; 
    }
    // Update for the next trial
    UpdateTrial();
}

    void UpdateTrial()
    {
        if (aiDisplay != null && currentTrialIndex < trials.Count)
        {
            aiDisplay.text = trials[currentTrialIndex].aiMessage;
        }
    }
}