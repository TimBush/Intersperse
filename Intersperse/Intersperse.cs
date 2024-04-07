using System.Reflection;

namespace Intersperse;

public class Intersperse
{
    private readonly IList<Type> _registeredTypes = new List<Type>();

    public void Add<I, C>()
    {
        if (!typeof(I).IsAssignableFrom(typeof(C))) return;
        var type = typeof(I);
        _registeredTypes.Add(type);
    }

    /// <summary>
    /// Gets an interface passed to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T Get<T>()
    {
        if (!_registeredTypes.Contains(typeof(T)))
            throw new InvalidOperationException("Type is not dependency injected.");
        
        var concreteType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .First(concreteType => typeof(T).IsAssignableFrom(concreteType) && concreteType.BaseType is not null); // Is T an interface that concreteType implements

        return (T)concreteType.GetConstructor(Type.EmptyTypes)
            .Invoke(Array.Empty<object>());
    }

    public void Dispose<T>()
    {
        _registeredTypes.Remove(typeof(T));
    }
}