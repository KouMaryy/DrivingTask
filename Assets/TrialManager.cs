using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TrialData
{
    public string aiMessage;      //  Text that the AI will display to the driver
    public bool shouldGoLeft;    // Correct direction for the driver to take (true for left, false for right)
    public bool aiIsLying;       // AI's honesty status for this trial (true if the AI is lying, false if it's telling the truth)
    [Range(0, 1)]
    public float weatherIntensity; // 0 = Clear, 1 = Heavy (used to control the intensity of weather effects in the trial)
}

public class TrialManager : MonoBehaviour
{
    [Header("References")]
    public CarController carController;
    public Rigidbody carRigidbody;
    public Transform playerCar;
    public TextMeshProUGUI aiDisplay;

    [Header("Experiment Settings")]
    public List<TrialData> trials; // List of trials to run in the experiment
    public float resetZ = 320f;
    public float downForce = 10000f; // power of the downward force applied to the car
    public float triggerZ = 260f; // obstacle position
    public float displayDistance = 150f; // distance at which the AI message will be displayed to the driver
    private bool messageDisplayed = false; // flag to ensure the AI message is displayed only once per trial
    private bool obstaclePassed = false; // flag to track if the car has passed the obstacle for the current trial

    private int currentTrialIndex = 0; // Index to keep track of the current trial

    [Header("Weather Settings")]
    public GameObject weatherObject;
    private ParticleSystem snowParticles;

    void Start()
    {
        if (weatherObject != null)
        {
            // Get the component even if the object is inactive
            snowParticles = weatherObject.GetComponentInChildren<ParticleSystem>();
            // Ensure the weather object is inactive at the start of the experiment
            if (snowParticles != null)
            {
                snowParticles.Stop();
                snowParticles.Clear();
            }
        }
        //Ensure that the scene starts with clear weather
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0;

        // Initialize the first trial 
        UpdateTrial();
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
        // Check if the car is within the range to display the AI message for the current trial
        if (!messageDisplayed && playerCar.position.z >= (triggerZ - displayDistance) && playerCar.position.z < triggerZ)
        {
            ShowAiMessage();
        }

        //
        if (!obstaclePassed && playerCar.position.z >= triggerZ)
        {
            ShowPostObstacleMessage();
        }
    }

    void ShowAiMessage()
    {
        if (aiDisplay != null && currentTrialIndex < trials.Count)
        {
            TrialData currentTrial = trials[currentTrialIndex];
            aiDisplay.text = trials[currentTrialIndex].aiMessage;

            // calculate the lane suggestion based on the current trial's shouldGoLeft value and the AI's honesty status
            // If shouldGoLeft is true, aiSuggestedLane = -1, otherwise aiSuggestedLane = 1
            int aiSuggestedLane = currentTrial.shouldGoLeft ? -1 : 1;
            if (currentTrial.aiIsLying)
            {
                aiSuggestedLane *= -1; // If the AI is lying, we invert the suggested lane.
            }

            // Find the current lane of the car based on its x position
            int currentLane;
            if (playerCar.position.x < -2f) currentLane = -1; // Left
            else currentLane = 1; // Right

            if (currentLane == aiSuggestedLane)
            {
                // car is already in the AI suggested lane, no need to change lanes
                aiDisplay.text = "SAFE LANE MAINTAINED";
                aiDisplay.color = Color.cyan; // Change text color to cyan for a positive message
                Debug.Log("AI: Car already in suggested lane. No steering needed.");
            }
            else
            {
                // car is not in the AI suggested lane, so we will command it to change lanes
                aiDisplay.text = "DANGER DETECTED\nSWITCHING LANE NOW";
                aiDisplay.color = new Color(1f, 0.5f, 0f); // Change text color to orange for ΑΙ activeintervention
                carController.SetTargetLane(aiSuggestedLane);
                Debug.Log("AI Intervention: Switching to Lane " + aiSuggestedLane);
            }

            messageDisplayed = true;
        }
    }

    void ShowPostObstacleMessage()
    {
        if (aiDisplay != null)
        {
            aiDisplay.text = "Consequences";
            aiDisplay.color = Color.white;
            obstaclePassed = true;
            Debug.Log("Car passed the obstacle point.");
        }
    }

    void PerformReset()
    {
        TrialData currentTrial = trials[currentTrialIndex];
        bool intervened = carController.didPlayerIntervene; // Check if the player intervened during the trial
        Debug.Log("Trial " + currentTrialIndex + " Did Player Intervene ? " + intervened);

        // Here we will add the code that will write to the .csv file
        // Example: SaveToCSV(currentTrialIndex, intervened, trials[currentTrialIndex].aiIsLying);

        // Reset the intervention flag for the next trial
        carController.ResetIntervention();

        // Reset the car's position to the starting point for the next trial
        playerCar.position = new Vector3(playerCar.position.x, -1.65f, 0f);

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
            playerCar.position = new Vector3(0f, 0f, 0f);

            // Lock the car by disabling movement 
            if (carController != null) carController.canMove = false;

            // kinematic mode to prevent any further physics interactions
            carRigidbody.linearVelocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;
            carRigidbody.isKinematic = true;

            // Disable this script to stop any further updates
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
            TrialData currentTrial = trials[currentTrialIndex];
            float intensity = currentTrial.weatherIntensity;

            // Fog settings based on the trial's weather intensity
            RenderSettings.fog = (intensity > 0);
            RenderSettings.fogDensity = intensity * 0.02f;

            // Snow particle settings based on the trial's weather intensity
            if (weatherObject != null && snowParticles != null)
            {
                if (intensity > 0)
                {
                    // Activate the weather object and set the particle emission rate based on intensity
                    Debug.Log("Trial " + currentTrialIndex + ": Weather starting now.");
                    weatherObject.SetActive(true);
                    if (!snowParticles.isPlaying) snowParticles.Play();

                    var emission = snowParticles.emission;
                    emission.rateOverTime = intensity * 2000f;
                }
                else
                {
                    Debug.Log("Trial " + currentTrialIndex + ": Weather clearing now.");
                    // Clear the weather object when intensity is 0
                    snowParticles.Stop();
                    snowParticles.Clear();
                    weatherObject.SetActive(false);
                }
            }


            // Reset the AI display to a default message for the next trial
            aiDisplay.text = "SAFE";
            aiDisplay.color = Color.white;

            // Reset flags for the new trial
            messageDisplayed = false;
            obstaclePassed = false;
        }
    }
}