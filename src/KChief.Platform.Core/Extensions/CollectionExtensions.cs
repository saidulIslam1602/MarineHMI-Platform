namespace KChief.Platform.Core.Extensions;

/// <summary>
/// Extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Checks if a collection is not null and has items.
    /// </summary>
    public static bool IsNotNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Performs an action for each item in the collection.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        if (collection == null || action == null)
        {
            return;
        }

        foreach (var item in collection)
        {
            action(item);
        }
    }

    /// <summary>
    /// Performs an async action for each item in the collection.
    /// </summary>
    public static async Task ForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> action)
    {
        if (collection == null || action == null)
        {
            return;
        }

        foreach (var item in collection)
        {
            await action(item);
        }
    }

    /// <summary>
    /// Batches a collection into smaller chunks.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
    {
        if (collection == null)
        {
            yield break;
        }

        var batch = new List<T>();
        foreach (var item in collection)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>();
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    /// <summary>
    /// Converts a collection to a dictionary with a key selector.
    /// </summary>
    public static Dictionary<TKey, TValue> ToDictionarySafe<TKey, TValue>(
        this IEnumerable<TValue> collection,
        Func<TValue, TKey> keySelector)
        where TKey : notnull
    {
        if (collection == null || keySelector == null)
        {
            return new Dictionary<TKey, TValue>();
        }

        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var item in collection)
        {
            var key = keySelector(item);
            if (key != null && !dictionary.ContainsKey(key))
            {
                dictionary[key] = item;
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Gets distinct items by a key selector.
    /// </summary>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        if (source == null || keySelector == null)
        {
            yield break;
        }

        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Shuffles a collection randomly.
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        if (collection == null)
        {
            yield break;
        }

        var list = collection.ToList();
        var random = new Random();
        var n = list.Count;

        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }

        foreach (var item in list)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Gets the first item or a default value if collection is empty.
    /// </summary>
    public static T? FirstOrDefaultSafe<T>(this IEnumerable<T>? collection, T? defaultValue = default)
    {
        if (collection == null)
        {
            return defaultValue;
        }

        return collection.FirstOrDefault() ?? defaultValue;
    }

    /// <summary>
    /// Gets items that are not null.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> collection)
        where T : class
    {
        if (collection == null)
        {
            yield break;
        }

        foreach (var item in collection)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Gets items that are not null.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> collection)
        where T : struct
    {
        if (collection == null)
        {
            yield break;
        }

        foreach (var item in collection)
        {
            if (item.HasValue)
            {
                yield return item.Value;
            }
        }
    }
}

