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
        public ServerRepositoryDirectory[] Repositories { get; set; }
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
        public string Path { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreGlob { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreRegex { get; set; }

        [XmlElement("AllowedClientKeys")]
        public string[] AllowedClientKeyPaths { get; set; }
    }
}
