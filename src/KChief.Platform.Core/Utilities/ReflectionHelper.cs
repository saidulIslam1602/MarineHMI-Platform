using System.Reflection;

namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Helper class for reflection operations.
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// Gets all types that implement a specific interface.
    /// </summary>
    public static IEnumerable<Type> GetTypesImplementing<TInterface>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => typeof(TInterface).IsAssignableFrom(t) && 
                      t.IsClass && 
                      !t.IsAbstract);
    }

    /// <summary>
    /// Gets all types that inherit from a specific base class.
    /// </summary>
    public static IEnumerable<Type> GetTypesInheritingFrom<TBase>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => typeof(TBase).IsAssignableFrom(t) && 
                      t.IsClass && 
                      !t.IsAbstract &&
                      t != typeof(TBase));
    }

    /// <summary>
    /// Gets property value by name.
    /// </summary>
    public static object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets property value by name.
    /// </summary>
    public static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        if (obj == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
    }

    /// <summary>
    /// Gets all properties with a specific attribute.
    /// </summary>
    public static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(Type type)
        where TAttribute : Attribute
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttribute<TAttribute>() != null);
    }

    /// <summary>
    /// Creates an instance of a type.
    /// </summary>
    public static T? CreateInstance<T>(params object[] args)
    {
        try
        {
            return (T?)Activator.CreateInstance(typeof(T), args);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Checks if a type has a specific attribute.
    /// </summary>
    public static bool HasAttribute<TAttribute>(Type type) where TAttribute : Attribute
    {
        return type.GetCustomAttribute<TAttribute>() != null;
    }
}

