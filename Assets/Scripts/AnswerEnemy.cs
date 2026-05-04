using TMPro;
using UnityEngine;

public class AnswerEnemy : MonoBehaviour
{
    public TMP_Text answerText;

    [Header("Movement")]
    public float orbitRadius = 0.6f;
    public float orbitHeight = 0.8f;
    public float orbitSpeed = 45f;
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 2f;

    public int AnswerValue { get; private set; }

    private Transform orbitCenter;
    private float startAngle;

    public void Initialise(int answerValue, Transform center, float angleDegrees)
    {
        AnswerValue = answerValue;
        orbitCenter = center;
        startAngle = angleDegrees;

        if (answerText != null)
            answerText.text = answerValue.ToString();
    }

    private void Update()
    {
        if (orbitCenter == null)
            return;

        float angle = (Time.time * orbitSpeed + startAngle) * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * orbitRadius,
            orbitHeight + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude,
            Mathf.Sin(angle) * orbitRadius
        );

        transform.position = orbitCenter.position + offset;

        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.forward = -transform.forward;
        }
    }
}