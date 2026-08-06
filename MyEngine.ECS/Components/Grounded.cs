namespace MyEngine.ECS.Components;

/// <summary>
/// Whether the entity is currently resting on something beneath it.
/// Server-internal simulation state written by PhysicsWorld - never
/// replicated to clients, since nothing there needs it (a jump request is
/// just silently ignored server-side if not grounded).
/// </summary>
public struct Grounded
{
    public bool Value;
}
