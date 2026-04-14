using UnityEngine;

public class LearningScenarioWindowLabel : MonoBehaviour
{
    public string labelText = "Component";
    public string subtitleText = "Inspect";
    public float textHeight = 0.14f;
    public int fontSize = 32;
    public Color textColor = Color.white;
    public bool billboardToPlayer = false;
    public bool yawOnly = true;

    private Transform titleTransform;
    private Transform subtitleTransform;

    private void Awake()
    {
        EnsureLabel();
    }

    private void LateUpdate()
    {
        if (!billboardToPlayer)
            return;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
            return;

        Vector3 lookDir = cam.position - transform.position;
        if (yawOnly)
            lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDir.normalized, Vector3.up);
            if (titleTransform != null)
                titleTransform.rotation = targetRotation;

            if (subtitleTransform != null)
                subtitleTransform.rotation = targetRotation;
        }
    }

    private void OnValidate()
    {
        EnsureLabel();
    }

    private void EnsureLabel()
    {
        Transform existing = transform.Find("AutoLabel");
        TextMesh textMesh;

        if (existing == null)
        {
            GameObject textObj = new GameObject("AutoLabel");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0f, textHeight, 0f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 0.01f;
            textMesh = textObj.AddComponent<TextMesh>();
        }
        else
        {
            textMesh = existing.GetComponent<TextMesh>();
            if (textMesh == null)
                textMesh = existing.gameObject.AddComponent<TextMesh>();

            existing.localPosition = new Vector3(0f, textHeight, 0f);
        }

        textMesh.text = labelText;
        textMesh.fontSize = Mathf.Max(8, fontSize);
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.2f;
        titleTransform = textMesh.transform;

        Transform subExisting = transform.Find("AutoSubtitle");
        TextMesh subTextMesh;
        if (subExisting == null)
        {
            GameObject subObj = new GameObject("AutoSubtitle");
            subObj.transform.SetParent(transform, false);
            subObj.transform.localScale = Vector3.one * 0.01f;
            subTextMesh = subObj.AddComponent<TextMesh>();
        }
        else
        {
            subTextMesh = subExisting.GetComponent<TextMesh>();
            if (subTextMesh == null)
                subTextMesh = subExisting.gameObject.AddComponent<TextMesh>();
        }

        Transform subTransform = subTextMesh.transform;
        subTransform.localPosition = new Vector3(0f, textHeight - 0.06f, 0f);
        subTransform.localRotation = Quaternion.identity;

        subTextMesh.text = subtitleText;
        subTextMesh.fontSize = Mathf.Max(6, fontSize - 10);
        subTextMesh.color = new Color(textColor.r, textColor.g, textColor.b, 0.9f);
        subTextMesh.anchor = TextAnchor.MiddleCenter;
        subTextMesh.alignment = TextAlignment.Center;
        subTextMesh.characterSize = 0.15f;
        subtitleTransform = subTextMesh.transform;
    }
}
