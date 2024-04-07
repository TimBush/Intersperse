namespace Intersperse;

public abstract class ModuleBase
{
    internal IList<Type> RegisteredTypes { get; } = new List<Type>();
    internal IList<Type> RegisteredConcreteTypes { get; } = new List<Type>();
    
    public abstract void Load();

    protected void Bind<TInterface, TConcreteType>()
    {
        var type = typeof(TInterface);
        if (!type.IsAssignableFrom(typeof(TConcreteType))) return;
        
        if (!RegisteredTypes.Contains(type)) RegisteredTypes.Add(type);
        
        RegisteredConcreteTypes.Add(typeof(TConcreteType));
    }
}