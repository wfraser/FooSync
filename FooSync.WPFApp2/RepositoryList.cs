///
/// Codewise/FooSync/WPFApp2/RepositoryList.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Codewise.FooSync.WPFApp2
{
    [Serializable]
    public class RepositoryList : INotifyCollectionChanged
    {
        public RepositoryList()
        {
            LocalPaths = new ObservableCollection<LocalRepositoryPair>();
            Servers    = new ObservableCollection<ServerRepositoryList>();

            LocalPaths.CollectionChanged += new NotifyCollectionChangedEventHandler(Child_CollectionChanged);
            Servers.CollectionChanged += new NotifyCollectionChangedEventHandler(Child_CollectionChanged);
        }

        void Child_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [XmlArray]
        [XmlArrayItem("Path")]
        public ObservableCollection<LocalRepositoryPair> LocalPaths { get; private set; }

        [XmlElement("Server")]
        public ObservableCollection<ServerRepositoryList> Servers { get; private set; }

        public static RepositoryList ReadFromFile(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(RepositoryList));

            var repoList = (RepositoryList)serializer.Deserialize(stream);

            foreach (var server in repoList.Servers)
            {
                foreach (var repo in server.Repositories)
                {
                    repo.Server = server;
                }
            }

            return repoList;
        }

        public void WriteToFile(FileStream stream)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(RepositoryList));

                serializer.Serialize(writer, this);
            }
        }
    }

    [Serializable]
    public class ServerRepositoryList
    {
        public ServerRepositoryList()
        {
            Port = 22022;
            Repositories = new List<ServerRepositoryPair>();
        }

        [XmlAttribute]
        public string HostName { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlElement("Repository")]
        public List<ServerRepositoryPair> Repositories { get; private set; }
    }

    public interface RepositoryPair
    {
        string RepositoryURL { get; }
        string SourceURL { get; }
    }

    [Serializable]
    public class LocalRepositoryPair : RepositoryPair
    {
        [XmlAttribute("Repo")]
        public string RepositoryPath { get; set; }

        [XmlAttribute("Source")]
        public string SourcePath { get; set; }

        [XmlIgnore]
        public string RepositoryURL
        {
            get
            {
                return new StringBuilder()
                       .Append("file:///")
                       .Append(RepositoryPath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString();
            }
        }

        [XmlIgnore]
        public string SourceURL
        {
            get
            {
                return new StringBuilder()
                       .Append("file:///")
                       .Append(SourcePath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString();
            }
        }
    }

    [Serializable]
    public class ServerRepositoryPair
    {
        [XmlAttribute("Name")]
        public string RepositoryName { get; set; }

        [XmlAttribute()]
        public string LocalPath { get; set; }

        [XmlIgnore]
        public ServerRepositoryList Server { get; set; }

        [XmlIgnore]
        public string RepositoryURL
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("fs://")
                  .Append(Server.HostName);

                if (Server.Port != 22022)
                    sb.Append(":").Append(Server.Port);

                sb.Append("/").Append(RepositoryName);

                return sb.ToString();
            }
        }

        [XmlIgnore]
        public string SourceURL
        {
            get
            {
                return new StringBuilder()
                       .Append("file:///")
                       .Append(LocalPath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString();
            }
        }
    }
}
