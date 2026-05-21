# Virus Hot Potato — Project Rules for Claude

## Project Overview
Collocated mixed reality multiplayer game for Meta Quest 3S. Players share the same physical space (via spatial anchors) and pass a virtual virus between each other. The holder when the fuse timer expires is eliminated. Each player has a unique power that lets them interact with the virus differently.

## Always
- Work in `Scenes/Spatial-Anchors.unity` — this is the only active scene
- Use Fusion **Shared Mode** — never assume Server/Host mode patterns
- Never create material instances — use `MaterialPropertyBlock` for all shader property changes
- Never add `using UnityEngine.InputSystem` without checking the project already has the package

---

## Architecture Rules

### Networking (Fusion Shared Mode)
- Visual properties on `NetworkGrabbableVirus` use `[Networked, OnChangedRender(nameof(...))]`
- RPCs that change networked state go to `RpcTargets.StateAuthority` with `RpcInfo info = default`
- Power role gates inside RPCs check `info.Source` — never `Runner.LocalPlayer`
- Never call `Object.ReleaseStateAuthority()` unless `releaseStateAuthorityOnUnselect` is explicitly enabled
- The `ChangeDetector` in `FixedUpdateNetwork` is for non-visual state (elimination) only

### Power Role System (`PowerRoleSession`)
- 4 roles: **Color**, **Scale**, **Shape**, **Pulse** — assigned in join order via `powerAssignmentOrder[]`
- `powerAssignmentOrder` is serialized on the prefab — set it there, not in code
- For 2-player tests: Element 0 = Color, Element 1 = Pulse
- Everyone can scale — there is no scale gate
- `debugAllowAllPowersWhenUnassigned` exists on `PowerRoleSession` for solo testing

### Virus Visual System
- `VirusSwipeCycler` — drives all shader color properties + spike displacement via `MaterialPropertyBlock`
- `VirusShapeCycler` — enables/disables shape child GameObjects
- `OnShapeVariantChanged` must call `swipeCycler.RefreshAfterShapeChange()` so the newly active renderer gets the current theme
- Spike pulsation = `_SpikeDisplacement` float in the shader, animated in `VirusSwipeCycler.Update()` when `IsPulsating` is true
- **Body scale does NOT animate during pulse** — only spike vertex displacement

### Breath Sensor Pipeline
```
UDP "BLOW" → BreathSensorHandler (port 5006)
           → UnityMainThreadDispatcher
           → BreathSensorVirusIntegration.Update()
           → NetworkGrabbableVirus.RequestSpikeBurstFromTangible()
           → RPC_TriggerPulse() [gated to PulsePowerPlayer]
           → IsPulsating = true for 1 second
           → VirusSwipeCycler animates _SpikeDisplacement
```
- `UDPReceiver` must be **disabled** in the scene when `BreathSensorHandler` is active — they both bind port 5006
- `BreathSensorHandler` and `UnityMainThreadDispatcher` have `DontDestroyOnLoad`

### Shader Graph Rules
- Shader: `VirusNew` (Shader Graph)
- ColorMask texture: R = body zone, G = spike zone, B = vein zone
- **Always use `Sample Texture 2D LOD`** (not `Sample Texture 2D`) for any texture read in the Vertex stage — the regular node is Fragment-only and will silently break Vertex Position connections
- Vertex displacement chain: `Normal (Object) × _SpikeDisplacement × G(ColorMask LOD) + Position → Vertex Position`
- If a second mesh has a gap at spike bases during displacement: fix by softening the G channel mask in the ColorMask texture, or add a Power node (exponent 2) after G output in the graph

---

## Key Files
| File | Purpose |
|---|---|
| `Scripts/NetworkGrabbableVirus.cs` | Main networked virus — grab, fuse, visual RPCs |
| `Scripts/PowerRoleSession.cs` | Assigns power roles to players on join |
| `Scripts/VirusSwipeCycler.cs` | Shader colors + spike displacement animation |
| `Scripts/VirusShapeCycler.cs` | Shape variant switching |
| `Scripts/VirusGestureRouter.cs` | Routes hand swipe gestures to the nearest virus |
| `Scripts/BreathSensorHandler.cs` | UDP listener for blow sensor (port 5006) |
| `Scripts/BreathSensorVirusIntegration.cs` | Bridges blow event to virus RPC |
| `Scripts/GrabFreeTransformerNetworkBridge.cs` | Syncs two-hand scale to network |
| `Scripts/FormationManager.cs` | Spawns viruses into petri dish formations |
| `Prefabs/PowerRoleSession.prefab` | Configure `powerAssignmentOrder` here |

---

## What Not To Do
- Do not add body-scale animation during pulse (`Render()` loop scaling the whole virus) — spikes only
- Do not gate scale actions behind a power role — everyone scales
- Do not call `FindObjectsByType` every frame — cache results or use the existing cache patterns
- Do not use `Thread.Abort()` — close the socket and let the thread exit naturally
- Do not push directly to `main` — branch, PR, review
- Do not overwrite `NetworkGrabbableVirus.cs` with a local copy without checking git diff first — this file has been accidentally overwritten before
