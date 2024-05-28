using System.IO.Abstractions;
using System.Text.Json;

namespace N2.DataStreams
{
    public class DataStreamManager : IDataStreamManager
    {
        public string FileName { get; set; } = string.Empty;
        public List<StreamConfig> StreamConfig { get; set; } = [];

        private readonly Dictionary<string, StreamConfig> _streams = [];
        private readonly IFileSystem _fileSystem;

        public DataStreamManager()
        {
            _fileSystem = new FileSystem();
        }

        public DataStreamManager(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Configfile { get; set; } = string.Empty;

        public void Initialize()
        {
            var jsonData = _fileSystem.File.ReadAllText(Configfile);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
                IgnoreReadOnlyProperties = false,
                IncludeFields = true,
                //UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip
            };
            var configData = JsonSerializer.Deserialize<DataStreamManager>(jsonData, options);
            if (configData == null)
            {
                throw new System.Exception("Error reading config file");
            }

            FileName = !string.IsNullOrEmpty(configData.FileName) ? configData.FileName : FileName;
            StreamConfig.Clear();
            StreamConfig.AddRange(configData.StreamConfig);
            Configfile = !string.IsNullOrEmpty(configData.Configfile) ? configData.Configfile : Configfile;

            _streams.Clear();
            foreach (var c in StreamConfig)
            {
                var key = c.Name;
                if (_streams.ContainsKey(key))
                {
                    _streams.Remove(key);
                }
                _streams.Add(key, c);
            }
        }

        /// <summary>
        /// Factory for data stream manager.
        /// </summary>
        /// <param name="configFile">The name for the config file</param>
        /// <param name="fileSystem">File system abstraction</param>
        /// <returns></returns>
        public static IDataStreamManager CreateDatastreamManager(IFileSystem fileSystem, string configFile)
        {
            var result = new DataStreamManager(fileSystem);
            result.Configfile = configFile;
            result.Initialize();
            return result;
        }

        public IDataStream CreateClient(string streamName)
        {
            var streamConfig = _streams[streamName];
            return new DataStream(streamConfig);
        }

        public IDataStream CreateClient(StreamType streamType)
        {
            var streamConfig = _streams.Values.FirstOrDefault(m => (m.StreamType & streamType) == streamType);
            return new DataStream(streamConfig);
        }

        public List<string> ListClients()
        {
            return _streams.Keys.ToList();
        }

        public int SaveConfig(string streamName)
        {
            var serialized = JsonSerializer.Serialize(this);
            _fileSystem.File.WriteAllText(streamName, serialized);
            return serialized.Length;
        }
    }
}