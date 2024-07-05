using System;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MSUScripter.Services;

public class YamlService
{
    public static YamlService Instance = null!;
    
    private readonly ISerializer _underscoreSerializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();
    
    private readonly IDeserializer _underscoreDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    
    private readonly ISerializer _pascalSerializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();
    
    private readonly IDeserializer _pascalDeserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ILogger<YamlService> _logger;

    public YamlService(ILogger<YamlService> logger)
    {
        Instance = this;
        _logger = logger;
    }

    public string ToYaml(object obj, bool isUnderscoreFormat)
    {
        return isUnderscoreFormat ? _underscoreSerializer.Serialize(obj) : _pascalSerializer.Serialize(obj);
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
            _logger.LogError(e, "Could not deserialize {Type} from yaml", typeof(T).Name);
            createdObject = default;
            error = e.Message;
            return false;
        }
    }
}