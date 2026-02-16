using UnityEngine;
using UnityEngine.InputSystem; // Needed for Player Input component

public class CarController : MonoBehaviour
{
    // Settings that can be changed from the Unity Inspector
    public float forwardSpeed = 15f; // Car Speed
    public float laneDistance = 3.0f; // Distance between lanes
    public float sideSpeed = 10f; //Car Speed of lane change

    private int targetLane = 0; // // 0: Left, 1: Right

    // Thia function is automatically called from Player Input component
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
    }
}