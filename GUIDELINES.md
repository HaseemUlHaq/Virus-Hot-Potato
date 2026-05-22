# Virus Hot Potato — Team Guidelines

## What We Are Building
A collocated mixed reality game for Meta Quest 3S. Players are physically in the same room, share a virtual virus object that floats above a table, and pass it between each other. The virus has a fuse timer — whoever is holding it when it explodes is eliminated. Each player has a unique power that changes how the virus looks and behaves, making the game a sensory and social experience.

**Final experience:** 2–4 players, same room, hands-only interaction, virus that reacts to touch and breath, colourful visual feedback, colocation via spatial anchors so everyone sees the virus in exactly the same physical spot.

---

## Player Powers
| Player | Power | How triggered |
|---|---|---|
| 1 | **Color** — cycle virus colour theme | Hand swipe left/right near the virus |
| 2 | **Pulse** — make spikes pulsate | Blow into the breath sensor |
| 3 | **Shape** — cycle virus shape variant | Hand swipe left/right near the virus |
| 4+ | No power role | Can still grab and pass |

Everyone can **scale** the virus (two-hand grab stretch).

**Solo debug (`Debug Allow All Powers` on `PowerRoleSession`):** left hand swipe = color only, right hand swipe = shape only. Normal multiplayer keeps both RPCs per swipe (roles gate on the virus).

---

## Tech Stack
- **Unity 6** (6000.3.10f1)
- **Photon Fusion — Shared Mode** (not dedicated server, not host-client)
- **Meta Quest 3S**, Oculus Interaction SDK for hand tracking
- **Spatial Anchors** for colocation (all players see objects in the same world position)
- **Breath sensor** sends UDP `"BLOW"` to port 5006 on the device running the game

---

## Networking Rules

### Shared Mode means
- There is no authoritative server — one peer holds `StateAuthority` per object
- Whoever grabs the virus gets `StateAuthority` on it
- Visual state (colour, scale, pulse, shape) is `[Networked]` and replicated to everyone

### RPCs
- RPCs that change game state go to `RpcTargets.StateAuthority`
- Always include `RpcInfo info = default` in the signature
- Power role gates use `info.Source` (the sender's PlayerRef), never `Runner.LocalPlayer`

### Power role gate example
```csharp
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_RequestCycleMaterial(bool next, RpcInfo info = default)
{
    if (_powerRoleSession != null && !_powerRoleSession.IsColorPlayer(info.Source))
        return;
    // ... change state
}
```

### Visual properties
```csharp
[Networked, OnChangedRender(nameof(OnMaterialIndexChanged))]
public int MaterialIndex { get; set; }
```
`OnChangedRender` fires on every client when the value changes — use it to update visuals, never use `Update()` to poll networked values for visuals.

---

## Virus Visual System

### Shader — how it works
All virus meshes share one Shader Graph (`VirusNew`). Each shape variant has its **own material** with its **own ColorMask texture** — the shader reads that texture to know which parts of the mesh are body, spikes, or veins.

**ColorMask texture channels (grayscale per channel):**
| Channel | Zone | Controls |
|---|---|---|
| R | Body | `_Color_1` — main body colour |
| G | Spikes | `_Color_2` + `_SpikeDisplacement` vertex push |
| B | Veins / overlay | `_Color_3_Overlay` + `_Color3_Scale` glow intensity |

White (1) = full effect, Black (0) = no effect. Soft gradients at zone boundaries give smooth transitions.

**Runtime color injection:**
`VirusSwipeCycler` holds a `VirusColorTheme[]` array (10 themes). On theme change it writes all properties into a `MaterialPropertyBlock` — **never create material instances**, this breaks GPU batching on the headset.

### Spike Pulsation
- `IsPulsating` (networked bool) is set to `true` for 1 second when the breath sensor fires
- `VirusSwipeCycler.Update()` reads `IsPulsating` and animates `_SpikeDisplacement` using a sine wave
- `_SpikeDisplacement` multiplies through the **G channel** of the ColorMask — only spike-zone vertices displace
- **Only spikes move** — the whole body does not scale during pulse
- In Shader Graph vertex stage: always use **`Sample Texture 2D LOD`** — the regular `Sample Texture 2D` is Fragment-only and silently breaks the Vertex Position connection

### Adding a new virus mesh / shape variant
1. Import the FBX — check Scale Factor in the import settings so it matches the existing virus scale in-scene
2. Add the mesh as a child GameObject under the virus prefab root
3. Create a new **material** using the `VirusNew` shader
4. Author a **ColorMask texture** for this mesh:
   - Paint **R channel** white over the body surface
   - Paint **G channel** white over the spikes — use a **soft gradient at the base** of each spike (hard edge = visible gap during displacement)
   - Paint **B channel** white over any vein/glow detail
5. Assign the ColorMask texture to the material
6. Register the new shape GameObject in `VirusShapeCycler` in the Inspector (drag into the shapes array)
7. Do the same for the matching `LockedVirusDisplay` in the ExampleFormation prefab (see below)

---

## Formation System

### How it works
Three things spawn when the session starts (via `FormationManager`):
1. **ExampleFormation** — static display showing players the target pattern to recreate
2. **PlaceholderFormation** — the interactive target with snap slots players place viruses into
3. **Work Viruses** — the actual virus objects players grab, modify, and place

### Placing a virus into a slot
- `PlaceholderSlot` (extends `PetriDish`) runs proximity snap every network tick
- Bring a virus within **0.4 m** of a slot and release it — after **0.25 s** dwell it snaps in
- Grabbing it again releases it from the slot

### Slot validation — what must match
Each slot has required properties (set from `VirusFormationData` ScriptableObject):

| Property | Tolerance |
|---|---|
| `MaterialIndex` (colour theme) | Exact match |
| `VirusScale` | ±0.3 (networked scale snaps to 0.5 steps: 0.5, 1.0, 1.5, … 3.0) |
| `ShapeVariantIndex` | Exact match |
| `IsPulsating` | Exact match |

Slot shows **green** when correct, **red** when wrong, neutral when empty.

### Completion
`PlaceholderFormation.FixedUpdateNetwork()` checks all slots every tick. When every slot is `IsFilledCorrectly`, `IsComplete` flips to `true` and `OnIsCompleteChanged` fires on all clients.

### Configuring a round — the ScriptableObject drives everything
The **ExampleFormation** (reference display) and the **PlaceholderFormation** (snap target) both read from the **same** `VirusFormationData` asset. You only set values in one place and both formations stay in sync automatically.

**To set up round 1:**
1. `Assets → Create → Virus Hot Potato → Formation Data` → name it `Round01`
2. Fill in each `SlotConfig`:
   - `materialIndex` 0–9 (which colour theme)
   - `scale` 0.05–3.0
   - `shapeVariantIndex`
   - `isPulsating` ⚠️ — see warning below
   - `localPosition` / `localEulerAngles` — where this slot sits within the formation
3. In the scene, find `FormationManager` and assign `Round01` into the **Formation Data Per Round** array (Element 0)

**For additional rounds:** create `Round02`, `Round03` etc. and add them to the array in order. `FormationManager` cycles through them automatically — `ResetForNewRound()` advances to the next index.

### Adding a new virus slot to the formations
When you add a new virus to the game (new shape or new round config) you need to update three things in sync — the data, the placeholder, and the example.

**Step 1 — Update the ScriptableObject**
Open `VirusFormationData` asset → add a new entry to `slots[]`:
- Set `materialIndex`, `scale`, `shapeVariantIndex`
- Set `localPosition` / `localEulerAngles` to position the slot within the formation
- Leave `isPulsating = false` (see warning below)

**Step 2 — PlaceholderFormation prefab**
- Open the `PlaceholderFormation` prefab
- Add a new child GameObject, add a `PlaceholderSlot` component to it
- Position it to match the `localPosition` you set in the ScriptableObject
- Assign visual materials: `emptyMaterial`, `correctMaterial`, `wrongMaterial` on the slot renderer
- Register the new slot in the `PlaceholderFormation.slots[]` array in the Inspector (same index as in the ScriptableObject)

**Step 3 — ExampleFormation prefab**
- Open the `ExampleFormation` prefab
- Add a new child GameObject, add a `LockedVirusDisplay` component to it
- Add `VirusSwipeCycler` and `VirusShapeCycler` to the same GameObject and assign them to `LockedVirusDisplay`'s fields
- Register the new display in `ExampleFormation.displayViruses[]` (same index)
- Position and appearance are driven automatically from the ScriptableObject at runtime — no manual setup needed beyond registering it

**Work viruses are spawned automatically** — `FormationManager` spawns one `virusWorkPrefab` per slot in the ScriptableObject.

### ⚠️ Known issue — isPulsating as a requirement
`IsPulsating` only lasts 1 second per blow. A virus cannot be reliably placed in a slot while pulsating. **Do not set `isPulsating = true` on any slot config until this is redesigned** (e.g. a latched pulsating state, or remove pulsating as a slot requirement entirely).

---

## Scene Setup Checklist
Before building, verify the scene (`Spatial-Anchors.unity`) has:
- [ ] `BreathSensorHandler` + `BreathSensorVirusIntegration` on a single empty GameObject
- [ ] `UnityMainThreadDispatcher` on its own GameObject
- [ ] `UDPReceiver` GameObject **disabled** (conflicts with BreathSensorHandler on port 5006)
- [ ] `PowerRoleSession` prefab configured: Element 0 = Color, Element 1 = Pulse (for 2-player test)
- [ ] `VirusSwipeCycler` has **Apply To Child Renderers** enabled and Spike Pulse Max Displacement set to a visible value (0.03–0.05)

---

## Git Rules
- **Never push directly to `main`** — always branch and PR
- **Never overwrite a file without checking `git diff` first** — `NetworkGrabbableVirus.cs` has been accidentally overwritten before by pushing a local copy
- Branch naming: `feature/description` or your-name + feature (e.g. `lorena/breath-sensor`)
- If you pull and something stops compiling, check whether a script you depend on was changed — common files that get edited by both: `NetworkGrabbableVirus.cs`, `PowerRoleSession.cs`

---

## Known Warnings (safe to ignore)
- `There are 2 event systems in the scene` — pre-existing, not breaking
- `There should be only one OVRPassthroughLayer` — pre-existing, not breaking
- `ErrorFunctionUnsupported: xrRequestSceneCaptureFB` — Quest 3S doesn't support scene capture, harmless

---

## Pending Work
- Victory condition: `PlaceholderFormation.OnIsCompleteChanged` fires when a formation is complete — needs win state / UI
- `SessionStatusHUD` needs adding to the scene (World Space Canvas, TMP_Text references)
- Normal map on virus mesh still has import issues
- Duplicate EventSystem and OVRPassthroughLayer should be cleaned up
- `UDPReceiver.cs` still in project — conflicts with `BreathSensorHandler` on port 5006 if accidentally re-enabled. Delete or add a clear DO-NOT-ENABLE note.
- `ShapeTestDriver.cs` is test-only (keyboard S = shape, P = pulse) — remove before final build
- `isPulsating` as a slot validation requirement is unreliable (lasts 1s) — remove from `PlaceholderSlot.ValidateVirus` or replace with a latched state
- `ExampleVirusFormation` prefab needs **Preassigned Formation Data** field set to `VirusFormationData_01` so non-master clients see the correct display viruses on join

---

## Upcoming Feature — Physical Toolbox

A real physical box (~80 cm wide, ~40 cm tall) on the table. Players see MR overlays on top of it, grab real handles to open virtual doors, and reveal virtual objects hidden inside.

---

## Toolbox Design Notes

### What it does
```
Real physical box on table (~80cm wide × 40cm tall)
  ↓
Players grab real handles → pull open real doors
  ↓
Virtual door animations follow the physical doors
  ↓
Virtual objects inside become visible
  ↓
Objects were hidden by an occluder mesh covering the interior
```

### Do you need Depth API? No.

Depth API (OVR Depth API) uses real-world depth sensing to auto-occlude virtual objects behind real surfaces. **Do not use it for this feature.** It causes build errors on this project and is not needed.

What you need is **manual occlusion via a custom occluder mesh** — an invisible mesh that writes to the depth buffer so virtual objects behind it are culled. You control exactly when it activates.

### Occluder material (URP, Quest-compatible)

Create a material in URP with these settings:
```
Shader: Universal Render Pipeline/Unlit  (or a custom URP Unlit shader)
Base Color: black, Alpha = 0
Surface Type: Opaque
Render Face: Both
ZWrite: On
ColorMask: 0   (writes depth but renders no pixels)
Render Queue: 2000 (Geometry — must render BEFORE interior objects)
```
Interior virtual objects must have Render Queue > 2000 (e.g. 2001) so they render after the occluder writes depth. When the occluder is active, their depth test fails → they are invisible. Disable the occluder → depth values gone → objects render.

### Anchoring the box to the real world

The project already uses QR codes for table detection (`TableAnchor.cs` + `NetworkedTableAnchor.cs`). Use the same pattern for the toolbox — stick a QR code or ArUco marker on the outside of the physical box. On detection, anchor the virtual box to that transform. Because the box is stationary once placed, a single detection at session start is enough.

```csharp
public void OnTrackableAdded(MRUKTrackable trackable)
{
    if (trackable.MarkerPayloadString != "virus_toolbox") return;
    virtualBox.transform.SetPositionAndRotation(
        trackable.transform.position, trackable.transform.rotation);
}
```

### Detecting the handle grab (hand proximity)

The project already has hand tracking via Oculus Interaction SDK. Use distance-based detection — the same approach as `VirusGestureRouter`. When the player's hand is within ~8–10cm of the handle's world position, trigger the door open event.

```csharp
void Update()
{
    if (Vector3.Distance(playerHand.position, handleAnchor.position) < 0.09f && !_doorOpen)
        OpenDoor();
}
```

Use 8–10cm tolerance (not exact) because Meta hand tracking has a few centimetres of positional error.

### Door state must be networked (Fusion Shared Mode)

All players must see the door open at the same moment. Add a `[Networked]` bool to the toolbox NetworkBehaviour:

```csharp
[Networked, OnChangedRender(nameof(OnDoorStateChanged))]
public NetworkBool IsDoorOpen { get; set; }

[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_OpenDoor(RpcInfo info = default)
{
    IsDoorOpen = true;
}

private void OnDoorStateChanged()
{
    occluderMesh.SetActive(!IsDoorOpen);
    doorAnimator.SetBool("Open", IsDoorOpen);
}
```

### Revealing interior objects

Interior objects start disabled or hidden behind the occluder. When door opens:
```csharp
public void OpenDoor()
{
    occluderMesh.SetActive(false);   // depth barrier removed
    doorAnimator.SetTrigger("Open");
    virtualLight.SetActive(true);    // add a point light — inside box is dark on camera
    foreach (var obj in interiorObjects)
        obj.SetActive(true);
}
```

### Roadblocks to watch out for

| Issue | What happens | Fix |
|---|---|---|
| Alignment drift | Virtual box drifts from physical over time as tracking accumulates error | Re-detect QR periodically or add a manual realign button |
| Hand tracking error | Player's virtual hand doesn't visually touch real handle | Use 8–10cm grab radius, not exact position |
| Occluder bleeds in one eye | SPI rendering issue — object visible in one eye only | Add `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX` to shader; or switch to a URP pre-built occluder approach |
| Interior too dark | Passthrough shows real dark box interior before door opens | Add virtual point light inside that activates on door open |
| Late-joiner sees open box wrong | If door is already open when someone joins, they need synced state | `[Networked]` bool + `OnChangedRender` handles this automatically |
| Virtual door vs physical door mismatch | Player pulls real door; virtual door plays fixed animation | Good enough for prototype. For polish: add a hinge sensor to read real door angle |
| Multiple headsets, one unseen QR | One player hasn't scanned box QR — sees objects appear from nowhere | Gate box activation until all active players have confirmed the QR |
