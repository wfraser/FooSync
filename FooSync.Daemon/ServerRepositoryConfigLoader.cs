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
                serializer.UnknownAttribute += serializer_UnknownAttribute;
                serializer.UnknownElement += serializer_UnknownElement;
                serializer.UnknownNode += serializer_UnknownNode;

                var config = (ServerRepositoryConfig)serializer.Deserialize(reader);

                foreach (var repo in config.RepositoriesList)
                {
                    config.Repositories.Add(repo.Name, repo);
                }

                return config;
            }
        }

        static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown attribute while deserializing {2}: \"{0}\", expecting: {1}",
                    e.Attr, e.ExpectedAttributes, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        static void serializer_UnknownElement(object sender, XmlElementEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown element while deserializing {2}: \"{0}\", expecting: {1}",
                    e.Element, e.ExpectedElements, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown node while deserializing {2}: \"{0}\", type {1}",
                    e.Name, e.NodeType, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        public static void WriteConfig(ServerRepositoryConfig config, string configXmlFilename)
        {
            using (var writer = XmlWriter.Create(configXmlFilename))
            {
                var serializer = new XmlSerializer(typeof(ServerRepositoryConfig));

                config.RepositoriesList.Clear();
                config.RepositoriesList.AddRange(config.Repositories.Values);

                serializer.Serialize(writer, config);
            }
        }
    }
}