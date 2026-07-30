using Silk.NET.Input;
using Silk.NET.Windowing;

namespace MyEngine.Core;

public sealed class InputState
{
    private readonly IInputContext _context;

    public InputState(IWindow window)
    {
        _context = window.CreateInput();
    }

    public IMouse? PrimaryMouse => _context.Mice.Count > 0 ? _context.Mice[0] : null;
    public IInputContext Context => _context;

    public bool IsKeyDown(Key key)
    {
        foreach (var keyboard in _context.Keyboards)
        {
            if (keyboard.IsKeyPressed(key))
                return true;
        }

        return false;
    }
}
