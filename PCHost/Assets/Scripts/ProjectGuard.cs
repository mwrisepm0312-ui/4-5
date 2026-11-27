// ─────────────────────────────────────────────
// 프로젝트 확인 (PC)
//
// MOBILE_CLIENT 는 휴대폰 프로젝트에만 있어야 하는 표시.
// PC 프로젝트에 들어가면 GameManager.cs가 PC를 휴대폰으로 착각해
// 용의자 선택 등 PC 기능이 빠진 채로 빌드되는 사고가 남 → 아예 빌드가 안 되게 막음.
// ─────────────────────────────────────────────
#if MOBILE_CLIENT
#error "PC 프로젝트에 MOBILE_CLIENT 표시가 들어가 있어요. Player Settings > Scripting Define Symbols 에서 MOBILE_CLIENT 를 지워 주세요."
#endif
