namespace Intersperse.Tests.Fakes;

public class KeyboardSizeModule : ModuleBase
{
    public override void Load()
    {
        Bind<IKeyboardSize, FullKeyboardSize>();
    }
}