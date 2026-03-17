using UnityEngine;

public class RadioPulseEffect : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 3f;     // How fast it throbs
    public float pulseAmount = 0.05f; // How much bigger it gets

    private Vector3 originalScale;

    void Start()
    {
        // Remember the starting size so it doesn't grow out of control
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Math magic to make it smoothly breathe in and out!
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }
}