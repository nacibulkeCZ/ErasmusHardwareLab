using Unity.VisualScripting;
using UnityEngine;

public class HandAnimationHandlerer : MonoBehaviour
{
    [Header("Finger Values")]
    public float palecValue = 0;
    public float ukazovacekValue = 0;
    public float prostrednicekValue = 0;
    public float prstenicekValue = 0;
    public float malicekValue = 0;

    [Header("Finger Toggle")]
    public bool palecToggleActive = true;
    public bool ukazovacekToggleActive = true;
    public bool prostrednicekToggleActive = true;
    public bool prstenicekToggleActive = true;
    public bool malicekToggleActive = true;

    [Header("Toggle animation speed")]
    public float toggleSpeed = 2f;

    [Header("Toggle values")]
    public bool palecToggle = false;
    public bool ukazovacekToggle = false;
    public bool prostrednicekToggle = false;
    public bool prstenicekToggle = false;
    public bool malicekToggle = false;

    [Header("Finger Animators")]
    public Animator palecAnimator;
    public Animator ukazovacekAnimator;
    public Animator prostrednicekAnimator;
    public Animator prstenicekAnimator;
    public Animator malicekAnimator;

    private void Update()
    {
        checkToggles();
        checkValues();
        setAnimationVariables();
    }

    void checkToggles()
    {
        if (palecToggle && palecToggleActive) { palecValue += Mathf.Lerp(palecValue, 1, toggleSpeed * Time.deltaTime); }
        else if (palecToggleActive) { palecValue -= Mathf.Lerp(palecValue, 0, toggleSpeed * Time.deltaTime); }

        if (ukazovacekToggle && ukazovacekToggleActive) { ukazovacekValue += Mathf.Lerp(ukazovacekValue, 1, toggleSpeed * Time.deltaTime); }
        else if (ukazovacekToggleActive) { ukazovacekValue -= Mathf.Lerp(ukazovacekValue, 0, toggleSpeed * Time.deltaTime); }

        if (prostrednicekToggle && prostrednicekToggleActive)
        {
            prostrednicekValue += Mathf.Lerp(prostrednicekValue, 1, toggleSpeed * Time.deltaTime);
        }
        else if (prostrednicekToggleActive)
        {
            prostrednicekValue -= Mathf.Lerp(prostrednicekValue, 0, toggleSpeed * Time.deltaTime);
        }

        if (prstenicekToggle && prstenicekToggleActive)
        {
            prstenicekValue += Mathf.Lerp(prstenicekValue, 1, toggleSpeed * Time.deltaTime);
        }
        else if (prstenicekToggleActive)
        {
            prstenicekValue -= Mathf.Lerp(prstenicekValue, 0, toggleSpeed * Time.deltaTime);
        }

        if (malicekToggle && malicekToggleActive)
        {
            malicekValue += Mathf.Lerp(malicekValue, 1, toggleSpeed * Time.deltaTime);
        }
        else if (malicekToggleActive)
        {
            malicekValue -= Mathf.Lerp(malicekValue, 0, toggleSpeed * Time.deltaTime);
        }
    }

    void checkValues()
    {
        if (palecValue < 0) palecValue = 0;
        if (ukazovacekValue < 0) ukazovacekValue = 0;
        if (prostrednicekValue < 0) prostrednicekValue = 0;
        if (prstenicekValue < 0) prstenicekValue = 0;
        if (malicekValue < 0) malicekValue = 0;

        if (palecValue > 1) palecValue = 1;
        if (ukazovacekValue > 1) ukazovacekValue = 1;
        if (prostrednicekValue > 1) prostrednicekValue = 1;
        if (prstenicekValue > 1) prstenicekValue = 1;
        if (malicekValue > 1) malicekValue = 1;
    }

    void setAnimationVariables()
    {
        palecAnimator.SetFloat("Blend", palecValue);
        ukazovacekAnimator.SetFloat("Blend", ukazovacekValue);
        prostrednicekAnimator.SetFloat("Blend", prostrednicekValue);
        prstenicekAnimator.SetFloat("Blend", prstenicekValue);
        malicekAnimator.SetFloat("Blend", malicekValue);
    }
}
