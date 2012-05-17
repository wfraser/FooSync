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

        [XmlElement]
        public string HashSalt { get; set; }

        [XmlArray]
        public List<UserSpec> Users { get; private set; }

        [XmlArray("Repositories")]
        public List<ServerRepositoryDirectory> RepositoriesList { get; private set; }

        [XmlIgnore]
        public Dictionary<string, ServerRepositoryDirectory> Repositories { get; private set; }

        public ServerRepositoryConfig()
        {
            Users = new List<UserSpec>();
            RepositoriesList = new List<ServerRepositoryDirectory>();
            Repositories = new Dictionary<string, ServerRepositoryDirectory>();
        }
    }

    [Serializable]
    [XmlType(TypeName="User", Namespace="http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class UserSpec
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement]
        public UserSpecPassword Password { get; set; }

        [XmlElement]
        public bool Disabled { get; set; }

        [Serializable]
        public class UserSpecPassword
        {
            [XmlText]
            public string Value { get; set; }

            [XmlAttribute]
            public string Type { get; set; }

            public UserSpecPassword()
            {
                Type = "SHA-512"; // default
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    [XmlType(TypeName="User", Namespace="Http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class UserRef
    {
        [XmlAttribute]
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    [XmlType(TypeName="Repository", Namespace="http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class ServerRepositoryDirectory
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Path { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreGlob { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreRegex { get; set; }

        [XmlArray]
        public List<UserRef> Users { get; private set; }

        public ServerRepositoryDirectory()
        {
            Users = new List<UserRef>();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Path);
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/ServerRepositoryConfig.xsd")]
    public class IgnorePatterns : FooSync.IIgnorePatterns
    {
        /*
         * Inherited (just changing the XmlType Namespace):
         */
        
        [XmlElement("Pattern")]
        public string[] Patterns { get; set; }

        [XmlAttribute]
        public bool CaseInsensitive { get; set; }
    }
    
}
