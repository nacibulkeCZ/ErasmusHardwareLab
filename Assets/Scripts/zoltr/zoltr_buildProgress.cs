using UnityEngine;

public class zoltr_buildProgress : MonoBehaviour
{
    public zoltr_socketItem[] allItems;

    void Start()
    {
        RecalculateDependenciesForAllItems();
    }

    void RecalculateDependenciesForAllItems()
    {
        foreach (zoltr_socketItem item in allItems)
        {
            item.RecalculateDependencies();
        }
    }

    void Update()
    {
        RecalculateDependenciesForAllItems();   
    }
}
