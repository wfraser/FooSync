///
/// Codewise/FooSync/Daemon/ServerRepositoryConfig.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Codewise.FooSync;

namespace Codewise.FooSync.Daemon
{
    [Serializable]
    [XmlType(Namespace="http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    [XmlRoot("FooSync.ServerRepositoryConfig", Namespace="http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd", IsNullable=false)]
    public class ServerRepositoryConfig
    {
        [XmlAttribute]
        public string Version { get; set; }

        [XmlElement]
        public string ServerName { get; set; }

        [XmlElement]
        public string ServerDescription { get; set; }

        [XmlElement("Repository")]
        public ServerRepositoryDirectory[] RepositoriesArray { get; set; }

        [XmlIgnore]
        public Dictionary<String, ServerRepositoryDirectory> Repositories { get; set; }
    }

    [Serializable]
    [XmlType(Namespace="http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class ServerRepositoryDirectory
    {
        public override string ToString()
        {
            return Path;
        }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Path { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreGlob { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreRegex { get; set; }

        [XmlElement]
        public ServerRepositoryDirectoryAllowedClientKeys AllowedClientKeys { get; set; }

        [XmlElement]
        public bool AllowAllClients { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class IgnorePatterns : FooSync.IgnorePatterns
    {
        /*
         * Inherited (just changing the XmlType Namespace):
        
        [XmlElement("Pattern")]
        public string[] Patterns { get; set; }

        [XmlAttribute]
        public bool CaseInsensitive { get; set; }
         */
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class ServerRepositoryDirectoryAllowedClientKeys
    {
        [XmlElement("Path")]
        public string[] Paths { get; set; }
    }
}
