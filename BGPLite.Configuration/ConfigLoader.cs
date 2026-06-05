using YamlDotNet.Serialization;

namespace BGPLite.Configuration;

public static class ConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public static AppConfig Load(string path)
    {
        var yaml = File.ReadAllText(path);
        return Deserializer.Deserialize<AppConfig>(yaml);
    }

    public static AppConfig LoadFromText(string yaml) =>
        Deserializer.Deserialize<AppConfig>(yaml);

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .Build();

    public static string Save(AppConfig config) =>
        Serializer.Serialize(config);
}
