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
            Port = FooSyncUrl.DefaultPort;
            Repositories = new List<ServerRepositoryPair>();
        }

        [XmlAttribute]
        public string HostName { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlElement("Repository")]
        public List<ServerRepositoryPair> Repositories { get; private set; }

        #region equality overrides
        public override bool Equals(object obj)
        {
            if (obj is ServerRepositoryList)
            {
                var other = (ServerRepositoryList)obj;
                return (this.HostName == other.HostName && this.Port == other.Port);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HostName.GetHashCode() + Port.GetHashCode();
        }
        #endregion
    }

    public interface RepositoryPair
    {
        Uri RepositoryURL { get; }
        Uri SourceURL { get; }
    }

    [Serializable]
    public class LocalRepositoryPair : RepositoryPair
    {
        [XmlAttribute("Repo")]
        public string RepositoryPath { get; set; }

        [XmlAttribute("Source")]
        public string SourcePath { get; set; }

        [XmlIgnore]
        public Uri RepositoryURL
        {
            get
            {
                return new Uri(
                    new StringBuilder()
                       .Append("file:///")
                       .Append(RepositoryPath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString());
            }
        }

        [XmlIgnore]
        public Uri SourceURL
        {
            get
            {
                return new Uri(
                    new StringBuilder()
                       .Append("file:///")
                       .Append(SourcePath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString());
            }
        }

        #region equality overrides
        public override bool Equals(object obj)
        {
            if (obj is RepositoryPair)
            {
                return this.RepositoryURL == ((RepositoryPair)obj).RepositoryURL
                    && this.SourceURL == ((RepositoryPair)obj).SourceURL;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return RepositoryURL.GetHashCode() + SourceURL.GetHashCode();
        }
        #endregion
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
        public FooSyncUrl RepositoryURL
        {
            get
            {
                return new FooSyncUrl(
                    new StringBuilder()
                      .Append("fs://")
                      .Append(Server.HostName)
                      .Append(":")
                      .Append(Server.Port)
                      .Append("/")
                      .Append(RepositoryName)
                      .ToString());
            }
        }

        [XmlIgnore]
        public Uri SourceURL
        {
            get
            {
                return new Uri(
                    new StringBuilder()
                       .Append("file:///")
                       .Append(LocalPath.Replace(Path.DirectorySeparatorChar, '/'))
                       .ToString());
            }
        }

        #region equality overrides
        public override bool Equals(object obj)
        {
            if (obj is RepositoryPair)
            {
                return this.RepositoryURL == ((RepositoryPair)obj).RepositoryURL
                    && this.SourceURL == ((RepositoryPair)obj).SourceURL;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return RepositoryURL.GetHashCode() + SourceURL.GetHashCode();
        }
        #endregion
    }
}
