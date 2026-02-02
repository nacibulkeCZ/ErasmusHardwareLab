using UnityEngine;

public class FixedScale : MonoBehaviour
{
    [Tooltip("The scale to keep constant.")]
    public Vector3 fixedScale = Vector3.one;

    [Tooltip("If true, locks local scale. If false, tries to keep world scale constant.")]
    public bool local = true;

    private Vector3 initialParentScale;

    void Start()
    {
        if (transform.parent != null)
            initialParentScale = transform.parent.lossyScale;
        else
            initialParentScale = Vector3.one;
    }

    void LateUpdate()
    {
        if (local)
        {
            // 🔒 Lock local scale
            transform.localScale = fixedScale;
        }
        else
        {
            // 🌍 Lock world scale (simulate setting lossyScale)
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    fixedScale.x / parentScale.x,
                    fixedScale.y / parentScale.y,
                    fixedScale.z / parentScale.z
                );
            }
            else
            {
                transform.localScale = fixedScale;
            }
        }
    }
}
