namespace MyEngine.ECS;

/// <summary>A simulation system driven by the fixed-tick update loop.</summary>
public interface ISystem
{
    void Update(World world, double fixedDeltaTime);
}
