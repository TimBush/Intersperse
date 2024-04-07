namespace Intersperse.Tests.Fakes;

public class KeyModule : ModuleBase
{
    public override void Load()
    {
        Bind<IKey, OEM>();
        Bind<IKey, SA>();
    }
}