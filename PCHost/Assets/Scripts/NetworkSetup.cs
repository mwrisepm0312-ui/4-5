using FishNet.Transporting.Tugboat;
using UnityEngine;

public class NetworkSetup : MonoBehaviour
{
    void Awake()
    {
        var tugboat = GetComponent<Tugboat>();
        if (tugboat != null)
        {
            // 휴대폰이 20초 동안 응답이 없을 때 끊음 (5초는 잠깐 끊김에도 쫓겨나서 늘림)
            tugboat.SetTimeout(20, true);
        }
    }
}


