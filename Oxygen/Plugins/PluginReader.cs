using System.Text.Json;

namespace Oxygen
{
    internal class PluginReader
    {
        private readonly List<Plugin> plugins = new List<Plugin>();

        public IList<Plugin> Plugins => plugins;

        public void Load(byte[] pluginData)
        {
            Utf8JsonReader reader = new Utf8JsonReader(pluginData);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.GetString() == "Plugins")
                        {
                            LoadPlugins(ref reader);
                        }
                        break;
                }
            }
        }

        private static void Advance(ref Utf8JsonReader reader)
        {
            do
            {
                if (!reader.Read())
                {
                    throw new JsonException();
                }
            } while (reader.TokenType == JsonTokenType.Comment);
        }

        private void LoadPlugins(ref Utf8JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            Advance(ref reader);

            while (reader.TokenType == JsonTokenType.StartObject)
            {
                LoadPlugin(ref reader);

                Advance(ref reader);
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }
        }

        private void LoadPlugin(ref Utf8JsonReader reader)
        {
            Advance(ref reader);

            Plugin plugin = new Plugin();

            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "Name":
                        Advance(ref reader);
                        plugin.Name = reader.GetString();
                        break;
                    case "ManualStart":
                        Advance(ref reader);
                        plugin.ManualStart = reader.GetBoolean();
                        break;
                    case "Type":
                        Advance(ref reader);
                        plugin.Type = reader.GetString();
                        break;
                    case "Filter":
                        Advance(ref reader);
                        plugin.Filter = reader.GetString();
                        break;
                    case "Package":
                        Advance(ref reader);
                        plugin.Package = reader.GetString();
                        break;
                    case "Actions":
                        Advance(ref reader);
                        LoadActions(plugin, ref reader);
                        break;
                    case "Artefacts":
                        Advance(ref reader);
                        LoadArtefacts(plugin, ref reader);
                        break;
                    default:
                        throw new JsonException();
                }

                Advance(ref reader);
            }

            this.plugins.Add(plugin);

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }
        }

        private void LoadActions(Plugin plugin, ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            Advance(ref reader);
            while (reader.TokenType == JsonTokenType.StartObject)
            {
                plugin.AddAction(LoadAction(ref reader));

                Advance(ref reader);
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }
        }

        private static PluginAction LoadAction(ref Utf8JsonReader reader)
        {
            PluginAction action = new PluginAction();

            Advance(ref reader);
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "Run":
                        Advance(ref reader);
                        action.Run = reader.GetString();
                        break;
                    case "Args":
                        Advance(ref reader);
                        action.Args = reader.GetString();
                        break;
                }

                Advance(ref reader);
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return action;
        }

        private static void LoadArtefacts(Plugin plugin, ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            Advance(ref reader);
            while (reader.TokenType == JsonTokenType.String)
            {
                string? artefact = reader.GetString();
                if (artefact != null)
                {
                    plugin.AddArtefact(artefact);
                }

                Advance(ref reader);
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }
        }
    }
}
