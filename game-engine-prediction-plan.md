# Client-Side Prediction & Reconciliation — Project Plan

## Goal

Hide round-trip latency for the local player's own movement. Right now (per the networking plan, Phases 4–5) the client uploads input and only sees its own cube move once a snapshot comes back from the server — the console output literally says "expect a little lag." This plan adds client-side prediction: the client applies its own input immediately, then reconciles against the server's authoritative state as snapshots arrive, replaying any not-yet-acknowledged inputs so the correction is invisible when nothing went wrong.

This was explicitly named in the networking plan's "Out of Scope" section as "the natural next increment once basic replication is proven." It now is.

**Scope boundary:** only the local player's own entity gets predicted/reconciled. Every other entity (background cubes, other players) keeps snapping directly to the latest snapshot `Transform`, exactly as today — smoothing that between snapshots (interpolation/extrapolation) is a separate deferred item and stays out of scope here too.

---

## Protocol Changes (MyEngine.Networking)

- **`ClientInputMessage`** gains a `uint Sequence` — a number that increases by one every input the client sends.
- **`WorldSnapshotMessage`** gains a `uint LastProcessedInputSequence`. This means the server can no longer send one shared broadcast buffer to everyone — each peer needs *its own* ack value — so the per-tick snapshot send changes from `SendToAll` to one personalized `Send` per connected peer. The entity list inside stays identical to what's already sent today; only the ack field differs per recipient.
- **New `YourPlayerMessage`** (NetworkId + Speed) — sent once to a newly-connected peer, right after its existing spawn batch, on the same reliable-ordered channel (so it's guaranteed to arrive after the entity itself exists client-side). This is the first time the client learns which of the entities it's rendering is *its own* — today it has no way to know.

## New Reusable Class: MyEngine.Networking.PredictedMovement

Buffers `(sequence, direction)` pairs the client has sent but not yet had acknowledged. `Reconcile(authoritative, speed, fixedDeltaTime, lastProcessedSequence)` drops every buffered input at or before the acknowledged sequence, then replays whatever's left on top of the server's authoritative `Transform` to reconstruct the predicted present. Pure data/math, no LiteNetLib types in its surface — same shape as the existing message codecs.

## Server Changes (MyEngine.Sandbox.Server/Program.cs)

- Track `Dictionary<NetPeer, uint> lastProcessedSequenceByPeer`, updated whenever a `ClientInputMessage` arrives (now carrying a sequence); cleaned up on disconnect alongside the existing `peerToEntity` removal.
- On `ClientConnected`, after the existing spawn-batch send, also send `YourPlayerMessage` with that peer's new entity's NetworkId and speed.
- Replace the per-tick `SendToAll` snapshot broadcast with one `Send` per connected peer carrying that peer's own ack value.

## Client Changes (MyEngine.Sandbox/Program.cs)

- Track `localPlayerNetworkId`, set when `YourPlayerMessage` arrives. At that point, add a real `Movement` component (server-told speed) to that one entity — it becomes the only entity in the client's `World` that ever has one.
- Each `FixedUpdate`: compute the arrow-key direction (unchanged), record it into a `PredictedMovement` to get a sequence number, send it — **and** immediately set that entity's `Movement.Velocity` and call the existing `MyEngine.ECS.Systems.MovementSystem.Update(world, fixedDeltaTime)`. Reusing that exact system means client prediction math literally cannot drift from server math — no duplicated formula to keep in sync. The call is automatically scoped correctly since the local player is the only entity with a `Movement` component client-side.
- On `WorldSnapshot`: for the local player's `NetworkId`, call `PredictedMovement.Reconcile(...)` instead of overwriting `Transform` directly. Every other entity keeps today's direct-overwrite behavior, unchanged.

---

## Verification Approach

LiteNetLib ships a built-in artificial-latency feature (`NetManager.SimulateLatency` / `SimulationMinLatency` / `SimulationMaxLatency`). Verification will temporarily crank this up on the server (e.g. 150–250ms) to make the effect unmistakable rather than subtle: at that latency, arrow-key movement would feel sluggish *without* prediction. *With* it, the local player should still respond instantly while the background cubes' snapshot-driven spin visibly lags behind — a direct, demonstrable before/after, not just "it didn't crash."

---

## Phased Roadmap

### 1. Protocol & Module Additions
Add the `Sequence`/`LastProcessedInputSequence` fields, the new `YourPlayerMessage`, and the `PredictedMovement` class. No behavior change yet — everything still compiles and runs exactly as today until the server/client actually use the new fields.

### 2. Server: Per-Peer Acknowledgment
Track and send personalized snapshots; send `YourPlayerMessage` on connect. **Goal:** existing client (not yet predicting) keeps working unmodified — snapshots still arrive, just now personalized per connection.

### 3. Client: Predict & Reconcile
Wire up instant local prediction and snapshot-driven reconciliation. **Goal:** arrow keys move the local player cube with no perceptible delay under normal (low) local-network latency.

### 4. Verify Under Artificial Latency
Enable LiteNetLib's latency simulation on the server and confirm the local player stays responsive while remote/background entities visibly lag at the simulated network speed. **Goal:** a clear, demonstrable before/after — the entire point of this plan, shown working under conditions that actually stress it.

---

## Explicitly Out of Scope (For Now)

- **Interpolation/extrapolation** for remote entities — already deferred in the networking plan; unaffected by this one.
- **Misprediction correction beyond simple snap-and-replay** — there's no collision or other divergence source yet, so server and client predictions should always agree. The mechanism is being built for when that stops being true (physics, collision), not because it's needed today. No smoothing/blending of a visible correction is being added since there's currently nothing to demonstrate it with.
- **Bandwidth optimization** — still deferred from the networking plan.
- **Any actual game logic** — still out of scope, unchanged.
