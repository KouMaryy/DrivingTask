using UnityEngine;

public class SolidObstacle : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the car hit the concrete
        if (collision.gameObject.CompareTag("Player"))
        {
            TrialManager manager = Object.FindFirstObjectByType<TrialManager>();
            if (manager != null)
            {
                manager.TriggerCrash();
            }
        }
    }
}
