# SIGNAL LOST

First-person sci-fi survival / psychological horror game built in Unity. You wake up alone on Kepler-9, a deep-space research station that has lost contact with Earth. Restore life support, trace an impossible signal, and discover what A.R.I.A. — the station AI — is hiding about you.

## Status

Playable vertical slice (13/13 runtime tests green): full guided loop from emergency pod to station core, with a 3-ending choice sequence.

## Features

- **First-person survival** — oxygen / health / battery resources, flashlight with power drain, crouch (noise management), sprint
- **Sealed industrial station** — procedurally built (ProBuilder-style) hub layout: PodRoom → Habitation Deck → Medbay / Storage → Comm Deck → Security → Research → Station Core
- **AI-driven horror** — Echo enemy with NavMesh FSM (dormant → patrol → investigate → chase → attack), noise perception system, door-banging behavior, contextual A.R.I.A. voice guidance via subtitle system
- **Story flags & objectives** — data-driven narrative (ScriptableObject logs, story flags, guided locked-door hints), 3 distinct endings
- **Procedural audio** — all SFX and ambience generated from code (no external audio assets): industrial hum, shutter doors, heartbeat during danger, stingers
- **Atmosphere** — URP fog, emissive signage, emergency flicker lighting, damage vignette, runtime-baked navmesh (zero embedded scene blobs)
- **Game UX** — title screen with settings (sensitivity / volume), pause menu, inventory panel, save/load (F5/F9), objective tracker

## Tech Stack

| | |
|---|---|
| Engine | Unity 6000.6.0f1 (URP 17) |
| Language | C# (~50 scripts, asmdef modular: Core / Player / Interaction / Inventory / AI / Narrative / Audio / UI / World) |
| AI Navigation | com.unity.ai.navigation 2.0.14 (NavMeshSurface runtime bootstrapped) |
| Level build | Fully code-generated scene (`SceneBuilder.BuildMain`) — deterministic, version-controlled |
| Testing | Unity Test Framework — 13 PlayMode tests + editor SmokeTest suite |
| Build | Windows standalone, batchmode CLI pipeline |

## Run

```
# Editor
Open in Unity 6000.6.0f1 → Assets/_Project/Scenes/Main.unity

# Build (batchmode)
Unity.exe -batchmode -projectPath . -quit -executeMethod SignalLost.EditorTools.PlayerBuild.BuildPlayer

# Tests
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode
```

## Controls

| Key | Action |
|---|---|
| WASD / Shift / Ctrl | Move / sprint / crouch |
| E | Interact |
| F | Flashlight |
| TAB | Inventory (1-6 use) |
| ESC | Pause |
| F5 / F9 | Save / load |

## Author

Raliq Hidayat BM3
