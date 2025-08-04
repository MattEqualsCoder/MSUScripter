using System.Security.AccessControl;

namespace MSUScripter.Models;

[
    System.AttributeUsage(System.AttributeTargets.Property |  System.AttributeTargets.Class),
]
public class SkipLastModifiedAttribute : System.Attribute
{
}