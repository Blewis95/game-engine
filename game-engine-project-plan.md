# Custom 3D Game Engine — Project Plan

## Goal

Build a home-grown 3D game engine (Unity-style, but custom) as a **standalone, reusable project**, separate from any game built on top of it. The first game targeted for this engine is an MMO RPG, but the engine itself should stay generic enough to support other games later.

This phase of planning covers **only the engine**, not the MMO RPG game logic or networking layer.

---

## Tech Stack

- **Language / Runtime:** C# on .NET 8
- **Graphics:** Silk.NET (bindings to OpenGL to start; leaves room to move to Vulkan later without changing language/tooling)
- **IDE / Workflow:** Visual Studio, built out via a Claude Code plugin

### Why this stack
- C# balances raw capability with development speed — critical for a project this large being built largely solo/AI-assisted.
- Silk.NET provides low-level graphics bindings without imposing engine opinions, so the engine design stays fully custom.
- .NET's tooling and package ecosystem in Visual Studio keeps iteration fast.

---

## Repo / Project Structure

Two **separate repositories**, one dependency direction only (Game → Engine, never the reverse):

```
/MyEngine (separate repo)
    /MyEngine.Core        - windowing, input, timing/game loop
    /MyEngine.Rendering    - Silk.NET wrapper, camera, mesh/material/shader
    /MyEngine.ECS          - entity-component-system
    /MyEngine.Assets       - model/texture loading, asset pipeline
    /MyEngine.Scene        - scene graph / level format
    /MyEngine.Sandbox      - internal test app for engine features (not a game)

/MyGame (separate repo)
    references MyEngine as a package or git submodule
```

Keeping the engine in its own repo (not just a folder inside the game repo) is what enforces real decoupling — code can't quietly leak game-specific logic into the engine if there's a repo boundary in the way.

---

## Core Architectural Decision: Simulation/Render Split (MMO-Ready by Design)

Even though networking is out of scope for now, one decision now avoids a rewrite later:

- **Fixed-tick simulation loop** — game logic (positions, health, AI) runs on a fixed timestep, independent of frame rate.
- **Variable-rate render loop** — reads simulation state to draw; never mutates it.
- **Simulation state as plain data** (ECS components) rather than logic-bearing objects.

This shape is exactly what authoritative-server MMO architectures need later (client-side prediction, server reconciliation), so building on an ECS with a fixed-tick sim loop now gets most of the way there for free — even while everything is local/single-player.

---

## Phased Roadmap

### 1. Environment & Repo Setup
Create the `MyEngine` solution with the class library projects above. Add Silk.NET via NuGet. Set up `MyEngine.Sandbox` as a minimal test app purely for exercising engine features in isolation.

### 2. Window + Game Loop
Get a Silk.NET window open with a fixed-tick update loop and a variable-rate render loop running side by side. This is the skeleton everything else attaches to.

### 3. Basic 3D Rendering Pipeline
Camera (perspective projection, basic fly/orbit controls), a simple shader (vertex/fragment), and the ability to load and draw a single textured mesh. **Goal:** a textured cube on screen.

### 4. ECS Core
Entities, components, and systems. Start simple: a `Transform` component, a `Render` component, and a couple of systems that iterate over them.

### 5. Asset Pipeline Basics
Loading models (glTF recommended — modern, well-supported) and textures from disk into engine-usable data, decoupled from any specific game's assets.

### 6. Scene Format & Simple RPG-Relevant Stubs
A basic scene/level description (e.g. JSON) the engine can load, plus generic placeholder components an RPG will eventually need: `Health`, `Movement`, basic input-to-movement binding. These stay generic engine building blocks — no game-specific logic.

### 7. Minimal Tooling
Even a bare-bones scene inspector (list entities, tweak a `Transform` live) pays off quickly once the actual game work starts.

---

## Open Decision (Not Yet Resolved)

Whether the `MyEngine.Sandbox` test app should evolve into a lightweight in-house editor over time, or whether scenes will be hand-authored in JSON for the foreseeable future. This affects how much time to invest in tooling before touching actual game code — worth revisiting once the ECS and rendering basics are in place.

---

## Explicitly Out of Scope (For Now)

- Networking / multiplayer / server architecture
- MMO RPG game logic (combat, inventory, quests, etc.)
- Physics engine
- Audio system
- Advanced rendering (lighting models beyond basics, shadows, post-processing)

These are deliberately deferred until the core engine loop, rendering, and ECS are solid.
