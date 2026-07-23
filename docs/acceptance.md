# Acceptance ledger notes

Live checklist is owned by `AcceptanceLedger` at runtime. This file tracks Phase 1 sign-off status for humans/agents.

## Status (2026-07-23)

- v1.1.0 freeze fix: never remap canvases inside `willRenderCanvases` (re-entrancy hang); lighter input latch; modular `BepInEx/plugins/UEBS2Stereo/` Nexus layout
- Loader / BepInEx: done
- F8 engage/restore: fixed continuous freeze while stereo on
- Package: `dist/UEBS2Stereo-1.1.0.zip`

## Rules

- Temporary UI hide is proof-only (`FirstProofUiHide`); F9 exits it. Shipped default is false.
- While `stereoEngaged`, never emit native mono; use dual-mono SBS.
- Completeness requires every normal-play overlay wired or explicitly Deferred (temporal PPS/HBAO Deferred).
- Confirm claims against jdocmunch `docs/PLAN.md` acceptance sections before marking todos done.

## Hotkeys

- F8 toggle stereo
- F9 exit proof UI hide
- `[` `]` IPD, `;` `'` convergence, F7 eye swap, F6 zero-IPD
