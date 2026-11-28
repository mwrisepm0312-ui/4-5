using FishNet.Transporting;
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

            // 전시 모드: 같은 PC 안의 휴대폰 창만 연결 (전시장 다른 기기가 들어오지 못하게,
            // 방화벽 허용 창이 뜨지 않게 하려는 목적. 방화벽 부분은 실제 PC에서 확인 필요)
            if (ExhibitionMode.IsOn)
            {
                tugboat.SetServerBindAddress(ExhibitionMode.ServerAddress, IPAddressType.IPv4);
                tugboat.SetServerBindAddress("::1", IPAddressType.IPv6);
                Debug.Log("[전시 모드] 서버를 이 PC 안(127.0.0.1)에서만 열어요");
            }
        }
    }
}


