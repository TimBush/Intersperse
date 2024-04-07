namespace Intersperse.Tests.Fakes;

public class Keyboard
{
    public IKey[] Keys;

    public Keyboard()
    {
        
    }
    
    public Keyboard(IKey[] keys)
    {
        Keys = keys;
    }
}

public class MechanicalKeyboard : Keyboard
{
    public MechanicalKeyboard(IKey[] keys) : base(keys)
    {
        
    }
}

public class MembraneKeyboard
{
    public IKey? Key;
    public IKey[]? Keys;

    public MembraneKeyboard(IKey key)
    {
        Key = key;
        Keys = null;
    }
    
    [Inject]
    public MembraneKeyboard(IKey[] keys)
    {
        Keys = keys;
        Key = null;
    }
}

public class ErgonomicKeyboard
{
    [Inject]
    public ErgonomicKeyboard(IKey[] keys)
    {
        
    }
    
    [Inject]
    public ErgonomicKeyboard(IKey keys)
    {
        
    }
    
    [Inject]
    public ErgonomicKeyboard(IKeyboardSize keyboardKeyboardSize)
    {
        
    }
}

public class OrtholinearKeyboard
{
    public OrtholinearKeyboard(IKey[] keys, IKey key)
    {
        
    }
}