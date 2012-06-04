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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Codewise.FooSync.WPFApp2
{
    [Serializable]
    [XmlType("SyncGroupList", Namespace="http://www.codewise.org/schema/foosync/SyncGroupList.xsd")]
    public class SyncGroupList : INotifyCollectionChanged
    {
        public SyncGroupList()
        {
            SyncGroups = new ObservableCollection<SyncGroup>();
            Servers    = new ObservableCollection<ServerRepositoryList>();

            SyncGroups.CollectionChanged += new NotifyCollectionChangedEventHandler(Child_CollectionChanged);
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
        [XmlArrayItem("SyncGroup")]
        public ObservableCollection<SyncGroup> SyncGroups { get; private set; }

        [XmlArray]
        [XmlArrayItem("Server")]
        public ObservableCollection<ServerRepositoryList> Servers { get; private set; }

        public static SyncGroupList ReadFromFile(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(SyncGroupList), "http://www.codewise.org/schema/foosync/SyncGroupList.xsd");

            var list = (SyncGroupList)serializer.Deserialize(stream);

            foreach (var server in list.Servers)
            {
                foreach (var repo in server.Repositories)
                {
                    repo.Server = server;
                }
            }

            return list;
        }

        public void WriteToFile(FileStream stream)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(SyncGroupList), "http://www.codewise.org/schema/foosync/SyncGroupList.xsd");

                serializer.Serialize(writer, this);
            }
        }
    }

    [Serializable]
    [XmlType("ServerRepositoryList", Namespace="http://www.codewise.org/schema/foosync/SyncGroupList.xsd")]
    public class ServerRepositoryList
    {
        public ServerRepositoryList()
        {
            Port = FooSyncUrl.DefaultPort;
            Repositories = new List<ServerRepository>();
        }

        [XmlAttribute]
        public string Hostname { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlAttribute]
        public string Username { get; set; }

        [XmlAttribute]
        public string Password { get; set; }

        [XmlArray]
        [XmlArrayItem("Repository")]
        public List<ServerRepository> Repositories { get; private set; }

        #region equality overrides
        public override bool Equals(object obj)
        {
            if (obj is ServerRepositoryList)
            {
                var other = (ServerRepositoryList)obj;
                return (this.Hostname == other.Hostname && this.Port == other.Port);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hostname.GetHashCode() + Port.GetHashCode();
        }
        #endregion
    }

    [Serializable]
    [XmlType("SyncGroup", Namespace="http://www.codewise.org/schema/foosync/SyncGroupList.xsd")]
    public class SyncGroup
    {
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// The System.Uri class isn't XML serializable, so this hack has to be used.
        /// The serializer adds entries to this temp list, which adds real FooSyncUrls
        /// to the URLs list.
        /// When the serializer needs to write back out again, we construct a new list for it,
        /// based off the real URLs list again.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlArray("URLs")]
        [XmlArrayItem("URL")]
        public ObservableCollection<string> RawStringURLs
        {
            get
            {
                if (_rawStringURLs == null)
                {
                    _rawStringURLs = new ObservableCollection<string>(
                            from url in URLs
                            select url.ToString()
                            );
                    _rawStringURLs.CollectionChanged += new NotifyCollectionChangedEventHandler(RawStringURLs_CollectionChanged);
                }
                return _rawStringURLs;
            }
        }

        [XmlIgnore]
        public List<FooSyncUrl> URLs
        {
            get
            {
                _rawStringURLs = null;
                return _urls;
            }
        }

        public SyncGroup()
        {
            _rawStringURLs = new ObservableCollection<string>();
            _rawStringURLs.CollectionChanged += new NotifyCollectionChangedEventHandler(RawStringURLs_CollectionChanged);
            _urls = new List<FooSyncUrl>();
        }

        private ObservableCollection<string> _rawStringURLs;
        private List<FooSyncUrl> _urls;

        private void RawStringURLs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
                throw new InvalidOperationException();

            foreach (string s in e.NewItems)
            {
                var url = new FooSyncUrl(s);
                URLs.Add(url);
            }
        }

        #region equality overrides
        public override bool Equals(object obj)
        {
            var other = obj as SyncGroup;
            if (other == null)
                return false;

            return Name.Equals(other.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        #endregion
    }

    [Serializable]
    [XmlType("ServerRepository", Namespace="http://www.codewise.org/schema/foosync/SyncGroupList.xsd")]
    public class ServerRepository
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlArray]
        [XmlArrayItem("Name")]
        public List<string> MemberOfSyncGroups { get; private set; }

        [XmlIgnore]
        public ServerRepositoryList Server { get; set; }

        public ServerRepository()
        {
            MemberOfSyncGroups = new List<string>();
        }

        #region equality overrides
        public override bool Equals(object obj)
        {
            var other = obj as ServerRepository;
            if (other == null)
                return false;

            return (Server.Equals(other.Server) && Name.Equals(other.Name));
        }

        public override int GetHashCode()
        {
            return Server.GetHashCode() + Name.GetHashCode();
        }
        #endregion
    }
}
