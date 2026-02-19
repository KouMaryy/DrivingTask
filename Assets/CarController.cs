using UnityEngine;
using UnityEngine.InputSystem; 

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]

    // Settings that can be changed from the Unity Inspector
    public float forwardSpeed = 15f; // Car Speed
    public float laneDistance = 3.0f; // Distance between lanes
    public float sideSpeed = 10f; //Car Speed of lane change

    [Header("Steering Wheel Settings")]
    public Transform steeringWheel; // Reference to the steering wheel GameObject
    public float maxSteerAngle = 45f; // Maximum angle the steering wheel can rotate
    public float wheelRotationSpeed = 5f; // Speed at which the steering wheel rotates

    private int targetLane = 0; // 0: Left, 1: Right
    private float currentSteerAngle = 0f; // Current angle of the steering wheel

    // This function is automatically called from Player Input component
    public void OnMove(InputValue value)
    {
        // Converts the input into a "vector".
        // If you press 'D' (or right arrow), input.x becomes positive (>0).
        // If you press 'A' (or left arrow), input.x becomes negative (<0).
        Vector2 input = value.Get<Vector2>();

        // right (D or right arrow)
        if (input.x > 0) targetLane = 1;

        // left (A or left arrow)
        if (input.x < 0) targetLane = 0;
    }

    void Update()
    {
        // 1. Car always moves forward
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

        // 2. Calculate target position based on targetLane
        Vector3 targetPosition = transform.position;

        // 3. if targetLane == 0 --> x = 0 else if targetLane == 1 --> x = laneDistance.
        targetPosition.x = targetLane * laneDistance;

        // 4. Smooth movement (Lerp) from current position to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, sideSpeed * Time.deltaTime);

        // 5. Handle steering wheel rotation
        HandleSteeringWheel();
    }

    void HandleSteeringWheel()
    {
        if (steeringWheel == null) return;

        // CALCULATE DYNAMIC ROTATION:
        // We find the distance between where the car is (transform.position.x) 
        // and where it wants to go (targetLane * laneDistance).
        float distanceToTarget = (targetLane * laneDistance) - transform.position.x;

        // The target angle now depends on this distance. 
        // As distance goes to 0 (car reaches lane), targetAngle goes to 0 (wheel straightens).
        float targetAngle = distanceToTarget * maxSteerAngle;

        // Clamp the angle so it doesn't exceed the max degrees we set
        targetAngle = Mathf.Clamp(targetAngle, -maxSteerAngle, maxSteerAngle);

        // Smoothly transition the current angle to the target dynamic angle
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * wheelRotationSpeed);
        
        // Rotate the steering wheel around the Z-axis based on the current steer angle
        steeringWheel.localRotation = Quaternion.Euler(0, 0, -currentSteerAngle);
    }
}