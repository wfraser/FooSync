using System;
using System.Xml.Serialization;

namespace FooSync
{
    [Serializable]
    [XmlType(Namespace="http://www.codewise.org/schema/foosync/RepositoryConfig.xsd")]
    [XmlRoot("FooSync", Namespace="http://www.codewise.org/schema/foosync/RepositoryConfig.xsd", IsNullable=false)]
    public class RepositoryConfig
    {
        [XmlElement("Directory")]
        public RepositoryDirectory[] Directories { get; set; }

        [XmlAttribute]
        public string Version { get; set; }

        [XmlAttribute]
        public string RepositoryName { get; set; }

        [XmlIgnore]
        public string Filename { get; set; }

        [XmlIgnore]
        public string RepositoryPath { get { return System.IO.Path.GetDirectoryName(Filename); } }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/RepositoryConfig.xsd")]
    public class RepositoryDirectory
    {
        public override string ToString()
        {
            if (Path == ".")
            {
                return "(main directory)";
            }
            else
            {
                return Path;
            }
        }

        [XmlElement]
        public string Path { get; set; }

        [XmlElement("Source")]
        public RepositorySource[] Sources { get; set; }

        /// <summary>
        /// Contains the RepositorySource corresponding to the current machine, or null if one does not exist.
        /// </summary>
        [XmlIgnore]
        public RepositorySource Source
        {
            get
            {
                if (sourceField == null)
                {
                    foreach (RepositorySource s in Sources)
                    {
                        if (s.Name.Equals(System.Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                        {
                            sourceField = s;
                            break;
                        }
                    }
                }

                return sourceField;
            }
        }

        [XmlElement]
        public IgnorePatterns IgnoreGlob { get; set; }

        [XmlElement]
        public IgnorePatterns IgnoreRegex { get; set; }

        private RepositorySource sourceField;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/RepositoryConfig.xsd")]
    public class RepositorySource
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement]
        public string Path { get; set; }

        /* Maybe support this later...
        [XmlElement]
        public MergeStrategy MergeStrategy { get; set; }
         */
    }

    [Serializable]
    [XmlType(Namespace = "http://www.codewise.org/schema/foosync/RepositoryConfig.xsd")]
    public class IgnorePatterns
    {
        [XmlElement("Pattern")]
        public string[] Patterns { get; set; }

        [XmlAttribute]
        public bool CaseInsensitive { get; set; }
    }

    [Serializable]
    public enum MergeStrategy
    {
        Synchronize,
        Theirs,
        Ours
    }
}
