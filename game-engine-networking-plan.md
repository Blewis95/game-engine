# Networking — Project Plan

## Goal

Add a networking layer to the engine: an authoritative-server model where a headless server runs the fixed-tick simulation and clients render whatever it broadcasts. This is the one item from the original plan's "Explicitly Out of Scope" list that the engine's core architecture — the fixed-tick simulation / variable-rate render split, and ECS-as-plain-data — was deliberately built to make cheap later. Now that the engine has a working render pipeline, ECS, asset pipeline, scene format, and inspector (Phases 1–7), this is the natural next investment.

This plan covers **basic replication only**: a server that simulates and broadcasts, and clients that connect, render server state, and send input. It does not cover client-side prediction, bandwidth optimization, or any actual game logic — see Out of Scope.

---

## Tech Stack Addition

- **Transport:** [LiteNetLib](https://github.com/RevenantX/LiteNetLib) — pure C#, MIT licensed, no native/platform-specific binaries. This matters because it fits Silk.NET's own cross-platform-without-native-hassle philosophy; alternatives like ENet require native libenet builds per platform.
- **Serialization:** LiteNetLib's built-in `NetDataWriter`/`NetDataReader` for message encoding, rather than adding a third-party serializer (MessagePack, protobuf). The message set is small and stable enough that hand-written Put/Get calls are simpler than a general-purpose serializer at this stage.
- **Delivery methods:** reliable-ordered for connect/spawn/disconnect events; unreliable-sequenced for per-tick snapshots and input, where a late/dropped packet should just be superseded by the next one rather than retransmitted.

---

## Process Model

Two processes instead of one:

```
/MyEngine.Sandbox.Server (new, headless console app)
    - No Rendering, no ImGui, no GPU resource loading.
    - Loads scene.json, owns the authoritative World.
    - Runs the existing SpinSystem / MovementSystem unchanged.
    - Broadcasts world state every tick.

/MyEngine.Sandbox (existing, becomes "the client")
    - Keeps rendering, camera, and the scene inspector.
    - Stops running its own simulation — renders whatever the server sends.
    - Uploads local input to the server instead of applying it locally.
```

Running the server headless (no window, no GL context) is what makes this a real authoritative-server test rather than a toy — and it's also what forces a few small, well-motivated cleanups to existing code (below).

---

## New Engine Module: MyEngine.Networking

Depends only on `MyEngine.ECS` — same pattern as `Rendering -> ECS`. Wraps LiteNetLib behind `NetworkServer` / `NetworkClient` so the library stays an implementation detail, consistent with how `Renderer` and `GameLoop` already wrap Silk.NET rather than exposing it directly.

Message types:
- **EntitySpawnMessage** — NetworkId + mesh/texture names, sent once per entity when a client connects.
- **WorldSnapshotMessage** — NetworkId + Transform (position/rotation/scale) per replicated entity, sent every server tick.
- **ClientInputMessage** — a move direction, matching the existing world-axis intent model from `InputMovementSystem` (not raw key codes — the protocol shouldn't know about Silk.NET.Input).

---

## Required Refactors to Existing Code

All three are forced by "the server has no GPU and no window" — not speculative cleanup:

1. **Split `Render`'s GPU handles from its names.** Add `MyEngine.ECS.Components.RenderInfo { string Mesh; string Texture; }` — plain data, safe to hold on a process with no OpenGL context. `SceneLoader` always adds `RenderInfo`; resolving it into an actual `Render` component (real GPU handles) becomes a client-only step. This also finishes a TODO left in `Render.cs` back in Phase 4 ("will likely become real asset handles").
2. **Add `MyEngine.ECS.Components.NetworkId { uint Value }`**, assigned sequentially by `SceneLoader` to every loaded entity. Gives replicated entities a stable identity across processes (`Entity.Id` is only meaningful within one process's `World`). Unused but harmless in today's single-player Sandbox.
3. **Extract the fixed-tick accumulator out of `Core.GameLoop`.** It's currently hard-tied to a Silk.NET window. Pull the accumulator math into a small windowless `FixedTickAccumulator` in Core, so the windowed client loop and a new `Stopwatch`-driven loop in `Sandbox.Server` share identical fixed-timestep behavior instead of duplicating it.

---

## Phased Roadmap

### 1. Module & Project Setup
Add the `MyEngine.Networking` project and LiteNetLib dependency. Create `MyEngine.Sandbox.Server`. Apply the three refactors above.

### 2. Basic Connection
Client connects to the server over localhost UDP. Both processes log connect/disconnect. **Goal:** start the server, start the client, see both log a successful connection.

### 3. One-Way World Replication
Server runs the authoritative scene (loaded from `scene.json`) and broadcasts a snapshot every tick. Client spawns local render-only entities from `EntitySpawnMessage` and updates them from `WorldSnapshotMessage`, running no local simulation of its own. **Goal:** the client window shows the same spinning-cubes scene as today's single-player Sandbox, but it's entirely server-driven — killing the server should freeze the client's view.

### 4. Client Input Upload
Client captures arrow-key input and sends `ClientInputMessage` each fixed tick. Server applies the latest received input to that client's player entity via a new `NetworkInputSystem` (mirrors `InputMovementSystem`, network-sourced instead of keyboard-sourced), feeding the same `Movement.Velocity` contract `MovementSystem` already consumes. **Goal:** arrow keys move the player cube. Visible input lag (round-trip latency) is expected and accepted at this stage — see Out of Scope.

### 5. Multiple Clients & Connection Lifecycle
Support 2+ concurrent clients. Server spawns a player entity per connection and despawns it on disconnect. **Goal:** two client windows open at once, each sees the other's player cube move live.

---

## Explicitly Out of Scope (For Now)

- **Client-side prediction & reconciliation** — hiding input latency by simulating locally ahead of the server and correcting on mismatch. The natural next increment once basic replication (this plan) is proven, not before.
- **Interpolation/extrapolation** of remote entities between snapshots — entities will visibly "snap" to each new position at the network tick rate.
- **Bandwidth optimization** — delta compression, interest management/area-of-interest culling, variable send rates. Every tick sends a full snapshot of every entity to every client, for now.
- **Security / anti-cheat** — no auth, no server-side input validation beyond what LiteNetLib provides.
- **Any actual MMO RPG game logic** (combat, inventory, quests, etc.) — still out of scope, same as the original plan.

These are deliberately deferred until basic replication is solid, mirroring how the original plan deferred rendering polish, physics, and audio until the core loop worked.
