using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using MyEngine.ECS;
using MyEngine.ECS.Components;

namespace MyEngine.Physics;

/// <summary>
/// Wraps a Bepu Simulation so the library stays an implementation detail,
/// same as Renderer/GameLoop/NetworkServer wrapping their own libraries.
/// Server-only: nothing here runs on the client.
/// </summary>
public sealed class PhysicsWorld : IDisposable
{
    private readonly BufferPool _bufferPool;
    private readonly Simulation _simulation;
    private readonly Dictionary<Entity, BodyHandle> _dynamicBodies = new();
    private readonly Dictionary<Entity, StaticHandle> _staticBodies = new();

    // Stronger than real-world (-9.81) for a snappier, more game-ish fall/jump
    // feel. Tunable later; not worth its own configuration surface yet.
    private static readonly Vector3 Gravity = new(0f, -20f, 0f);

    public PhysicsWorld()
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks { Gravity = Gravity },
            new SolveDescription(velocityIterationCount: 8, substepCount: 1));
    }

    /// <summary>Creates a Bepu body/static for any Transform+Collider entity not tracked yet.</summary>
    public void SyncNewEntities(World world)
    {
        foreach (var entity in world.Query<Transform, Collider>())
        {
            if (_dynamicBodies.ContainsKey(entity) || _staticBodies.ContainsKey(entity))
                continue;

            var transform = world.GetComponent<Transform>(entity);
            var collider = world.GetComponent<Collider>(entity);

            var shape = new Box(collider.HalfExtents.X * 2f, collider.HalfExtents.Y * 2f, collider.HalfExtents.Z * 2f);
            var shapeIndex = _simulation.Shapes.Add(shape);
            var pose = new RigidPose(ToSystem(transform.Position), ToSystem(transform.Rotation));

            if (collider.IsStatic)
            {
                var handle = _simulation.Statics.Add(new StaticDescription(pose, shapeIndex));
                _staticBodies[entity] = handle;
            }
            else
            {
                var inertia = shape.ComputeInertia(1f);
                var description = BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(shapeIndex, 0.1f), new BodyActivityDescription(0.01f));
                var handle = _simulation.Bodies.Add(description);
                _dynamicBodies[entity] = handle;
            }
        }
    }

    /// <summary>
    /// One-shot upward velocity impulse - the only place that intentionally
    /// overrides Y velocity (ApplyVelocities deliberately leaves Y alone so
    /// it stays gravity's to own the rest of the time).
    /// </summary>
    public void ApplyJump(Entity entity, float jumpSpeed)
    {
        if (!_dynamicBodies.TryGetValue(entity, out var handle))
            return;

        var body = _simulation.Bodies.GetBodyReference(handle);
        body.Awake = true;

        var currentVelocity = body.Velocity.Linear;
        body.Velocity.Linear = new Vector3(currentVelocity.X, jumpSpeed, currentVelocity.Z);
    }

    /// <summary>Stops tracking an entity (e.g. a disconnected player) and removes its body/static from the simulation.</summary>
    public void RemoveEntity(Entity entity)
    {
        if (_dynamicBodies.Remove(entity, out var bodyHandle))
            _simulation.Bodies.Remove(bodyHandle);
        else if (_staticBodies.Remove(entity, out var staticHandle))
            _simulation.Statics.Remove(staticHandle);

        // Shapes aren't removed/pooled here - a known simplification for
        // this plan's scope, not a correctness issue at our entity counts.
    }

    /// <summary>Pushes Movement.Velocity's horizontal (X/Z) components into each tracked dynamic body ahead of a step.</summary>
    public void ApplyVelocities(World world)
    {
        foreach (var (entity, handle) in _dynamicBodies)
        {
            if (!world.TryGetComponent<Movement>(entity, out var movement))
                continue;

            var body = _simulation.Bodies.GetBodyReference(handle);

            // A sleeping body's velocity isn't integrated even if we set it -
            // force it awake whenever we're actively driving it from input.
            body.Awake = true;

            // Only drive X/Z from input - Y is gravity's (and later, jump's)
            // to own. Overwriting it here would cancel gravity out every
            // tick before Step() ever gets a chance to accumulate it.
            var horizontal = ToSystem(movement.Velocity);
            var currentVelocity = body.Velocity.Linear;
            body.Velocity.Linear = new Vector3(horizontal.X, currentVelocity.Y, horizontal.Z);
        }
    }

    /// <summary>
    /// Casts a short ray straight down from each dynamic body to determine
    /// whether it's currently resting on something, writing the result into
    /// a Grounded component (added if the entity doesn't have one yet).
    /// </summary>
    public void UpdateGrounded(World world)
    {
        const float rayDistance = 0.2f;

        foreach (var (entity, handle) in _dynamicBodies)
        {
            if (!world.TryGetComponent<Collider>(entity, out var collider))
                continue;

            var body = _simulation.Bodies.GetBodyReference(handle);
            var self = new CollidableReference(CollidableMobility.Dynamic, handle);
            var hitHandler = new GroundRayHitHandler { Self = self };

            _simulation.RayCast(body.Pose.Position, -Vector3.UnitY, collider.HalfExtents.Y + rayDistance, ref hitHandler);

            world.AddComponent(entity, new Grounded { Value = hitHandler.Hit });
        }
    }

    /// <summary>Pushes each static entity's current Transform (e.g. SpinSystem's rotation) into its Bepu pose.</summary>
    public void SyncStaticPoses(World world)
    {
        foreach (var (entity, handle) in _staticBodies)
        {
            var transform = world.GetComponent<Transform>(entity);
            var staticRef = _simulation.Statics.GetStaticReference(handle);
            staticRef.Pose = new RigidPose(ToSystem(transform.Position), ToSystem(transform.Rotation));
        }
    }

    public void Step(float fixedDeltaTime) => _simulation.Timestep(fixedDeltaTime);

    /// <summary>Pulls resulting dynamic body poses back into Transform after a step.</summary>
    public void SyncTransformsToWorld(World world)
    {
        foreach (var (entity, handle) in _dynamicBodies)
        {
            var body = _simulation.Bodies.GetBodyReference(handle);
            ref var transform = ref world.GetComponent<Transform>(entity);
            transform.Position = FromSystem(body.Pose.Position);
            transform.Rotation = FromSystem(body.Pose.Orientation);
        }
    }

    private static Vector3 ToSystem(Silk.NET.Maths.Vector3D<float> v) => new(v.X, v.Y, v.Z);
    private static Silk.NET.Maths.Vector3D<float> FromSystem(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Quaternion ToSystem(Silk.NET.Maths.Quaternion<float> q) => new(q.X, q.Y, q.Z, q.W);
    private static Silk.NET.Maths.Quaternion<float> FromSystem(Quaternion q) => new(q.X, q.Y, q.Z, q.W);

    public void Dispose()
    {
        _simulation.Dispose();
        _bufferPool.Clear();
    }
}
