namespace Intersperse.Tests.Fakes;

public class KeyboardSize
{
    public IKeyboardSize Size { get; }
    public KeyboardSize(IKeyboardSize size)
    {
        Size = size;
    }
}