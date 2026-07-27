# UEBS2 Mods — Agent instructions

## Related project: Arkham Knight 3D SBS

If working on `Arkham 3D Projects/Arkham Knight 3D` or Batman Arkham Knight stereo:
**NEVER remove `Binaries\Win64\dxgi.dll`.** It is required for half-SBS; without it the game is mono.
Must be the **geo-11 loader** dxgi (SHA256 `5B871985…`, 174080 bytes) — not the legacy `8603C2CB…` (Fatals the packer).
Locked recipe: that project's `working_config/MILESTONE_v0.6.0_LOADER_DXGI.md`, `NEVER_REMOVE_DXGI.txt`, `AGENTS.md`, `.cursor/rules/dxgi-required.mdc`.

## Required tooling

This project **requires** jcodemunch and jdocmunch. Do not use Grep/Read/Glob as the primary path for indexed material.

### jcodemunch (code)

- Index root: this workspace (`UEBS2 Mods`), especially `StereoMod/`.
- At session start: `list_repos`. If missing/stale: `index_folder` on this workspace.
- Prefer: `search_symbols` / `search_text` → `get_context_bundle` / `get_symbol_source` → `find_references` / `get_call_hierarchy` / `get_blast_radius`.
- After meaningful code edits: incremental `index_folder` or `register_edit`.

### jdocmunch (plan / docs)

- Repo: `local/uebs2-mods-docs`
- Sources: `docs/PLAN.md`, `docs/acceptance.md`, `UEBS2_3D_VR_Mod_Plan.txt`, `tools/*.md`
- Prefer: `search_sections` / `search_titles` / `lookup_term` → `get_section` / `get_section_context`
- After plan/doc revisions: `index_local` name `uebs2-mods-docs` (incremental)

### Routing

| Question type | Tool / source |
|---|---|
| Intent, wiring contract, acceptance, delivery order | jdocmunch → **`docs/PLAN.md` only** |
| Plugin symbols, patches, call sites, refactors | jcodemunch |
| Closed game DLL runtime behavior | RuntimeProbe artifacts under `docs/probe/` |
| `UEBS2_3D_VR_Mod_Plan.txt` | Historical discovery notes only — **ignore on conflict** with `docs/PLAN.md` |

## Live plan

Authoritative Phase 1 plan: [`docs/PLAN.md`](docs/PLAN.md) (synced from the Cursor plan file). Keep it synced after every plan revision, then refresh `uebs2-mods-docs`.
