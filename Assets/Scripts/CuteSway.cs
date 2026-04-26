using UnityEngine;

public class CuteSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAngle = 10.0f; 
    public float swaySpeed = 2.0f;  

    void LateUpdate() // Using LateUpdate to ensure camera has moved first
    {
        // 1. Get the direction to the camera
        Vector3 lookDir = Camera.main.transform.forward;

        // 2. Calculate the "Sway" rotation (Z-axis tilt)
        float zTilt = Mathf.Sin(Time.time * swaySpeed) * swayAngle;

        // 3. Set the rotation: Face camera AND apply the tilt
        Quaternion lookRotation = Quaternion.LookRotation(-lookDir, Camera.main.transform.up);
        transform.rotation = lookRotation * Quaternion.Euler(0, 0, zTilt);
    }
}