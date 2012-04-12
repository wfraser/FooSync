using System;
using System.Text.RegularExpressions;
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

                return config;
            }
        }

        public static void WriteConfig(ServerRepositoryConfig config, string configXmlFilename)
        {
            using (var writer = XmlWriter.Create(configXmlFilename))
            {
                var serializer = new XmlSerializer(typeof(ServerRepositoryConfig));

                serializer.Serialize(writer, config);
            }
        }
    }
}