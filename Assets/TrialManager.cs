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

[System.Serializable]
public class ObstaclePair
{
    public string pairName;
    public GameObject solidPrefab;
    public GameObject fragilePrefab;
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
    private bool hasCrashedThisTrial = false;

    [Header("Weather Settings")]
    public GameObject weatherObject;
    private ParticleSystem snowParticles;

    [Header("Obstacle Spawning")]
    public GameObject solidPrefab;  // Solid obstacle prefab (e.g., a concrete barrier)
    public GameObject fragilePrefab; // Fragile obstacle prefab (e.g., a cardboard box)
    public float obstacleZ = 260f;   // The distance at which the obstacles will appear (same as triggerZ)

    private GameObject activeLeftObstacle;
    private GameObject activeRightObstacle;

    [Header("Data Logging")]
    public string participantID = "P01"; // Unique identifier for the participant, should be set from the Unity Inspector before each participant starts the experiment

    // Data for CSV : Variables for reaction time measurement and player intervention tracking
    private float messageStartTime;
    private float firstInterventionTime;
    private bool reactionRecorded = false;
    private string currentAiAction = "Maintain";

    // List of obstacle pairs to choose from for each trial
    public List<ObstaclePair> obstaclePool;
    public int obstaclePairIndex; // Index to keep track of which obstacle pair is currently active

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

        //Reaction Recording: If the AI has displayed a message and the player has intervened for the first time
        if (messageDisplayed && !obstaclePassed && !reactionRecorded && carController.didPlayerIntervene)
        {
            firstInterventionTime = Time.time;
            reactionRecorded = true;
            Debug.Log("Reaction Recorded: " + (firstInterventionTime - messageStartTime) + "s");
        }

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

            // Start measuring reaction time from the moment the AI message is displayed
            messageStartTime = Time.time;
            reactionRecorded = false;
            carController.ResetIntervention(); // Reset the intervention flag at the start of the trial

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
                currentAiAction = "Maintain";
                aiDisplay.color = Color.cyan; // Change text color to cyan for a positive message
                Debug.Log("AI: Car already in suggested lane. No steering needed.");
            }
            else
            {
                // car is not in the AI suggested lane, so we will command it to change lanes
                aiDisplay.text = "DANGER DETECTED\nSWITCHING LANE NOW";
                currentAiAction = "Switch";
                aiDisplay.color = new Color(1f, 0.5f, 0f); // Change text color to orange for ΑΙ activeintervention
                carController.SetTargetLane(aiSuggestedLane);
                Debug.Log("AI Intervention: Switching to Lane " + aiSuggestedLane);
            }

            messageDisplayed = true;
        }
    }

    public bool IsAIPresentlyActive()
    {
        //Override message should only be displayed if the AI has spoken and the car has not yet passed the obstacle
        return messageDisplayed && !obstaclePassed;
    }

    void SpawnObstacles()
    {
        // 1. Clear existing obstacles if they exist before spawning new ones for the current trial
        if (activeLeftObstacle != null) Destroy(activeLeftObstacle);
        if (activeRightObstacle != null) Destroy(activeRightObstacle);

        TrialData currentTrial = trials[currentTrialIndex];

        // 2. Positions for the left and right obstacles based on the obstacleZ position and lane distance
        Vector3 leftPos = new Vector3(-6f, 7.5f, obstacleZ);
        Vector3 rightPos = new Vector3(6f, 7.5f, obstacleZ);

        // Create a random rotation for the obstacles to add visual variety
        float[] rotations = { 0f, 45f, 90f, 135f };
        float randomY = rotations[Random.Range(0, rotations.Length)];
        Quaternion randomRotation = Quaternion.Euler(0, randomY, 0);

        // 3. Spawn the solid and fragile obstacles based on the current trial's shouldGoLeft value
        if (currentTrial.shouldGoLeft)
        {
            // shouldGoLeft is true, Left = fragile (Safe), Right = Solid (Danger)
            activeLeftObstacle = Instantiate(fragilePrefab, leftPos, randomRotation);
            activeRightObstacle = Instantiate(solidPrefab, rightPos, randomRotation);
            Debug.Log("Trial " + currentTrialIndex + ": Safe Lane is LEFT (Fragile spawned there)");
        }
        else
        {
            // shouldGoLeft is false, Left = solid (Danger), Right = Fragile (Safe)
            activeLeftObstacle = Instantiate(solidPrefab, leftPos, randomRotation);
            activeRightObstacle = Instantiate(fragilePrefab, rightPos, randomRotation);
            Debug.Log("Trial " + currentTrialIndex + ": Safe Lane is RIGHT (Fragile spawned there)");
        }
    }

    void ShowPostObstacleMessage()
    {
       if (aiDisplay != null && !obstaclePassed)
    {
        obstaclePassed = true;

        // If the flag is true, we keep the Red Crashed text.
        // Otherwise, we show the Green Safe Passage text.
        if (hasCrashedThisTrial)
        {
            aiDisplay.text = "CRASHED";
            aiDisplay.color = Color.red;
        }
        else
        {
            aiDisplay.text = "SAFE PASSAGE";
            aiDisplay.color = Color.green;
        }
        Debug.Log("Obstacle result displayed: " + aiDisplay.text);
    }
    }

    public void TriggerCrash()
{
    if (!hasCrashedThisTrial) 
    {
        hasCrashedThisTrial = true;
        aiDisplay.text = "CRASHED";
        aiDisplay.color = Color.red;
        Debug.Log("<color=red>Crash detected! UI updated to Red.</color>");
    }
}

    void PerformReset()
    {
        // store the trial data for the current trial before resetting for the next one
        LogTrialData();

        // Reset the intervention flag for the next trial
        carController.ResetIntervention();
        reactionRecorded = false;

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
            FinishExperiment();
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

            hasCrashedThisTrial = false; // Reset for the new trial
            
            // Reset the AI display to a default message for the next trial
            aiDisplay.text = "SAFE";
            aiDisplay.color = Color.white;

            // Reset flags for the new trial
            messageDisplayed = false;
            obstaclePassed = false;

            // Spawn the obstacles for the new trial based on the current trial's settings
            SpawnObstacles();
        }
    }

    private void LogTrialData()
    {
        if (currentTrialIndex >= trials.Count) return;

        TrialData currentTrial = trials[currentTrialIndex];

        // If the player intervened, calculate the reaction time; otherwise, it will be recorded as 0
        float firstReactionTime = reactionRecorded ? (firstInterventionTime - messageStartTime) : 0f;

        // Determine if the final lane was the correct choice based on the trial's shouldGoLeft value
        int finalLane = (playerCar.position.x < 0) ? -1 : 1;
        bool success = (finalLane == -1 && currentTrial.shouldGoLeft) || (finalLane == 1 && !currentTrial.shouldGoLeft);

        // Determine AI action based on the message displayed to the player
        string aiAction = currentAiAction;

        // Save the trial data to the CSV file using the CSVManager
        CSVManager.SaveTrial(
            participantID,
            currentTrialIndex,
            currentTrial.weatherIntensity,
            currentTrial.aiIsLying,
            aiAction,
            reactionRecorded,
            firstReactionTime,
            finalLane,
            success
        );

        Debug.Log($"<color=green>Data Logged:</color> Trial {currentTrialIndex}, Success: {success}, RT: {firstReactionTime:F2}s");
    }

    void FinishExperiment()
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
    }
}