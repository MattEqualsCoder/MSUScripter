using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MSUScripter.Services;

public class YamlService(ILogger<YamlService> logger)
{
    private readonly Dictionary<YamlType, ISerializer> _serializers = new()
    {
        {
            YamlType.Pascal, new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build()
        },
        {
            YamlType.PascalIgnoreDefaults, new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build()
        },
        {
            YamlType.UnderscoreIgnoreDefaults, new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
        }
    };

    private readonly Dictionary<YamlType, IDeserializer> _deserializers = new()
    {
        {
            YamlType.Pascal, new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build()
        },
        {
            YamlType.PascalIgnoreDefaults, new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build()
        },
        {
            YamlType.UnderscoreIgnoreDefaults, new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build()
        }
    };
    
    public string ToYaml(object obj, YamlType yamlType)
    {
        SanitizeNullStrings(obj, true);
        return _serializers[yamlType].Serialize(obj);
    }

    public bool FromYaml<T>(string yaml, YamlType yamlType, out T? createdObject, out string? error)
    {
        try
        {
            createdObject = _deserializers[yamlType].Deserialize<T>(yaml);
            SanitizeNullStrings(createdObject, false);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not deserialize {Type} from yaml", typeof(T).Name);
            createdObject = default;
            error = e.Message;
            return false;
        }
    }

    public void SanitizeNullStrings(object? obj, bool sanitize)
    {
        if (obj == null || obj.GetType().IsPrimitive) return; 
        
        foreach (var prop in obj.GetType().GetProperties())
        {
            if (prop.PropertyType == typeof(string))
            {
                var value = prop.GetValue(obj) as string;
                if (sanitize && "null".Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    prop.SetValue(obj, $"`{value}`");
                }
                else if (!sanitize && "`null`".Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    prop.SetValue(obj, value.Replace("`", ""));
                }
            }
            else if (prop.PropertyType.Name.StartsWith("List`"))
            {
                var list = prop.GetValue(obj) as IEnumerable<object?> ?? [];
                foreach (var item in list)
                {
                    SanitizeNullStrings(item, sanitize);
                }
            }
            else if (prop.PropertyType.IsClass)
            {
                try
                {
                    var newObj = prop.GetValue(obj);
                    SanitizeNullStrings(newObj, sanitize);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error sanitizing null strings");
                }
                
            }
        }
    }
    
}

public enum YamlType
{
    UnderscoreIgnoreDefaults,
    Pascal,
    PascalIgnoreDefaults
}