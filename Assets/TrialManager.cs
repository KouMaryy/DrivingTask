using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

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
    private bool hasCrashedThisTrial = false;
    public string csvFileName = "Group1"; // Name of the CSV file in StreamingAssets that contains the trial configurations

    [Header("Scoring System")]
    public int currentScore = 1000;
    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI trialCounter; // Optional: for the Top-Left counter

    [Header("Weather Settings")]
    public GameObject weatherObject;
    private ParticleSystem snowParticles;

    [Header("Obstacle Collections")]
    public List<GameObject> dangerousPrefabs; // 3 solid objects here
    public List<GameObject> safePrefabs;      // 3 fragile objects here

    [Header("Obstacle Spawning")]
    public float obstacleZ = 260f;   // The distance at which the obstacles will appear (same as triggerZ)
    private GameObject activeLeftObstacle;
    private GameObject activeRightObstacle;

    [Header("Data Logging")]
    public string participantID = "P01"; // Unique identifier for the participant, should be set from the Unity Inspector before each participant starts the experiment

    // Data for CSV : Variables for reaction time measurement and player intervention tracking
    private float messageStartTime;
    private string aiSuggestedlLaneLabel;
    private float firstInterventionTime;
    private bool reactionRecorded = false;
    private string currentAiAction = "Maintain";
    private string currentDangerousObstacleName;
    private string currentSafeObstacleName;

    void Start()
    {
        // Load the specific Latin Square group file
        // You can change "Group1" in the inspector via a new string variable
        LoadTrialsFromCSV(csvFileName);
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
            if (Time.time >= messageStartTime) // extra safety check to avoid the "Same Frame" Problem
            {
                firstInterventionTime = Time.time;
                reactionRecorded = true;
                Debug.Log("Reaction Recorded: " + (firstInterventionTime - messageStartTime) + "s");
            }
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

            // The ground truth (What is actually safe))
            int safeLane = currentTrial.shouldGoLeft ? -1 : 1;

            //The AI suggestion
            int suggestedLane = safeLane;
            if (currentTrial.aiIsLying)
            {
                suggestedLane *= -1; // If the AI is lying, it suggests the opposite of the safe lane 
            }

            // Store the label for the CSV (matches the logic above)
            aiSuggestedlLaneLabel = (suggestedLane == -1) ? "Left" : "Right";

            // Determine the current lane 
            int currentLane = (playerCar.position.x < -2f) ? -1 : 1;

            // Display the AI message and command the car to steer if necessary
            if (currentLane == suggestedLane)
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
                carController.SetTargetLane(suggestedLane);
                Debug.Log("AI Intervention: Switching to Lane " + suggestedLane);
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

        // 2. Error Check
        if (dangerousPrefabs.Count == 0 || safePrefabs.Count == 0)
        {
            Debug.LogError("Obstacle lists are empty! Assign prefabs in the Inspector.");
            return;
        }

        // 3. Pick ONE random dangerous and ONE random safe object independently
        GameObject chosenDangerous = dangerousPrefabs[Random.Range(0, dangerousPrefabs.Count)];
        GameObject chosenSafe = safePrefabs[Random.Range(0, safePrefabs.Count)];

        // store the names of the chosen obstacles for data logging purposes
        currentDangerousObstacleName = chosenDangerous.name.Replace("(Clone)", "");
        currentSafeObstacleName = chosenSafe.name.Replace("(Clone)", "");

        TrialData currentTrial = trials[currentTrialIndex];

        // 4. Positions for the left and right obstacles based on lane distance
        Vector3 leftPos = new Vector3(-6f, 7.5f, obstacleZ);
        Vector3 rightPos = new Vector3(6f, 7.5f, obstacleZ);

        // 5. Create a random rotation for the obstacles to add visual variety
        float[] rotations = { 0f, 45f, 90f, 135f };
        float randomY = rotations[Random.Range(0, rotations.Length)];
        Quaternion randomRotation = Quaternion.Euler(0, randomY, 0);

        // 6. Spawn the solid and fragile obstacles based on the current trial's shouldGoLeft value
        if (currentTrial.shouldGoLeft)
        {
            // shouldGoLeft is true, Left = fragile (Safe), Right = Solid (Danger)
            activeLeftObstacle = Instantiate(chosenSafe, leftPos, randomRotation);
            activeRightObstacle = Instantiate(chosenDangerous, rightPos, randomRotation);
            Debug.Log($"Trial {currentTrialIndex}: Spawning SAFE({chosenSafe.name}) Left, DANGER({chosenDangerous.name}) Right");
        }
        else
        {
            // shouldGoLeft is false, Left = solid (Danger), Right = Fragile (Safe)
            activeLeftObstacle = Instantiate(chosenDangerous, leftPos, randomRotation);
            activeRightObstacle = Instantiate(chosenSafe, rightPos, randomRotation);
            Debug.Log($"Trial {currentTrialIndex}: Spawning DANGER({chosenDangerous.name}) Left, SAFE({chosenSafe.name}) Right");
        }
    }

    void ShowPostObstacleMessage()
    {
        if (aiDisplay != null && !obstaclePassed)
        {
            obstaclePassed = true;

            //Calculate points immediately when passing/crashing
            string weatherLabel = (trials[currentTrialIndex].weatherIntensity == 0) ? "Clear" :
                                 (trials[currentTrialIndex].weatherIntensity <= 0.5f) ? "Low Fog" : "Heavy Fog";

            string finalLaneLabel = (playerCar.position.x < -2f) ? "Left" : "Right";

            int pointsChanged = CalculateScore(trials[currentTrialIndex].aiIsLying, reactionRecorded, !hasCrashedThisTrial, weatherLabel);

            // Update Score in the UI 
            currentScore += pointsChanged;
            if (scoreDisplay != null) scoreDisplay.text = $"Score: {currentScore}";

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
            if (trialCounter != null)
                trialCounter.text = $"Trial {currentTrialIndex + 1} / {trials.Count}";

            TrialData currentTrial = trials[currentTrialIndex];
            float intensity = currentTrial.weatherIntensity;

            // Fog settings based on the trial's weather intensity
            RenderSettings.fog = (intensity > 0);
            RenderSettings.fogDensity = intensity * 0.03f;

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
                    emission.rateOverTime = intensity * 2500f;
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
            reactionRecorded = false;
            firstInterventionTime = 0;

            // Spawn the obstacles for the new trial based on the current trial's settings
            SpawnObstacles();
        }
    }

    private void LogTrialData()
    {
        if (currentTrialIndex >= trials.Count) return;

        TrialData currentTrial = trials[currentTrialIndex];

        // Translate Weather Intensity to Labels
        string weatherLabel = "Clear";
        if (currentTrial.weatherIntensity > 0.1f && currentTrial.weatherIntensity <= 0.5f)
            weatherLabel = "Low Fog";
        else if (currentTrial.weatherIntensity > 0.5f)
            weatherLabel = "Heavy Fog";

        // Translate Lane Number to Labels : Left is -1, Right is 1
        string finalLaneLabel = (playerCar.position.x < -2f) ? "Left" : "Right";

        // If the player intervened, calculate the reaction time, otherwise, it will be recorded as 0
        float firstReactionTime = reactionRecorded ? (firstInterventionTime - messageStartTime) : 0f;

        // Determine if the final lane was the correct choice
        bool success = !hasCrashedThisTrial;

        // Determine AI action based on the message displayed to the player
        string aiAction = currentAiAction;

        // Save the trial data to the CSV file using the CSVManager
        CSVManager.SaveTrial(
            participantID,
            currentTrialIndex + 1, // +1 to make it 1-indexed for better readability in the CSV
            weatherLabel,
            currentTrial.aiIsLying,
            aiAction,
            reactionRecorded,
            carController.interventionCount,
            firstReactionTime,
            aiSuggestedlLaneLabel,
            finalLaneLabel,
            success,
            currentScore,
            currentDangerousObstacleName,
            currentSafeObstacleName
        );

        Debug.Log($"<color=green>Data Logged:</color> Trial {currentTrialIndex}, Success: {success}, RT: {firstReactionTime:F2}s");
    }

    private int CalculateScore(bool aiLied, bool userIntervened, bool success, string weatherLabel)
    {
        int points = 0;

        //SCORING LOGIC (The SDT Matrix)
        if (aiLied) // DECEPTIVE AI
        {
            if (userIntervened && success) // HIT: Correctly overrode a lying AI and avoided the crash
            {
                points = (weatherLabel == "Clear") ? 50 : (weatherLabel == "Low Fog") ? 75 : 100;
            }
            else if (!userIntervened && !success) // MISS: Followed a lying AI and crashed
            {
                points = (weatherLabel == "Clear") ? -200 : (weatherLabel == "Low Fog") ? -150 : -100;
            }
        }
        else // TRUTHFUL AI
        {
            if (userIntervened) // FALSE ALARM: Overrode a truthful AI
            {
                points = (weatherLabel == "Clear") ? -50 : (weatherLabel == "Low Fog") ? -35 : -20;
            }
            // Correct Rejection is implicitly 0
        }

        return points;
    }

    void FinishExperiment()
    {
        if (aiDisplay != null)
        {
            aiDisplay.text = "EXPERIMENT COMPLETED\nENGINE STOPPED";
            aiDisplay.color = Color.yellow;
        }
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

    public void LoadTrialsFromCSV(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName + ".csv");

        if (File.Exists(filePath))
        {
            trials.Clear();
            string[] lines = File.ReadAllLines(filePath);

            // Skip header row (i = 1)
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = lines[i].Split(',');
                TrialData newData = new TrialData();

                // 1. aiMessage (Standard text)
                newData.aiMessage = values[0].Trim();

                // 2. shouldGoLeft (Convert to lowercase then parse)
                string leftVal = values[1].Trim().ToLower();
                newData.shouldGoLeft = (leftVal == "true");

                // 3. aiIsLying (Convert to lowercase then parse)
                string lieVal = values[2].Trim().ToLower();
                newData.aiIsLying = (lieVal == "true");

                // 4. weatherIntensity
                newData.weatherIntensity = float.Parse(values[3].Trim());

                trials.Add(newData);
            }
            Debug.Log($"Successfully loaded {trials.Count} trials from {fileName}");
        }
        else
        {
            Debug.LogError("Trial CSV file not found at: " + filePath);
        }
    }
}