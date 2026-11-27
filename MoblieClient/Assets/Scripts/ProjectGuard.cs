// ─────────────────────────────────────────────
// 프로젝트 확인 (모바일)
//
// 이 프로젝트는 '휴대폰 프로그램'이라 MOBILE_CLIENT 표시가 꼭 있어야 함.
// GameManager.cs가 이 표시로 PC·휴대폰을 구분하기 때문에,
// 표시가 빠지면 휴대폰 기능이 빠진 채로 빌드되는 사고가 남 → 아예 빌드가 안 되게 막음.
//
// 표시 위치: Project Settings → Player → Other Settings → Scripting Define Symbols
//            (Android, Windows(Standalone) 두 곳 모두)
// ─────────────────────────────────────────────
#if !MOBILE_CLIENT
#error "휴대폰 프로젝트에 MOBILE_CLIENT 표시가 없어요. Player Settings > Scripting Define Symbols (Android, Windows 둘 다)에 MOBILE_CLIENT 를 넣어 주세요."
#endif
