using FishNet.Transporting.Tugboat;
using UnityEngine;

public class NetworkSetup : MonoBehaviour
{
    void Awake()
    {
        var tugboat = GetComponent<Tugboat>();
        if (tugboat != null)
        {
            tugboat.SetTimeout(5, true);
        }
    }
}


