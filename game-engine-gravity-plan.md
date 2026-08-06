# Gravity, Falling & Jumping — Project Plan

## Goal

Give the world verticality. Right now movement is flat X/Z only — there's no ground plane (the 5 background cubes float at y=0 with nothing beneath them), and `PhysicsWorld`'s gravity integrator callback is a deliberate no-op left over from the physics plan, which named this exact follow-on explicitly: "adding real gravity isn't free... it's a follow-on, not a side effect of adding collision." This plan adds a real ground, real gravity, and jumping.

---

## A Real Bug This Plan Must Fix

Re-reading `MyEngine.Physics/PhysicsWorld.cs`, `ApplyVelocities` currently does:

```csharp
body.Velocity.Linear = ToSystem(movement.Velocity);
```

This overwrites X, Y, *and* Z every tick, before `Step()` runs. Once gravity starts accumulating downward Y velocity during `Step()`, this line would stomp it right back to zero on the very next tick — the player would never actually fall. `ApplyVelocities` needs to only drive the horizontal (X/Z) components from `Movement.Velocity` and preserve whatever Y velocity the physics step already produced. Flagging this now the same way the sleeping-body bug was flagged ahead of the physics plan's Phase 2.

---

## Architecture

**Ground plane:** one new entity in `scene.json` — the same `cube.gltf`/`checker.png` mesh, scaled large and flat (`Transform.Scale = (50, 0.5, 50)`) with a matching static `Collider.HalfExtents = (50, 0.5, 50)`, positioned so its top surface sits at y=0. No new asset or rendering code needed. The 5 background cubes and the player spawn position both shift from y=0 to y=0.5 so they rest on top of the ground instead of being half-buried in it.

Worth naming honestly: `Collider.HalfExtents` and `Transform.Scale` are independent fields by design (mirroring the `RenderInfo`/`Render` decoupling), so nothing keeps their numbers in sync automatically — the scene author matches them by hand. Fine at 6 hand-authored entities; exactly the kind of repeated friction that should inform an eventual editor rather than something to solve now.

**Real gravity:** `PoseIntegratorCallbacks` (`MyEngine.Physics/PhysicsCallbacks.cs`) gets an actual `Gravity` vector applied to every dynamic body's velocity each integration step — the standard Bepu v2 pattern (precompute `gravity * dt` once per step in `PrepareForIntegration`, add it to velocity in `IntegrateVelocity`). Static bodies (the ground, the background cubes) are entirely unaffected, which is correct — nothing about them should fall.

**Grounded detection:** a short downward raycast from each dynamic body every tick (Bepu supports this natively) determines whether it's currently resting on something. Exposed as a new ECS `Grounded` component, written by `PhysicsWorld`. This is **server-internal only** — never replicated to clients, since nothing about it needs to be; a jump request is just silently ignored server-side if not grounded, same as any other invalid input.

**Jump input:** `ClientInputMessage` gains a `bool Jump` flag alongside its existing direction/sequence fields. The server applies an instantaneous upward velocity impulse when a jump is requested *and* the entity is currently grounded — gated so there's no infinite mid-air jumping. Space is the jump key (genre-standard); it already doubles as the dev-camera's fly-up control while the camera-look mouse button is held, but that's an orthogonal local-only tool and the overlap is harmless.

**Prediction implications, named honestly:** client-side local prediction (`MovementSystem`, no gravity, no collision) will now mispredict vertically too, not just horizontally — a jump will visibly not-quite-match the server's arc for a moment before `PredictedMovement.Reconcile` corrects it, the same accepted tradeoff already established for horizontal collision mispredictions. Not fixing this with client-side gravity simulation is a deliberate non-goal, consistent with the prediction plan's own boundary.

---

## Phased Roadmap

### 1. Ground Plane, Real Gravity & the ApplyVelocities Fix
Add the ground entity to `scene.json`, shift existing entities' y positions, implement real gravity in `PoseIntegratorCallbacks`, fix `ApplyVelocities` to preserve Y velocity. **Goal:** the player falls under gravity and comes to rest on the ground plane instead of floating or falling through it.

### 2. Grounded Detection
Downward raycast each tick, exposed as the new `Grounded` component. No player-visible behavior change yet — this phase just makes the signal available for Phase 3 to gate on.

### 3. Jump Input End-to-End
`ClientInputMessage.Jump`, server-side upward impulse gated on `Grounded`. **Goal:** pressing Space makes the local player jump and land back down, and can't be spammed to fly by holding it.

### 4. Verify & Feel
Playtest confirming no infinite-jump, gravity/landing feels reasonable, and the expected (accepted) client-side jump misprediction is visible-but-corrected rather than jarring or broken.

---

## Explicitly Out of Scope (For Now)

- **Falling off the world / void respawn** — the ground plane is simply sized large enough that walking off it isn't realistically reachable during testing.
- **Reduced air control** — full air control is kept; no separate airborne movement model.
- **Double-jump or variable/charge-based jump height** — a single fixed-height jump only.
- **Climbing or ledge-grabbing.**
- **Terrain / heightmap-based ground** — flat plane only.
- **Client-side gravity simulation to reduce jump misprediction** — stays deferred, same boundary the prediction plan already drew.
