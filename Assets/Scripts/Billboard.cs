using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Cache the camera transform for performance
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // Use LateUpdate so the camera has finished moving for the frame
    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Option A: Standard LookAt (Object faces the camera position)
            transform.LookAt(mainCameraTransform);

            // Option B: Parallel Alignment (Object faces the same direction as the camera)
            // transform.forward = mainCameraTransform.forward;
        }
    }
}