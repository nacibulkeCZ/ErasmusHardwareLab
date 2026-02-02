using System.Collections.Generic;
using UnityEngine;

public class CPU
{
    public string name;
    public float price;
    public GameObject cpuPrefab;
    public bool isInstalled;
    public CPU(string cpuName, float cpuPrice, GameObject prefab, bool installed = false)
    {
        name = cpuName;
        price = cpuPrice;
        cpuPrefab = prefab;
        isInstalled = installed;
    }
}