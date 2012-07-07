using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Codewise.FooSync;

namespace Codewise.FooSync.Daemon
{
    public static class ServerRepositoryConfigLoader
    {
        const string ServerRepositoryConfigXSD = "ServerRepositoryConfig.xsd";

        public static ServerRepositoryConfig GetConfig(string configXmlFilename, out string error)
        {
            if (!XmlConfigLoader.Validate(
                    configXmlFilename,
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        ServerRepositoryConfigXSD),
                    out error))
            {
                return null;
            }

            try
            {
                ServerRepositoryConfig config = XmlConfigLoader.Load<ServerRepositoryConfig>(configXmlFilename);

                foreach (var repo in config.RepositoriesList)
                {
                    config.Repositories.Add(repo.Name, repo);
                }

                return config;
            }
            catch (Exception ex)
            {
                error = string.Format("{0} ({1})", ex.Message, ex.GetType().Name);
                return null;
            }
        }

        public static void WriteConfig(ServerRepositoryConfig config, string configXmlFilename)
        {
            config.Repositories.Clear();
            config.RepositoriesList.AddRange(config.Repositories.Values);

            XmlConfigLoader.Write<ServerRepositoryConfig>(config, configXmlFilename);
        }
    }
}
