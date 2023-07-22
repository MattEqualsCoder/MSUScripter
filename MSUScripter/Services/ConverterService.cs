using System;
using System.Linq;

namespace MSUScripter.Services;

public class ConverterService
{
    public bool ConvertViewModel<A, B>(A input, B output)
    {
        var propertiesA = typeof(A).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x);
        var propertiesB = typeof(B).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x);
        var updated = false;

        if (propertiesA.Count != propertiesB.Count)
        {
            throw new InvalidOperationException($"Types {typeof(A).Name} and {typeof(B).Name} are not compatible");
        }

        foreach (var propA in propertiesA.Values)
        {
            if (!propertiesB.TryGetValue(propA.Name, out var propB))
            {
                continue;
            }

            var value = propA.GetValue(input);
            var originalValue = propA.GetValue(input);
            updated |= value != originalValue;
            propB.SetValue(output, value);
        }

        return updated;
    }
}