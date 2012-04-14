using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Codewise.FooSync.Daemon
{
    public static class ServerRepositoryConfigLoader
    {
        const string ServerRepositoryConfigXSD = "ServerRepositoryConfig.xsd";

        public static ServerRepositoryConfig GetConfig(string configXmlFilename, out string error)
        {
            if (!RepositoryConfigLoader.ValidateAgainstSchema(
                    configXmlFilename,
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        ServerRepositoryConfigXSD),
                    out error))
            {
                return null;
            }

            using (var reader = XmlReader.Create(configXmlFilename))
            {
                var serializer = new XmlSerializer(typeof(ServerRepositoryConfig));

                var config = (ServerRepositoryConfig)serializer.Deserialize(reader);

                config.Repositories = new System.Collections.Generic.Dictionary<string, ServerRepositoryDirectory>();
                foreach (var repo in config.RepositoriesArray)
                {
                    config.Repositories.Add(repo.Name, repo);
                }

                return config;
            }
        }

        public static void WriteConfig(ServerRepositoryConfig config, string configXmlFilename)
        {
            using (var writer = XmlWriter.Create(configXmlFilename))
            {
                var serializer = new XmlSerializer(typeof(ServerRepositoryConfig));

                config.RepositoriesArray = new ServerRepositoryDirectory[config.Repositories.Count];
                config.Repositories.Values.CopyTo(config.RepositoriesArray, 0);

                serializer.Serialize(writer, config);
            }
        }
    }
}