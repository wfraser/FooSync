///
/// Codewise/FooSync/ConsoleApp/SyncGropConfig.cs
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

namespace Codewise.FooSync.ConsoleApp
{
    [Serializable]
    [XmlType(Namespace="http://www.codewise.org/schema/foosync/SyncGroupConfig.xsd")]
    [XmlRoot("FooSyncGroup", Namespace="http://www.codewise.org/schema/foosync/SyncGroupConfig.xsd", IsNullable=false)]
    public class SyncGroupConfig
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Version { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreGlob { get; set; }

        [XmlElement]
        public IgnorePatterns IgnorePatterns { get; set; }

        [XmlArray]
        [XmlArrayItem("Member")]
        public List<SyncGroupConfigMember> Members { get; private set; }

        public SyncGroupConfig()
        {
            Members = new List<SyncGroupConfigMember>();
        }
    }

    [Serializable]
    public class SyncGroupConfigMember
    {
        [XmlAttribute]
        public string URL { get; set; }

        [XmlAttribute]
        public string Host { get; set; }

        [XmlElement]
        public SyncGroupConfigMemberAuth Auth { get; set; }
    }

    [Serializable]
    public class SyncGroupConfigMemberAuth
    {
        [XmlAttribute]
        public string User { get; set; }

        [XmlAttribute]
        public string Password { get; set; }

        [XmlIgnore]
        public bool PromptForPassword
        {
            get { return (Password == null); }
        }
    }

    [Serializable]
    public class IgnorePatterns : FooSync.IIgnorePatterns
    {
        [XmlElement("Pattern")]
        public string[] Patterns { get; set; }

        [XmlAttribute]
        public bool CaseInsensitive { get; set; }
    }
}
