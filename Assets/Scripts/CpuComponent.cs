using System.Collections.Generic;
using UnityEngine;

public class CpuComponent : MonoBehaviour
{
    public CPU cpu;
    [SerializeField] private List<Socket> sockets = new();
}
