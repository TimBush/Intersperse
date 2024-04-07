using Intersperse.Tests.Fakes;

namespace Intersperse.Tests;

public class KernelTests
{
    [Fact]
    public void TwoCtor_MultiInjection_ResolvesTwoConcreteTypes()
    {
        var module = new KeyModule();
        var kernel = new Kernel(module);

        var keyboard = kernel.GetClassWithValuesInjected<Keyboard>();
        Assert.Equal(2, keyboard.Keys.Length);
        Assert.Equal(typeof(OEM), keyboard.Keys[0].GetType());
        Assert.Equal(typeof(SA), keyboard.Keys[1].GetType());
    }
    
    [Fact]
    public void SingleCtor_Inheritance_MultiInjection_ResolvesTwoConcreteTypes()
    {
        var module = new KeyModule();
        var kernel = new Kernel(module);

        var mechanicalKeyboard = kernel.GetClassWithValuesInjected<MechanicalKeyboard>();
        Assert.Equal(2, mechanicalKeyboard.Keys.Length);
        Assert.Equal(typeof(OEM), mechanicalKeyboard.Keys[0].GetType());
        Assert.Equal(typeof(SA), mechanicalKeyboard.Keys[1].GetType());
    }
    
    [Fact]
    public void TwoCtor_SingleInjectAttrApplied_MultiInjection_ResolvesInjectAttrCtorWithTwoConcreteTypes()
    {
        var module = new KeyModule();
        var kernel = new Kernel(module);

        var membraneKeyboard = kernel.GetClassWithValuesInjected<MembraneKeyboard>();
        Assert.Equal(2, membraneKeyboard.Keys.Length);
    }
    
    [Fact]
    public void TwoCtor_SingleParamInjection_ResolvesOneConcreteType()
    {
        var module = new KeyboardSizeModule();
        var kernel = new Kernel(module);

        var keyboardSize = kernel.GetClassWithValuesInjected<KeyboardSize>();
        Assert.Equal(100, keyboardSize.Size.NumOfKeys);
    }
    
    [Fact]
    public void MultipleCtor_MultipleInjectAttr_ThrowsNotSupportedException()
    {
        var module = new KeyModule();
        var kernel = new Kernel(module);
        
        Assert.Throws<NotSupportedException>(() => kernel.GetClassWithValuesInjected<ErgonomicKeyboard>());
    }

    [Fact]
    public void Kernel_GetAll_SingleModule_Works()
    {
        var module = new KeyModule();
        var kernel = new Kernel(module);

        var keys = kernel.GetAll<IKey>(true);
        
        Assert.Equal(2, keys.Length);
    }
    
    [Fact]
    public void Kernel_GetAll_TwoModules()
    {
        var keyModule = new KeyModule();
        var keyboardSizeModule = new KeyboardSizeModule();
        var kernel = new Kernel(keyModule, keyboardSizeModule);

        var keys = kernel.GetAll<IKey>(true);
        var keyboardSize = kernel.GetAll<IKeyboardSize>(true);
        
        Assert.Equal(2, keys.Length);
        Assert.Single(keyboardSize);
    }
    
    [Fact]
    public void Kernel_TwoModules_OneGetAll_OneGet()
    {
        var keyModule = new KeyModule();
        var keyboardSizeModule = new KeyboardSizeModule();
        var kernel = new Kernel(keyModule, keyboardSizeModule);

        var keys = kernel.GetAll<IKey>(true);
        var keyboardSize = kernel.Get<IKeyboardSize>(true);
        
        Assert.Equal(2, keys.Length);
        Assert.Equal(typeof(FullKeyboardSize), keyboardSize.GetType());
    }
}