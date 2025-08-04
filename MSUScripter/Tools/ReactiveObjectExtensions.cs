using System.Collections.Generic;
using System.Linq;
using MSUScripter.Models;
using ReactiveUI;

namespace MSUScripter.Tools;

public static class ReactiveObjectExtensions
{
    public static HashSet<string> GetSkipLastModifiedPropertyNames(this ReactiveObject reactiveObject)
    {
        var toReturn = new HashSet<string>();

        foreach (var property in reactiveObject.GetType().GetProperties())
        {
            if (property.GetCustomAttributes(true).Any(x => x is SkipLastModifiedAttribute))
            {
                toReturn.Add(property.Name);
            }
        }

        return toReturn;
    }
}