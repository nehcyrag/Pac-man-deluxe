using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MenuIconSwing : MonoBehaviour
{
    [SerializeField] private float swingAngle = 12f;
    [SerializeField] private float cyclesPerSecond = 1.4f;

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float angle = Mathf.Sin(Time.unscaledTime * cyclesPerSecond * Mathf.PI * 2f) * swingAngle;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, angle);
    }
}
