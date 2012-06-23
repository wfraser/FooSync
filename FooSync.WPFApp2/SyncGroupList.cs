///
/// Codewise/FooSync/WPFApp2/SyncGroupList.cs
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
    public class SyncGroupList : INotifyPropertyChanged
    {
        public SyncGroupList()
        {
            SyncGroups = new ObservableCollection<SyncGroup>();
            Servers    = new ObservableCollection<FooServer>();

            SyncGroups.CollectionChanged += new NotifyCollectionChangedEventHandler(Child_CollectionChanged);
            Servers.CollectionChanged += new NotifyCollectionChangedEventHandler(Child_CollectionChanged);
        }

        void Child_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                string property = null;
                if (sender == SyncGroups)
                {
                    property = "SyncGroups";
                }
                else if (sender == Servers)
                {
                    property = "Servers";
                }

                if (property != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "unknown property");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlArray]
        [XmlArrayItem("SyncGroup")]
        public ObservableCollection<SyncGroup> SyncGroups { get; private set; }

        [XmlArray]
        [XmlArrayItem("Server")]
        public ObservableCollection<FooServer> Servers { get; private set; }

        public static SyncGroupList Deserialize(Stream stream)
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

        public void Serialize(Stream stream)
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
    public class FooServer
    {
        public FooServer()
        {
            Port = FooSyncUrl.DefaultPort;
            Repositories = new Collection<ServerRepository>();
        }

        [XmlElement]
        public string Description { get; set; }

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
        public Collection<ServerRepository> Repositories { get; private set; }

        #region equality overrides
        public override bool Equals(object obj)
        {
            FooServer other = obj as FooServer;
            if (other != null)
            {
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
        public Collection<FooSyncUrl> URLs
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
            _urls = new Collection<FooSyncUrl>();
        }

        private ObservableCollection<string> _rawStringURLs;
        private Collection<FooSyncUrl> _urls;

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
        public Collection<string> MemberOfSyncGroups { get; private set; }

        [XmlIgnore]
        public FooServer Server { get; set; }

        public ServerRepository()
        {
            MemberOfSyncGroups = new Collection<string>();
        }

        public FooSyncUrl URL
        {
            get
            {
                if (_url == null)
                {
                    var urlString = new StringBuilder();
                    urlString.Append("fs://");

                    /*
                    if (!string.IsNullOrEmpty(Server.Username))
                    {
                        urlString.Append(Server.Username);
                        if (!string.IsNullOrEmpty(Server.Password))
                        {
                            urlString.Append(":").Append(Server.Password);
                        }
                        urlString.Append("@");
                    }
                     */

                    urlString.Append(Server.Hostname)
                             .Append(":")
                             .Append(Server.Port)
                             .Append("/")
                             .Append(Name);

                    _url = new FooSyncUrl(urlString.ToString());
                }
                return _url;
            }
        }

        [NonSerialized]
        private FooSyncUrl _url = null;

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
