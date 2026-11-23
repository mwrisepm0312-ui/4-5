using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

// ──────────────────────────────────────
// 이 PC의 공유기(LAN) 주소 찾기
// 192.168.x.x 뿐 아니라 학교·전시장 와이파이에서 쓰는 10.x.x.x, 172.16~31.x.x 도 찾음
// (가상 네트워크·꺼진 랜카드는 건너뛰고, 기본 게이트웨이가 있는 것을 먼저 고름)
// ──────────────────────────────────────
public static class LanAddress
{
    // 휴대폰에 보여 줄 PC 주소. 못 찾으면 null
    public static string GetLocalIP()
    {
        return TryFind(out IPAddress ip, out _) ? ip.ToString() : null;
    }

    // 같은 공유기 안 휴대폰에게 보내는 브로드캐스트 주소. 못 찾으면 255.255.255.255
    public static string GetBroadcastIP()
    {
        if (TryFind(out IPAddress ip, out IPAddress mask) && mask != null)
        {
            byte[] ipBytes = ip.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();
            byte[] result = new byte[4];
            for (int i = 0; i < 4; i++)
                result[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            return new IPAddress(result).ToString();
        }
        return "255.255.255.255";
    }

    static bool TryFind(out IPAddress bestIp, out IPAddress bestMask)
    {
        bestIp = null;
        bestMask = null;
        int bestScore = -1;

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                bool isVirtual = desc.Contains("virtual") || desc.Contains("vmware")
                              || desc.Contains("virtualbox") || desc.Contains("hyper-v")
                              || desc.Contains("vethernet") || desc.Contains("bluetooth");

                IPInterfaceProperties props = ni.GetIPProperties();
                bool hasGateway = false;
                foreach (var gw in props.GatewayAddresses)
                {
                    if (gw.Address.AddressFamily == AddressFamily.InterNetwork
                        && !gw.Address.Equals(IPAddress.Any))
                    {
                        hasGateway = true;
                        break;
                    }
                }

                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    int rangeScore = PrivateRangeScore(addr.Address);
                    if (rangeScore == 0) continue;   // 사설 주소가 아니면 건너뜀

                    int score = rangeScore + (hasGateway ? 10 : 0) + (isVirtual ? 0 : 20);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIp = addr.Address;
                        bestMask = addr.IPv4Mask;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[LanAddress] 네트워크 주소 확인 오류: {e.Message}");
        }

        return bestIp != null;
    }

    // 사설 주소 범위 점수 (집 공유기에서 흔한 192.168 을 가장 먼저)
    static int PrivateRangeScore(IPAddress ip)
    {
        byte[] b = ip.GetAddressBytes();
        if (b[0] == 192 && b[1] == 168) return 3;
        if (b[0] == 10) return 2;
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return 1;
        return 0;
    }
}
