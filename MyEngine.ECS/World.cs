using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MyEngine.ECS;

/// <summary>
/// Owns entities and their component data. Components are plain data
/// stored per-type in a Dictionary&lt;entityId, T&gt; — simple rather than
/// cache-optimal, which is fine at this stage.
/// </summary>
public sealed class World
{
    private int _nextEntityId;
    private readonly HashSet<int> _entities = new();
    private readonly Dictionary<Type, object> _componentStores = new();

    public Entity CreateEntity()
    {
        var entity = new Entity(_nextEntityId++);
        _entities.Add(entity.Id);
        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        _entities.Remove(entity.Id);
        foreach (var store in _componentStores.Values)
            ((System.Collections.IDictionary)store).Remove(entity.Id);
    }

    public void AddComponent<T>(Entity entity, T component) where T : struct =>
        GetStore<T>()[entity.Id] = component;

    public bool HasComponent<T>(Entity entity) where T : struct => GetStore<T>().ContainsKey(entity.Id);

    public void RemoveComponent<T>(Entity entity) where T : struct => GetStore<T>().Remove(entity.Id);

    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        var store = GetStore<T>();
        ref T value = ref CollectionsMarshal.GetValueRefOrNullRef(store, entity.Id);
        if (Unsafe.IsNullRef(ref value))
            throw new KeyNotFoundException($"Entity {entity.Id} has no component of type {typeof(T).Name}.");

        return ref value;
    }

    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct =>
        GetStore<T>().TryGetValue(entity.Id, out component);

    public IEnumerable<Entity> All()
    {
        foreach (int id in _entities)
            yield return new Entity(id);
    }

    public IEnumerable<Entity> Query<T>() where T : struct
    {
        foreach (int id in GetStore<T>().Keys)
            yield return new Entity(id);
    }

    public IEnumerable<Entity> Query<T1, T2>() where T1 : struct where T2 : struct
    {
        var store1 = GetStore<T1>();
        var store2 = GetStore<T2>();

        foreach (int id in store1.Keys)
        {
            if (store2.ContainsKey(id))
                yield return new Entity(id);
        }
    }

    public IEnumerable<Entity> Query<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct
    {
        var store1 = GetStore<T1>();
        var store2 = GetStore<T2>();
        var store3 = GetStore<T3>();

        foreach (int id in store1.Keys)
        {
            if (store2.ContainsKey(id) && store3.ContainsKey(id))
                yield return new Entity(id);
        }
    }

    private Dictionary<int, T> GetStore<T>() where T : struct
    {
        var type = typeof(T);
        if (!_componentStores.TryGetValue(type, out var store))
        {
            store = new Dictionary<int, T>();
            _componentStores[type] = store;
        }

        return (Dictionary<int, T>)store;
    }
}
