using System;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MSUScripter.Services;

public class YamlService(ILogger<YamlService> logger)
{
    private readonly ISerializer _underscoreSerializerIgnoreDefaults = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();
    
    private readonly ISerializer _underscoreSerializerAll = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();
    
    private readonly IDeserializer _underscoreDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    
    private readonly ISerializer _pascalSerializerIgnoreDefaults = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();
    
    private readonly ISerializer _pascalSerializerAll = new SerializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();
    
    private readonly IDeserializer _pascalDeserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string ToYaml(object obj, bool isUnderscoreFormat, bool ignoreDefaults)
    {
        if (ignoreDefaults)
        {
            return isUnderscoreFormat ? _underscoreSerializerIgnoreDefaults.Serialize(obj) : _pascalSerializerIgnoreDefaults.Serialize(obj);    
        }
        else
        {
            return isUnderscoreFormat ? _underscoreSerializerAll.Serialize(obj) : _pascalSerializerAll.Serialize(obj);
        }
    }

    public bool FromYaml<T>(string yaml, out T? createdObject, out string? error, bool isUnderscoreFormat)
    {
        try
        {
            createdObject = isUnderscoreFormat
                ? _underscoreDeserializer.Deserialize<T>(yaml)
                : _pascalDeserializer.Deserialize<T>(yaml);
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
}