# Physics & Collision — Project Plan

## Goal

Give the engine real collision so entities can't pass through each other or through the world. Today a player cube walks straight through background cubes and other players — that's a hard blocker for anything resembling real MMORPG movement or combat, and the most foundational gap left across the three completed plans (engine core, networking, prediction).

This plan also pays off something the prediction plan deliberately left untested: it named "no collision or other divergence source yet" as the reason client-side mispredictions never actually happen. This is where that stops being true — the final phase here is built specifically to watch a misprediction occur and get corrected, not just hope the mechanism works.

---

## Library Choice

**BepuPhysics v2** — pure C#, no native/platform-specific binaries. Same reasoning as picking LiteNetLib over ENet: it fits Silk.NET's cross-platform-without-native-hassle philosophy, where alternatives (Bullet, PhysX bindings) require native builds per platform. It's free and permissively licensed for commercial use, fast, actively maintained, and the de facto standard physics engine for .NET.

---

## Architecture

**New engine module: `MyEngine.Physics`** — wraps a Bepu `Simulation` behind a `PhysicsWorld` class, keeping the library an implementation detail the same way `Renderer`/`GameLoop`/`NetworkServer` already wrap Silk.NET and LiteNetLib. Depends only on `MyEngine.ECS`, the same shape as `Rendering -> ECS`.

**New ECS component: `Collider`** — plain data, no Bepu handle types leaking into ECS. Box shape (half-extents) + an `IsStatic` flag. This mirrors the existing `RenderInfo`/`Render` split: `Collider` is safe to hold anywhere, including the client, which never acts on it — same as how `RenderInfo` sits inertly on a process with no resolver wired up.

**Server-authoritative, exactly like the sim/render split:** physics only ever runs on the headless `MyEngine.Sandbox.Server`. `PhysicsWorld` maps entities with a `Collider` to real Bepu bodies/statics, keeping that mapping internally — the same shape as `RenderResourceResolver`'s name→resource cache. Each server tick:
1. Push `Movement.Velocity` into each dynamic body.
2. Step the Bepu simulation.
3. Pull the resulting poses back into `Transform`.

This **replaces** `MovementSystem.Update` for physics-enabled entities — collision-aware integration instead of today's naive `Position += Velocity * dt`. Background cubes get *static* box colliders; `SpinSystem`'s rotation is pushed into their static body's pose each tick too, so a spinning decorative cube still blocks a player walking into it.

**Deliberate non-goal — and the interesting payoff:** the client stays exactly as "dumb" as it is today. No client-side physics simulation; it still just renders `Transform` snapshots. Client-side prediction (`PredictedMovement`) stays naive velocity-only integration, which means it has no idea colliders exist. Walking toward an obstacle will visibly mispredict for a moment before the next snapshot's reconciliation snaps the client back to the server's collision-corrected position — the exact scenario the prediction plan named and explicitly deferred.

**Scene format extension:** `SceneDocument`/`SceneLoader` gains an optional `collider` block (half-extents, static/dynamic), the same way `render`/`spin`/`health`/`movement` were each added before it. The 5 background cubes in `scene.json` get static colliders; dynamically-spawned player entities (created server-side on connect, unchanged from today) get dynamic colliders.

---

## Phased Roadmap

### 1. Module, Component & Scene-Format Setup
Add the `MyEngine.Physics` project and BepuPhysics package. Add the `Collider` component. Extend the scene format and give the 5 background cubes static colliders in `scene.json`. No behavior change yet — nothing reads `Collider` until Phase 2.

### 2. Server Runs Physics for World Collision
Wire `PhysicsWorld` into the server tick loop, replacing `MovementSystem` for physics-enabled entities. **Goal:** the player cube can no longer walk through a background cube — it stops or slides along it instead of passing through.

### 3. Player-vs-Player Collision
Falls out mostly for free once multiple dynamic bodies share one simulation — every connected player is already a dynamic body once Phase 2 lands. **Goal:** two client windows open at once; walk player A into player B and both visibly block rather than overlap.

### 4. Prediction-Divergence Proof
Walk the local player into an obstacle and observe the visible mispredict-then-snap-correct. **Goal:** confirm `PredictedMovement.Reconcile` — built in the last plan, never actually exercised against a real divergence — does what it was built for, and that the correction is a clean snap rather than something jarring or broken.

---

## Explicitly Out of Scope (For Now)

- **Gravity, falling, jumping** — current movement is flat X/Z with no verticality. Adding real gravity isn't free (it needs a ground plane, a jump input, air-vs-grounded state) and is its own follow-on, not a side effect of adding collision.
- **Sphere, capsule, or mesh colliders** — box shapes only for this plan.
- **Any combat or hit-detection game logic** — still belongs to a future separate game project, not the engine, same boundary every prior plan has held.
- **A client-side physics mirror to eliminate mispredictions entirely** — the existing snap-correct reconciliation is considered sufficient for now. A full deterministic client-side physics simulation (needed to predict collision outcomes exactly) is a much larger undertaking than this plan needs to justify.
