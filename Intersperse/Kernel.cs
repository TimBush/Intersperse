using System.Reflection;
using System.Runtime.InteropServices;
using Type = System.Type;

namespace Intersperse;

public class Kernel
{
    private readonly ModuleBase[] _modules;
    private IList<Type> _registeredTypes = new List<Type>();
    private IList<Type> _registeredConcreteTypes = new List<Type>();
    
    public Kernel(params ModuleBase[] modules)
    {
        _modules = modules;
        Load();
    }

    public void Add<I, C>()
    {
        if (!typeof(I).IsAssignableFrom(typeof(C))) return;
        var type = typeof(I);
        _registeredTypes.Add(type);
    }

    public void Dispose<T>()
    {
        _registeredTypes.Remove(typeof(T));
    }

    public TConcrete GetClassWithValuesInjected<TConcrete>()
    {
        var typeOfConcrete = typeof(TConcrete);
        var constructors = typeOfConcrete.GetConstructors().ToList();
        
        // 1. If we've found a single ctor w/ the inject attr, resolve params and then invoke
        // 2. Otherwise, fall back
        var constructorsWithInjectAttr = FindCtorWithInjectAttr(typeOfConcrete);
        
        
        if (constructorsWithInjectAttr is not null)
        {
            Console.WriteLine("A single ctor with an inject attr was found, executing.");
            
            var ctorParamTypes = constructorsWithInjectAttr.GetParameters().Select(pi => pi.ParameterType);
            var paramConcreteTypes = ResolveParametersWithRegisteredTypes(ctorParamTypes).ToArray();
            if (!paramConcreteTypes.Any())
                throw new InvalidOperationException(
                    $"'{nameof(TConcrete)}' had a single constructor marked with the 'Inject' attribute, but all parameters have not been registered.");
            
            return (TConcrete)constructorsWithInjectAttr.Invoke(paramConcreteTypes);
        }
        
        Console.WriteLine("No inject attr, falling back...");
        // If no constructorsWithInjectAttr was found, then fall back to just look for ctor with the most params we can inject to
        var (ctor, concreteParams) = FindCtorWithMostMatchingParamsWeCanInject(constructors);

        if (ctor is null)
            throw new NotSupportedException(
                $"'{typeof(TConcrete).Name}' has no valid constructors that support dependency injection for all specified parameters.");
        
        
        return (TConcrete)ctor.Invoke(concreteParams.ToArray());
    }
    
    /// <summary>
    /// Get a concrete type that implements TInterface.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal TInterface Get<TInterface>(bool shouldThrowIfNoRegisteredType)
    {
        if (shouldThrowIfNoRegisteredType)
        {
            if (!_registeredTypes.Contains(typeof(TInterface)))
                throw new InvalidOperationException("Type is not dependency injected.");
        }
        
        Type concreteType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .First(concreteType => typeof(TInterface).IsAssignableFrom(concreteType) && concreteType.BaseType is not null); // Is T an interface that concreteType implements
        
        // TODO: It could be the case where the concrete type that is mapped to TInterface
        // Actually has a ctor that takes some values during init
        ConstructorInfo? constructor = concreteType.GetConstructor(Type.EmptyTypes);

        return constructor is null
            ? throw new Exception($"A constructor that takes 0 arguments was not found for: {concreteType.Name}")
            : (TInterface)constructor.Invoke(Array.Empty<object>());
    }

    internal TInterface[] GetAll<TInterface>(bool shouldThrowIfNoRegisteredType)
    {
        if (shouldThrowIfNoRegisteredType)
        {
            if (!_registeredTypes.Contains(typeof(TInterface)))
                throw new InvalidOperationException("Type is not dependency injected.");
        }

        // Get all concrete types that implement this interface
        IEnumerable<Type> concreteTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(concreteType => 
                typeof(TInterface).IsAssignableFrom(concreteType) 
                && concreteType.BaseType is not null
                && _registeredConcreteTypes.Contains(concreteType)); // Is T an interface that concreteType implements

        List<TInterface> allInitializedClasses = new();
        foreach (var concreteType in concreteTypes)
        {
            // TODO: It could be the case where the concrete type that is mapped to TInterface
            // Actually has a ctor that takes some values during init
            var ctor = concreteType.GetConstructor(Type.EmptyTypes);
            
            // TODO: How does it affect client code in this case?
            if (ctor is null) continue;

            var initializedClass = (TInterface)ctor.Invoke(Array.Empty<object>());
            allInitializedClasses.Add(initializedClass);
        }

        return allInitializedClasses.ToArray();
    }

    private (ConstructorInfo? ctor, IEnumerable<object> concreteParams) FindCtorWithMostMatchingParamsWeCanInject(IEnumerable<ConstructorInfo> constructors)
    {
        (ConstructorInfo? ctor, IEnumerable<object> concreteParams) ctorToUse = (null, new List<object>());
        foreach (var ctor in constructors)
        {
            Console.WriteLine($"Evaluating Ctor: {ctor}");
            var parameterTypes = ctor.GetParameters().Select(pi => pi.ParameterType).ToList();
            
            // Now try to resolve each parameterType
            var registeredConcreteTypesMappedToParameter =
                ResolveParametersWithRegisteredTypes(parameterTypes).ToList();
            
            // Current ctor couldn't have its params mapped, it's not a candidate, so move on
            if (!registeredConcreteTypesMappedToParameter.Any()) continue;
            
            // Have we found a better candidate for a ctor
            if (ctorToUse.ctor is null || ctorToUse.concreteParams.Count() < registeredConcreteTypesMappedToParameter.Count)
                ctorToUse = (ctor, registeredConcreteTypesMappedToParameter);
        }
        
        // Two things can happen here
        // 1. ctorToUser.ctor is null, in which case we have no valid ctor for ANY ctors on this class
        // 2. ctorToUser.ctor is not null, in which case we've found the best option that we can safely inject values into
        Console.WriteLine($"The best ctor option is: {ctorToUse.ctor}");
        return ctorToUse;
    }

    private IEnumerable<object> ResolveParametersWithRegisteredTypes(IEnumerable<Type> parameterTypes)
    {
        
        List<object> registeredConcreteTypesMappedToParameter = new();
        foreach (var type in parameterTypes)
        {
            if (type.IsArray)
            {
                var typesWithinArray = type.GetElementType();
                var listOfInitializedClasses = CallGetAllMethodOnKernelForType(typesWithinArray);
                
                if (listOfInitializedClasses is null) break;
                registeredConcreteTypesMappedToParameter.Add(listOfInitializedClasses);
                continue;
            }
            // If 'type' has a BaseType we know that it is a concrete type, so don't do anything with it
            // Alternatively, we could allow users to pass params for these concrete types.
            // May need to totally exclude any ctor values that are not interface, since we don't know what
            // value to use at the moment
            if (type.BaseType is not null) break;

            var registeredType = CallGetMethodOnKernelForType(type);
            
            // If a Type is not bound, an error will be thrown from Get<T>, however, look at cases where this may be null
            // May want to allow throwing if the type is not mapped, in this case
            // instead, if the param is not mapped, we won't be able to init the ctor, since we don't know what the type
            // is, so we can't do anything
            if (registeredType is null) break;
            // Now we invoke the first ctor of TConcrete. E.g if it's 'Keyboard', this is like calling new Keyboard(methodOutput), where methodOutput == OEM
            //var output = (TConcrete)constructorInfo[0].Invoke(new[] { methodOutput });
            registeredConcreteTypesMappedToParameter.Add(registeredType);
        }
        
        // Two things could happen here
        // 1. count == 0, meaning we can't use the ctor that has these params
        // 2. All params are mapped, which means it's a candidate for use
        return registeredConcreteTypesMappedToParameter;
    }

    private object? CallGetMethodOnKernelForType(Type type)
    {
        MethodInfo kernelGetMethod = KernelGetMethodViaReflection();
        return InvokeGetMethods(kernelGetMethod, type);
    }
    
    private object? CallGetAllMethodOnKernelForType(Type type)
    {
        MethodInfo kernelGetAllMethod = KernelGetAllMethodViaReflection();
        return InvokeGetMethods(kernelGetAllMethod, type);
    }

    private object? InvokeGetMethods(MethodInfo methodInfo, Type genericTypeParam) 
    {
        // methodOutput should be an Interface here that has already been mapped, in this case it would be a concrete type.
        return methodInfo.MakeGenericMethod(genericTypeParam)
            .Invoke(this, new object[] { false });
    }

    private ConstructorInfo? FindCtorWithInjectAttr(Type type)
    {
        var constructors = type.GetConstructors().ToList();
        var constructorsWithInjectAttr = constructors.Where(ctor =>
        {
            var attrs = ctor.GetCustomAttributes(typeof(InjectAttribute));
            return attrs.Any();
        }).ToList();
        Console.WriteLine($"Constructors found with 'Inject' Attr: {constructorsWithInjectAttr.Count}");

        if (constructorsWithInjectAttr.Count > 1)
            throw new NotSupportedException(
                $"'{type.Name}' has more than one {nameof(InjectAttribute)} applied to its constructors.");


        return constructorsWithInjectAttr.FirstOrDefault();
    }

    private MethodInfo KernelGetMethodViaReflection()
    {
        return typeof(Kernel).GetMethod(nameof(Get), BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(bool) })!;
    }
    
    private MethodInfo KernelGetAllMethodViaReflection()
    {
        return typeof(Kernel).GetMethod(nameof(GetAll), BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(bool) })!;
    }

    private void Load()
    {
        foreach (ModuleBase module in _modules)
        {
            module.Load();
            _registeredTypes = _registeredTypes.Concat(module.RegisteredTypes).ToList();
            _registeredConcreteTypes = _registeredConcreteTypes.Concat(module.RegisteredConcreteTypes).ToList();
        }
    }
}