namespace Intersperse.Tests.Fakes;

public class Size
{
    public ISize Size { get; }
    public Size(ISize size)
    {
        Size = size;
    }
}