using System.Collections.Generic;

// ──────────────────────────────────────
// 직업별로 꼭 끝내야 하는 미션 (증거 보내기 조건)
// NFCManager의 카드 허용 규칙과 같게 맞출 것
//  2인: 수사관1 = 현장감식·CCTV / 수사관2 = 목격자·사이버
//  3인: 수사관1 = 현장감식 / 수사관2 = CCTV / 수사관3 = 목격자 (사이버는 공용이라 필수 아님)
//  4인: 직업마다 자기 미션 1개
// 미션은 끝까지 하면(증거를 모두 모으면) 클리어되므로, 클리어 = 그 미션의 증거를 모두 모음
// ──────────────────────────────────────
public static class MissionRules
{
    public const string Forensic = "Forensic";
    public const string CCTV = "CCTV";
    public const string Witness = "Witness";
    public const string Cyber = "Cyber";

    public static string[] RequiredMissions(string job, int maxPlayers)
    {
        if (maxPlayers == 2)
        {
            if (job == "수사관1") return new[] { Forensic, CCTV };
            if (job == "수사관2") return new[] { Witness, Cyber };
        }
        else if (maxPlayers == 3)
        {
            if (job == "수사관1") return new[] { Forensic };
            if (job == "수사관2") return new[] { CCTV };
            if (job == "수사관3") return new[] { Witness };
        }
        else
        {
            if (job == "현장감식관") return new[] { Forensic };
            if (job == "CCTV분석관") return new[] { CCTV };
            if (job == "목격자조사관") return new[] { Witness };
            if (job == "사이버수사관") return new[] { Cyber };
        }
        return new string[0];
    }

    public static string DisplayName(string mission)
    {
        switch (mission)
        {
            case Forensic: return "현장감식";
            case CCTV: return "CCTV";
            case Witness: return "목격자";
            case Cyber: return "사이버";
        }
        return mission;
    }

    // 아직 끝내지 않은 필수 미션 (없으면 빈 목록)
    public static List<string> RemainingMissions(DataManager dm)
    {
        var remaining = new List<string>();
        if (dm == null) return remaining;
        foreach (string mission in RequiredMissions(dm.SelectedJob, dm.MaxPlayers))
        {
            if (!dm.ClearedMissions.Contains(mission))
                remaining.Add(mission);
        }
        return remaining;
    }
}
