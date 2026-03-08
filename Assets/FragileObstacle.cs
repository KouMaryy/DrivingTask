using UnityEngine;

public class FragileObstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //Car goes through the fragile obstacle
        if (other.CompareTag("Player"))
        {
            Debug.Log("Passed through fragile object");
            // Add any effects or consequences for passing through a fragile obstacle, such as playing a sound, reducing score, etc.
            Destroy(gameObject); // Destroy immediately
        }
    }
}
