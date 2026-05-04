using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobotUnit : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text questionText;
    public TMP_Text statusText;
    public Image timerRing;

    [Header("Orbit")]
    public Transform orbitCenter;

    [Header("Damage Flash")]
    public Renderer[] flashRenderers;

    private Color[] originalColors;

    private void Awake()
    {
        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            originalColors = new Color[flashRenderers.Length];

            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] != null)
                {
                    originalColors[i] = flashRenderers[i].material.color;
                }
            }
        }
    }

    public void SetQuestion(string text)
    {
        if (questionText != null)
            questionText.text = text;
    }

    public void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    public void UpdateTimer(float current, float max)
    {
        if (timerRing == null || max <= 0f)
            return;

        float fill = Mathf.Clamp01(current / max);
        timerRing.fillAmount = fill;

        if (current > 5f)
        {
            timerRing.color = Color.green;
        }
        else if (current > 2f)
        {
            timerRing.color = new Color(1f, 0.55f, 0f);
        }
        else
        {
            timerRing.color = Color.red;
        }
    }

    public void PlayDamageFlash()
    {
        StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
            yield break;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].material.color = Color.red;
            }
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].material.color = originalColors[i];
            }
        }
    }
}