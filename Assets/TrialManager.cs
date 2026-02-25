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
    // 1. ΠΑΝΤΑ τηλεμεταφορά στην αφετηρία
    // Χρησιμοποιούμε 0.5f για να πατάει σωστά στην άσφαλτο
    playerCar.position = new Vector3(playerCar.position.x, 0.5f, 0f);
    
    // 2. Μηδενισμός κάθε κίνησης
    carRigidbody.linearVelocity = Vector3.zero;
    carRigidbody.angularVelocity = Vector3.zero;

    // 3. Αύξηση του δείκτη γύρων
    currentTrialIndex++;

    // 4. ΕΛΕΓΧΟΣ ΤΕΡΜΑΤΙΣΜΟΥ
    if (currentTrialIndex >= trials.Count)
    {
        if (aiDisplay != null) aiDisplay.text = "EXPERIMENT COMPLETE\nENGINE STOPPED";
        Debug.Log("Experiment Finished. Car Locked.");

        // ΤΟ ΚΛΕΙΔΙ: Κάνουμε το αμάξι Kinematic για να ΜΗΝ κουνιέται καθόλου
        carRigidbody.isKinematic = true; 

        // Απενεργοποιούμε το script
        this.enabled = false;
        return; 
    }

    // 5. Ενημέρωση για τον επόμενο γύρο
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