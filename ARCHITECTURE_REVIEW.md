# Architectural Sanity Check – ExpeditionsReforged (tModLoader 1.4.4)

## Overall Health
**Status:** Good – scaffolding follows modern tModLoader roles with clear separation between Mod entry point, ModSystem UI orchestration, ModPlayer UI flags, and pure data models. No legacy APIs detected.

## Architecture Observations
- **Folder layout** mirrors modern practice: root Mod (`ExpeditionsReforged.cs`), `Systems` for cross-cutting UI orchestration, `Players` for per-player state, `UI` for client-only states, `Content/Expeditions` for data models, and `Common` for identifiers.
- **Responsibilities** are appropriately split: ModSystem owns UI lifecycle and layer insertion, ModPlayer keeps UI-open flags (no gameplay yet), UIState classes are pure UI shells, and expedition models are data-only.
- **Client/server separation** respected: UI is gated behind `Main.dedServ` in `Load` and only drawn when player-local flags request it.

## Risks / Concerns
1) **Persisted UI-open flags:** `ExpeditionsPlayer.SaveData` stores UI toggles. These are client-only concerns and persisting them across sessions/worlds could cause odd load states or cross-world leakage. Flags also are not network-synchronized, so multiplayer clients will diverge naturally (desired for UI), but persistence may be unnecessary.
2) **UI lifecycle vs. gameplay sync:** UI visibility currently relies solely on local booleans. As gameplay systems arrive (expedition state, rewards), dedicated network packets and authoritative state in `ModPlayer`/`ModSystem` will be needed to avoid desync.
3) **Lack of centralized expedition registry:** Data models exist, but there is no registry/factory yet. Before adding gameplay, a registry in a `ModSystem` (or content loader) should own definitions and validation to keep UI/data decoupled from gameplay logic.

## Recommendations (minimal)
- Treat UI-open booleans as **volatile client state**: consider not persisting them, or explicitly resetting on `OnEnterWorld` to avoid stale values.
- Plan a **network-aware expedition state flow**: `ModPlayer` should own per-player progress with explicit net packets for starting/completing/claiming rewards; `ModSystem` should broadcast authoritative updates to UI.
- Introduce an **expedition definition registry** in `ModSystem` before porting gameplay to ensure consistent lookups and facilitate multiplayer-safe synchronization.

## Migration Readiness
Current scaffolding is aligned with modern 1.4.4 patterns and is safe to continue migration. No premature coupling between UI and gameplay is present; there is room to add progression, rewards, UI interaction, and multiplayer sync using modern APIs.
