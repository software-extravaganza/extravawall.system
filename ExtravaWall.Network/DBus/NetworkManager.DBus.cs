using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ExtravaWall.Network;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace NetworkManager.DBus
{
    public enum DeviceState : uint
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Unmanaged")]
        Unmanaged = 10,
        [Description("Unavailable")]
        Unavailable = 20,
        [Description("Disconnected")]
        Disconnected = 30,
        [Description("Prepare")]
        Prepare = 40,
        [Description("Config")]
        Config = 50,
        [Description("NeedAuth")]
        NeedAuth = 60,
        [Description("IpConfig")]
        IpConfig = 70,
        [Description("IpCheck")]
        IpCheck = 80,
        [Description("Secondaries")]
        Secondaries = 90,
        [Description("Activated")]
        Activated = 100,
        [Description("Deactivating")]
        Deactivating = 110,
        [Description("Failed")]
        Failed = 120
    }
    
    [DBusInterface("org.freedesktop.DBus.ObjectManager")]
    public interface IObjectManager : IDBusObject
    {
        Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
        Task<IDisposable> WatchInterfacesAddedAsync(Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfacesAndProperties)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchInterfacesRemovedAsync(Action<(ObjectPath objectPath, string[] interfaces)> handler, Action<Exception> onError = null);
    }

    [DBusInterface("org.freedesktop.NetworkManager")]
    public interface INetworkManager : IDBusObject
    {
        Task ReloadAsync(uint Flags);
        Task<ObjectPath[]> GetDevicesAsync();
        Task<ObjectPath[]> GetAllDevicesAsync();
        Task<ObjectPath> GetDeviceByIpIfaceAsync(string Iface);
        Task<ObjectPath> ActivateConnectionAsync(ObjectPath Connection, ObjectPath Device, ObjectPath SpecificObject);
        Task<(ObjectPath path, ObjectPath activeConnection)> AddAndActivateConnectionAsync(IDictionary<string, IDictionary<string, object>> Connection, ObjectPath Device, ObjectPath SpecificObject);
        Task<(ObjectPath path, ObjectPath activeConnection, IDictionary<string, object> result)> AddAndActivateConnection2Async(IDictionary<string, IDictionary<string, object>> Connection, ObjectPath Device, ObjectPath SpecificObject, IDictionary<string, object> Options);
        Task DeactivateConnectionAsync(ObjectPath ActiveConnection);
        Task SleepAsync(bool Sleep);
        Task EnableAsync(bool Enable);
        Task<IDictionary<string, string>> GetPermissionsAsync();
        Task SetLoggingAsync(string Level, string Domains);
        Task<(string level, string domains)> GetLoggingAsync();
        Task<uint> CheckConnectivityAsync();
        Task<uint> stateAsync();
        Task<ObjectPath> CheckpointCreateAsync(ObjectPath[] Devices, uint RollbackTimeout, uint Flags);
        Task CheckpointDestroyAsync(ObjectPath Checkpoint);
        Task<IDictionary<string, uint>> CheckpointRollbackAsync(ObjectPath Checkpoint);
        Task CheckpointAdjustRollbackTimeoutAsync(ObjectPath Checkpoint, uint AddTimeout);
        Task<IDisposable> WatchCheckPermissionsAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchStateChangedAsync(Action<uint> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchDeviceAddedAsync(Action<ObjectPath> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchDeviceRemovedAsync(Action<ObjectPath> handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<NetworkManagerProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class NetworkManagerProperties
    {
        private ObjectPath[] _devices = default(ObjectPath[]);
        public ObjectPath[] Devices
        {
            get
            {
                return _devices;
            }

            set
            {
                _devices = (value);
            }
        }

        private ObjectPath[] _allDevices = default(ObjectPath[]);
        public ObjectPath[] AllDevices
        {
            get
            {
                return _allDevices;
            }

            set
            {
                _allDevices = (value);
            }
        }

        private ObjectPath[] _checkpoints = default(ObjectPath[]);
        public ObjectPath[] Checkpoints
        {
            get
            {
                return _checkpoints;
            }

            set
            {
                _checkpoints = (value);
            }
        }

        private bool _networkingEnabled = default(bool);
        public bool NetworkingEnabled
        {
            get
            {
                return _networkingEnabled;
            }

            set
            {
                _networkingEnabled = (value);
            }
        }

        private bool _wirelessEnabled = default(bool);
        public bool WirelessEnabled
        {
            get
            {
                return _wirelessEnabled;
            }

            set
            {
                _wirelessEnabled = (value);
            }
        }

        private bool _wirelessHardwareEnabled = default(bool);
        public bool WirelessHardwareEnabled
        {
            get
            {
                return _wirelessHardwareEnabled;
            }

            set
            {
                _wirelessHardwareEnabled = (value);
            }
        }

        private bool _wwanEnabled = default(bool);
        public bool WwanEnabled
        {
            get
            {
                return _wwanEnabled;
            }

            set
            {
                _wwanEnabled = (value);
            }
        }

        private bool _wwanHardwareEnabled = default(bool);
        public bool WwanHardwareEnabled
        {
            get
            {
                return _wwanHardwareEnabled;
            }

            set
            {
                _wwanHardwareEnabled = (value);
            }
        }

        private bool _wimaxEnabled = default(bool);
        public bool WimaxEnabled
        {
            get
            {
                return _wimaxEnabled;
            }

            set
            {
                _wimaxEnabled = (value);
            }
        }

        private bool _wimaxHardwareEnabled = default(bool);
        public bool WimaxHardwareEnabled
        {
            get
            {
                return _wimaxHardwareEnabled;
            }

            set
            {
                _wimaxHardwareEnabled = (value);
            }
        }

        private uint _radioFlags = default(uint);
        public uint RadioFlags
        {
            get
            {
                return _radioFlags;
            }

            set
            {
                _radioFlags = (value);
            }
        }

        private ObjectPath[] _activeConnections = default(ObjectPath[]);
        public ObjectPath[] ActiveConnections
        {
            get
            {
                return _activeConnections;
            }

            set
            {
                _activeConnections = (value);
            }
        }

        private ObjectPath _primaryConnection = default(ObjectPath);
        public ObjectPath PrimaryConnection
        {
            get
            {
                return _primaryConnection;
            }

            set
            {
                _primaryConnection = (value);
            }
        }

        private string _primaryConnectionType = default(string);
        public string PrimaryConnectionType
        {
            get
            {
                return _primaryConnectionType;
            }

            set
            {
                _primaryConnectionType = (value);
            }
        }

        private uint _metered = default(uint);
        public uint Metered
        {
            get
            {
                return _metered;
            }

            set
            {
                _metered = (value);
            }
        }

        private ObjectPath _activatingConnection = default(ObjectPath);
        public ObjectPath ActivatingConnection
        {
            get
            {
                return _activatingConnection;
            }

            set
            {
                _activatingConnection = (value);
            }
        }

        private bool _startup = default(bool);
        public bool Startup
        {
            get
            {
                return _startup;
            }

            set
            {
                _startup = (value);
            }
        }

        private string _version = default(string);
        public string Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = (value);
            }
        }

        private uint[] _capabilities = default(uint[]);
        public uint[] Capabilities
        {
            get
            {
                return _capabilities;
            }

            set
            {
                _capabilities = (value);
            }
        }

        private uint _state = default(uint);
        public uint State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = (value);
            }
        }

        private uint _connectivity = default(uint);
        public uint Connectivity
        {
            get
            {
                return _connectivity;
            }

            set
            {
                _connectivity = (value);
            }
        }

        private bool _connectivityCheckAvailable = default(bool);
        public bool ConnectivityCheckAvailable
        {
            get
            {
                return _connectivityCheckAvailable;
            }

            set
            {
                _connectivityCheckAvailable = (value);
            }
        }

        private bool _connectivityCheckEnabled = default(bool);
        public bool ConnectivityCheckEnabled
        {
            get
            {
                return _connectivityCheckEnabled;
            }

            set
            {
                _connectivityCheckEnabled = (value);
            }
        }

        private string _connectivityCheckUri = default(string);
        public string ConnectivityCheckUri
        {
            get
            {
                return _connectivityCheckUri;
            }

            set
            {
                _connectivityCheckUri = (value);
            }
        }

        private IDictionary<string, object> _globalDnsConfiguration = default(IDictionary<string, object>);
        public IDictionary<string, object> GlobalDnsConfiguration
        {
            get
            {
                return _globalDnsConfiguration;
            }

            set
            {
                _globalDnsConfiguration = (value);
            }
        }
    }

    public static class NetworkManagerExtensions
    {
        public static Task<ObjectPath[]> GetDevicesAsync(this INetworkManager o) => o.GetAsync<ObjectPath[]>("Devices");
        public static Task<ObjectPath[]> GetAllDevicesAsync(this INetworkManager o) => o.GetAsync<ObjectPath[]>("AllDevices");
        public static Task<ObjectPath[]> GetCheckpointsAsync(this INetworkManager o) => o.GetAsync<ObjectPath[]>("Checkpoints");
        public static Task<bool> GetNetworkingEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("NetworkingEnabled");
        public static Task<bool> GetWirelessEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WirelessEnabled");
        public static Task<bool> GetWirelessHardwareEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WirelessHardwareEnabled");
        public static Task<bool> GetWwanEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WwanEnabled");
        public static Task<bool> GetWwanHardwareEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WwanHardwareEnabled");
        public static Task<bool> GetWimaxEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WimaxEnabled");
        public static Task<bool> GetWimaxHardwareEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("WimaxHardwareEnabled");
        public static Task<uint> GetRadioFlagsAsync(this INetworkManager o) => o.GetAsync<uint>("RadioFlags");
        public static Task<ObjectPath[]> GetActiveConnectionsAsync(this INetworkManager o) => o.GetAsync<ObjectPath[]>("ActiveConnections");
        public static Task<ObjectPath> GetPrimaryConnectionAsync(this INetworkManager o) => o.GetAsync<ObjectPath>("PrimaryConnection");
        public static Task<string> GetPrimaryConnectionTypeAsync(this INetworkManager o) => o.GetAsync<string>("PrimaryConnectionType");
        public static Task<uint> GetMeteredAsync(this INetworkManager o) => o.GetAsync<uint>("Metered");
        public static Task<ObjectPath> GetActivatingConnectionAsync(this INetworkManager o) => o.GetAsync<ObjectPath>("ActivatingConnection");
        public static Task<bool> GetStartupAsync(this INetworkManager o) => o.GetAsync<bool>("Startup");
        public static Task<string> GetVersionAsync(this INetworkManager o) => o.GetAsync<string>("Version");
        public static Task<uint[]> GetCapabilitiesAsync(this INetworkManager o) => o.GetAsync<uint[]>("Capabilities");
        public static Task<DeviceState> GetStateAsync(this INetworkManager o) => o.GetAsync<DeviceState>("State");
        public static Task<uint> GetConnectivityAsync(this INetworkManager o) => o.GetAsync<uint>("Connectivity");
        public static Task<bool> GetConnectivityCheckAvailableAsync(this INetworkManager o) => o.GetAsync<bool>("ConnectivityCheckAvailable");
        public static Task<bool> GetConnectivityCheckEnabledAsync(this INetworkManager o) => o.GetAsync<bool>("ConnectivityCheckEnabled");
        public static Task<string> GetConnectivityCheckUriAsync(this INetworkManager o) => o.GetAsync<string>("ConnectivityCheckUri");
        public static Task<IDictionary<string, object>> GetGlobalDnsConfigurationAsync(this INetworkManager o) => o.GetAsync<IDictionary<string, object>>("GlobalDnsConfiguration");
        public static Task SetWirelessEnabledAsync(this INetworkManager o, bool val) => o.SetAsync("WirelessEnabled", val);
        public static Task SetWwanEnabledAsync(this INetworkManager o, bool val) => o.SetAsync("WwanEnabled", val);
        public static Task SetWimaxEnabledAsync(this INetworkManager o, bool val) => o.SetAsync("WimaxEnabled", val);
        public static Task SetConnectivityCheckEnabledAsync(this INetworkManager o, bool val) => o.SetAsync("ConnectivityCheckEnabled", val);
        public static Task SetGlobalDnsConfigurationAsync(this INetworkManager o, IDictionary<string, object> val) => o.SetAsync("GlobalDnsConfiguration", val);
    }

    [DBusInterface("org.freedesktop.NetworkManager.DHCP4Config")]
    public interface IDHCP4Config : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<DHCP4ConfigProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class DHCP4ConfigProperties
    {
        private IDictionary<string, object> _options = default(IDictionary<string, object>);
        public IDictionary<string, object> Options
        {
            get
            {
                return _options;
            }

            set
            {
                _options = (value);
            }
        }
    }

    public static class DHCP4ConfigExtensions
    {
        public static Task<IDictionary<string, object>> GetOptionsAsync(this IDHCP4Config o) => o.GetAsync<IDictionary<string, object>>("Options");
    }

    [DBusInterface("org.freedesktop.NetworkManager.IP4Config")]
    public interface IIP4Config : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<IP4ConfigProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class IP4ConfigProperties
    {
        private uint[][] _addresses = default(uint[][]);
        public uint[][] Addresses
        {
            get
            {
                return _addresses;
            }

            set
            {
                _addresses = (value);
            }
        }

        private IDictionary<string, object>[] _addressData = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] AddressData
        {
            get
            {
                return _addressData;
            }

            set
            {
                _addressData = (value);
            }
        }

        private string _gateway = default(string);
        public string Gateway
        {
            get
            {
                return _gateway;
            }

            set
            {
                _gateway = (value);
            }
        }

        private uint[][] _routes = default(uint[][]);
        public uint[][] Routes
        {
            get
            {
                return _routes;
            }

            set
            {
                _routes = (value);
            }
        }

        private IDictionary<string, object>[] _routeData = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] RouteData
        {
            get
            {
                return _routeData;
            }

            set
            {
                _routeData = (value);
            }
        }

        private IDictionary<string, object>[] _nameserverData = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] NameserverData
        {
            get
            {
                return _nameserverData;
            }

            set
            {
                _nameserverData = (value);
            }
        }

        private uint[] _nameservers = default(uint[]);
        public uint[] Nameservers
        {
            get
            {
                return _nameservers;
            }

            set
            {
                _nameservers = (value);
            }
        }

        private string[] _domains = default(string[]);
        public string[] Domains
        {
            get
            {
                return _domains;
            }

            set
            {
                _domains = (value);
            }
        }

        private string[] _searches = default(string[]);
        public string[] Searches
        {
            get
            {
                return _searches;
            }

            set
            {
                _searches = (value);
            }
        }

        private string[] _dnsOptions = default(string[]);
        public string[] DnsOptions
        {
            get
            {
                return _dnsOptions;
            }

            set
            {
                _dnsOptions = (value);
            }
        }

        private int _dnsPriority = default(int);
        public int DnsPriority
        {
            get
            {
                return _dnsPriority;
            }

            set
            {
                _dnsPriority = (value);
            }
        }

        private string[] _winsServerData = default(string[]);
        public string[] WinsServerData
        {
            get
            {
                return _winsServerData;
            }

            set
            {
                _winsServerData = (value);
            }
        }

        private uint[] _winsServers = default(uint[]);
        public uint[] WinsServers
        {
            get
            {
                return _winsServers;
            }

            set
            {
                _winsServers = (value);
            }
        }
    }

    public static class IP4ConfigExtensions
    {
        public static Task<uint[][]> GetAddressesAsync(this IIP4Config o) => o.GetAsync<uint[][]>("Addresses");
        public static Task<IDictionary<string, object>[]> GetAddressDataAsync(this IIP4Config o) => o.GetAsync<IDictionary<string, object>[]>("AddressData");
        public static Task<string> GetGatewayAsync(this IIP4Config o) => o.GetAsync<string>("Gateway");
        public static Task<uint[][]> GetRoutesAsync(this IIP4Config o) => o.GetAsync<uint[][]>("Routes");
        public static Task<IDictionary<string, object>[]> GetRouteDataAsync(this IIP4Config o) => o.GetAsync<IDictionary<string, object>[]>("RouteData");
        public static Task<IDictionary<string, object>[]> GetNameserverDataAsync(this IIP4Config o) => o.GetAsync<IDictionary<string, object>[]>("NameserverData");
        public static Task<uint[]> GetNameserversAsync(this IIP4Config o) => o.GetAsync<uint[]>("Nameservers");
        public static Task<string[]> GetDomainsAsync(this IIP4Config o) => o.GetAsync<string[]>("Domains");
        public static Task<string[]> GetSearchesAsync(this IIP4Config o) => o.GetAsync<string[]>("Searches");
        public static Task<string[]> GetDnsOptionsAsync(this IIP4Config o) => o.GetAsync<string[]>("DnsOptions");
        public static Task<int> GetDnsPriorityAsync(this IIP4Config o) => o.GetAsync<int>("DnsPriority");
        public static Task<string[]> GetWinsServerDataAsync(this IIP4Config o) => o.GetAsync<string[]>("WinsServerData");
        public static Task<uint[]> GetWinsServersAsync(this IIP4Config o) => o.GetAsync<uint[]>("WinsServers");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Connection.Active")]
    public interface IActive : IDBusObject
    {
        Task<IDisposable> WatchStateChangedAsync(Action<(uint state, uint reason)> handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<ActiveProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class ActiveProperties
    {
        private ObjectPath _connection = default(ObjectPath);
        public ObjectPath Connection
        {
            get
            {
                return _connection;
            }

            set
            {
                _connection = (value);
            }
        }

        private ObjectPath _specificObject = default(ObjectPath);
        public ObjectPath SpecificObject
        {
            get
            {
                return _specificObject;
            }

            set
            {
                _specificObject = (value);
            }
        }

        private string _id = default(string);
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = (value);
            }
        }

        private string _uuid = default(string);
        public string Uuid
        {
            get
            {
                return _uuid;
            }

            set
            {
                _uuid = (value);
            }
        }

        private string _type = default(string);
        public string Type
        {
            get
            {
                return _type;
            }

            set
            {
                _type = (value);
            }
        }

        private ObjectPath[] _devices = default(ObjectPath[]);
        public ObjectPath[] Devices
        {
            get
            {
                return _devices;
            }

            set
            {
                _devices = (value);
            }
        }

        private uint _state = default(uint);
        public uint State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = (value);
            }
        }

        private uint _stateFlags = default(uint);
        public uint StateFlags
        {
            get
            {
                return _stateFlags;
            }

            set
            {
                _stateFlags = (value);
            }
        }

        private bool _default = default(bool);
        public bool Default
        {
            get
            {
                return _default;
            }

            set
            {
                _default = (value);
            }
        }

        private ObjectPath _ip4Config = default(ObjectPath);
        public ObjectPath Ip4Config
        {
            get
            {
                return _ip4Config;
            }

            set
            {
                _ip4Config = (value);
            }
        }

        private ObjectPath _dhcp4Config = default(ObjectPath);
        public ObjectPath Dhcp4Config
        {
            get
            {
                return _dhcp4Config;
            }

            set
            {
                _dhcp4Config = (value);
            }
        }

        private bool _default6 = default(bool);
        public bool Default6
        {
            get
            {
                return _default6;
            }

            set
            {
                _default6 = (value);
            }
        }

        private ObjectPath _ip6Config = default(ObjectPath);
        public ObjectPath Ip6Config
        {
            get
            {
                return _ip6Config;
            }

            set
            {
                _ip6Config = (value);
            }
        }

        private ObjectPath _dhcp6Config = default(ObjectPath);
        public ObjectPath Dhcp6Config
        {
            get
            {
                return _dhcp6Config;
            }

            set
            {
                _dhcp6Config = (value);
            }
        }

        private bool _vpn = default(bool);
        public bool Vpn
        {
            get
            {
                return _vpn;
            }

            set
            {
                _vpn = (value);
            }
        }

        private ObjectPath _master = default(ObjectPath);
        public ObjectPath Master
        {
            get
            {
                return _master;
            }

            set
            {
                _master = (value);
            }
        }
    }

    public static class ActiveExtensions
    {
        public static Task<ObjectPath> GetConnectionAsync(this IActive o) => o.GetAsync<ObjectPath>("Connection");
        public static Task<ObjectPath> GetSpecificObjectAsync(this IActive o) => o.GetAsync<ObjectPath>("SpecificObject");
        public static Task<string> GetIdAsync(this IActive o) => o.GetAsync<string>("Id");
        public static Task<string> GetUuidAsync(this IActive o) => o.GetAsync<string>("Uuid");
        public static Task<string> GetTypeAsync(this IActive o) => o.GetAsync<string>("Type");
        public static Task<ObjectPath[]> GetDevicesAsync(this IActive o) => o.GetAsync<ObjectPath[]>("Devices");
        public static Task<DeviceState> GetStateAsync(this IActive o) => o.GetAsync<DeviceState>("State");
        public static Task<uint> GetStateFlagsAsync(this IActive o) => o.GetAsync<uint>("StateFlags");
        public static Task<bool> GetDefaultAsync(this IActive o) => o.GetAsync<bool>("Default");
        public static Task<IIP4Config> GetIp4ConfigAsync(this IActive o) => o.GetAsync<IIP4Config>("Ip4Config");
        public static Task<ObjectPath> GetDhcp4ConfigAsync(this IActive o) => o.GetAsync<ObjectPath>("Dhcp4Config");
        public static Task<bool> GetDefault6Async(this IActive o) => o.GetAsync<bool>("Default6");
        public static Task<ObjectPath> GetIp6ConfigAsync(this IActive o) => o.GetAsync<ObjectPath>("Ip6Config");
        public static Task<ObjectPath> GetDhcp6ConfigAsync(this IActive o) => o.GetAsync<ObjectPath>("Dhcp6Config");
        public static Task<bool> GetVpnAsync(this IActive o) => o.GetAsync<bool>("Vpn");
        public static Task<ObjectPath> GetMasterAsync(this IActive o) => o.GetAsync<ObjectPath>("Master");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device.Statistics")]
    public interface IStatistics : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<StatisticsProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class StatisticsProperties
    {
        private uint _refreshRateMs = default(uint);
        public uint RefreshRateMs
        {
            get
            {
                return _refreshRateMs;
            }

            set
            {
                _refreshRateMs = (value);
            }
        }

        private ulong _txBytes = default(ulong);
        public ulong TxBytes
        {
            get
            {
                return _txBytes;
            }

            set
            {
                _txBytes = (value);
            }
        }

        private ulong _rxBytes = default(ulong);
        public ulong RxBytes
        {
            get
            {
                return _rxBytes;
            }

            set
            {
                _rxBytes = (value);
            }
        }
    }

    public static class StatisticsExtensions
    {
        public static Task<uint> GetRefreshRateMsAsync(this IStatistics o) => o.GetAsync<uint>("RefreshRateMs");
        public static Task<ulong> GetTxBytesAsync(this IStatistics o) => o.GetAsync<ulong>("TxBytes");
        public static Task<ulong> GetRxBytesAsync(this IStatistics o) => o.GetAsync<ulong>("RxBytes");
        public static Task SetRefreshRateMsAsync(this IStatistics o, uint val) => o.SetAsync("RefreshRateMs", val);
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device")]
    public interface IDevice : IDBusObject
    {
        Task ReapplyAsync(IDictionary<string, IDictionary<string, object>> Connection, ulong VersionId, uint Flags);
        Task<(IDictionary<string, IDictionary<string, object>> connection, ulong versionId)> GetAppliedConnectionAsync(uint Flags);
        Task DisconnectAsync();
        Task DeleteAsync();
        Task<IDisposable> WatchStateChangedAsync(Action<(DeviceState newState, DeviceState oldState, uint reason)> handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<DeviceProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class DeviceProperties
    {
        private string _udi = default(string);
        public string Udi
        {
            get
            {
                return _udi;
            }

            set
            {
                _udi = (value);
            }
        }

        private string _path = default(string);
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = (value);
            }
        }

        private string _interface = default(string);
        public string Interface
        {
            get
            {
                return _interface;
            }

            set
            {
                _interface = (value);
            }
        }

        private string _ipInterface = default(string);
        public string IpInterface
        {
            get
            {
                return _ipInterface;
            }

            set
            {
                _ipInterface = (value);
            }
        }

        private string _driver = default(string);
        public string Driver
        {
            get
            {
                return _driver;
            }

            set
            {
                _driver = (value);
            }
        }

        private string _driverVersion = default(string);
        public string DriverVersion
        {
            get
            {
                return _driverVersion;
            }

            set
            {
                _driverVersion = (value);
            }
        }

        private string _firmwareVersion = default(string);
        public string FirmwareVersion
        {
            get
            {
                return _firmwareVersion;
            }

            set
            {
                _firmwareVersion = (value);
            }
        }

        private uint _capabilities = default(uint);
        public uint Capabilities
        {
            get
            {
                return _capabilities;
            }

            set
            {
                _capabilities = (value);
            }
        }

        private uint _ip4Address = default(uint);
        public uint Ip4Address
        {
            get
            {
                return _ip4Address;
            }

            set
            {
                _ip4Address = (value);
            }
        }

        private uint _state = default(uint);
        public uint State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = (value);
            }
        }

        private (uint, uint) _stateReason = default((uint, uint));
        public (uint, uint) StateReason
        {
            get
            {
                return _stateReason;
            }

            set
            {
                _stateReason = (value);
            }
        }

        private ObjectPath _activeConnection = default(ObjectPath);
        public ObjectPath ActiveConnection
        {
            get
            {
                return _activeConnection;
            }

            set
            {
                _activeConnection = (value);
            }
        }

        private ObjectPath _ip4Config = default(ObjectPath);
        public ObjectPath Ip4Config
        {
            get
            {
                return _ip4Config;
            }

            set
            {
                _ip4Config = (value);
            }
        }

        private ObjectPath _dhcp4Config = default(ObjectPath);
        public ObjectPath Dhcp4Config
        {
            get
            {
                return _dhcp4Config;
            }

            set
            {
                _dhcp4Config = (value);
            }
        }

        private ObjectPath _ip6Config = default(ObjectPath);
        public ObjectPath Ip6Config
        {
            get
            {
                return _ip6Config;
            }

            set
            {
                _ip6Config = (value);
            }
        }

        private ObjectPath _dhcp6Config = default(ObjectPath);
        public ObjectPath Dhcp6Config
        {
            get
            {
                return _dhcp6Config;
            }

            set
            {
                _dhcp6Config = (value);
            }
        }

        private bool _managed = default(bool);
        public bool Managed
        {
            get
            {
                return _managed;
            }

            set
            {
                _managed = (value);
            }
        }

        private bool _autoconnect = default(bool);
        public bool Autoconnect
        {
            get
            {
                return _autoconnect;
            }

            set
            {
                _autoconnect = (value);
            }
        }

        private bool _firmwareMissing = default(bool);
        public bool FirmwareMissing
        {
            get
            {
                return _firmwareMissing;
            }

            set
            {
                _firmwareMissing = (value);
            }
        }

        private bool _nmPluginMissing = default(bool);
        public bool NmPluginMissing
        {
            get
            {
                return _nmPluginMissing;
            }

            set
            {
                _nmPluginMissing = (value);
            }
        }

        private uint _deviceType = default(uint);
        public uint DeviceType
        {
            get
            {
                return _deviceType;
            }

            set
            {
                _deviceType = (value);
            }
        }

        private ObjectPath[] _availableConnections = default(ObjectPath[]);
        public ObjectPath[] AvailableConnections
        {
            get
            {
                return _availableConnections;
            }

            set
            {
                _availableConnections = (value);
            }
        }

        private string _physicalPortId = default(string);
        public string PhysicalPortId
        {
            get
            {
                return _physicalPortId;
            }

            set
            {
                _physicalPortId = (value);
            }
        }

        private uint _mtu = default(uint);
        public uint Mtu
        {
            get
            {
                return _mtu;
            }

            set
            {
                _mtu = (value);
            }
        }

        private uint _metered = default(uint);
        public uint Metered
        {
            get
            {
                return _metered;
            }

            set
            {
                _metered = (value);
            }
        }

        private IDictionary<string, object>[] _lldpNeighbors = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] LldpNeighbors
        {
            get
            {
                return _lldpNeighbors;
            }

            set
            {
                _lldpNeighbors = (value);
            }
        }

        private bool _real = default(bool);
        public bool Real
        {
            get
            {
                return _real;
            }

            set
            {
                _real = (value);
            }
        }

        private uint _ip4Connectivity = default(uint);
        public uint Ip4Connectivity
        {
            get
            {
                return _ip4Connectivity;
            }

            set
            {
                _ip4Connectivity = (value);
            }
        }

        private uint _ip6Connectivity = default(uint);
        public uint Ip6Connectivity
        {
            get
            {
                return _ip6Connectivity;
            }

            set
            {
                _ip6Connectivity = (value);
            }
        }

        private uint _interfaceFlags = default(uint);
        public uint InterfaceFlags
        {
            get
            {
                return _interfaceFlags;
            }

            set
            {
                _interfaceFlags = (value);
            }
        }

        private string _hwAddress = default(string);
        public string HwAddress
        {
            get
            {
                return _hwAddress;
            }

            set
            {
                _hwAddress = (value);
            }
        }

        private ObjectPath[] _ports = default(ObjectPath[]);
        public ObjectPath[] Ports
        {
            get
            {
                return _ports;
            }

            set
            {
                _ports = (value);
            }
        }
    }

    public static class DeviceExtensions
    {
        public static Task<string> GetUdiAsync(this IDevice o) => o.GetAsync<string>("Udi");
        public static Task<string> GetPathAsync(this IDevice o) => o.GetAsync<string>("Path");
        public static Task<string> GetInterfaceAsync(this IDevice o) => o.GetAsync<string>("Interface");
        public static Task<string> GetIpInterfaceAsync(this IDevice o) => o.GetAsync<string>("IpInterface");
        public static Task<string> GetDriverAsync(this IDevice o) => o.GetAsync<string>("Driver");
        public static Task<string> GetDriverVersionAsync(this IDevice o) => o.GetAsync<string>("DriverVersion");
        public static Task<string> GetFirmwareVersionAsync(this IDevice o) => o.GetAsync<string>("FirmwareVersion");
        public static Task<uint> GetCapabilitiesAsync(this IDevice o) => o.GetAsync<uint>("Capabilities");
        public static Task<uint> GetIp4AddressAsync(this IDevice o) => o.GetAsync<uint>("Ip4Address");
        public static Task<DeviceState> GetStateAsync(this IDevice o) => o.GetAsync<DeviceState>("State");
        public static Task<(uint, uint)> GetStateReasonAsync(this IDevice o) => o.GetAsync<(uint, uint)>("StateReason");
        public static Task<ObjectPath> GetActiveConnectionAsync(this IDevice o) => o.GetAsync<ObjectPath>("ActiveConnection");
        public static Task<IIP4Config> GetIp4ConfigAsync(this IDevice o) => o.GetAsync<IIP4Config>("Ip4Config");
        public static Task<ObjectPath> GetDhcp4ConfigAsync(this IDevice o) => o.GetAsync<ObjectPath>("Dhcp4Config");
        public static Task<ObjectPath> GetIp6ConfigAsync(this IDevice o) => o.GetAsync<ObjectPath>("Ip6Config");
        public static Task<ObjectPath> GetDhcp6ConfigAsync(this IDevice o) => o.GetAsync<ObjectPath>("Dhcp6Config");
        public static Task<bool> GetManagedAsync(this IDevice o) => o.GetAsync<bool>("Managed");
        public static Task<bool> GetAutoconnectAsync(this IDevice o) => o.GetAsync<bool>("Autoconnect");
        public static Task<bool> GetFirmwareMissingAsync(this IDevice o) => o.GetAsync<bool>("FirmwareMissing");
        public static Task<bool> GetNmPluginMissingAsync(this IDevice o) => o.GetAsync<bool>("NmPluginMissing");
        public static Task<uint> GetDeviceTypeAsync(this IDevice o) => o.GetAsync<uint>("DeviceType");
        public static Task<ObjectPath[]> GetAvailableConnectionsAsync(this IDevice o) => o.GetAsync<ObjectPath[]>("AvailableConnections");
        public static Task<string> GetPhysicalPortIdAsync(this IDevice o) => o.GetAsync<string>("PhysicalPortId");
        public static Task<uint> GetMtuAsync(this IDevice o) => o.GetAsync<uint>("Mtu");
        public static Task<uint> GetMeteredAsync(this IDevice o) => o.GetAsync<uint>("Metered");
        public static Task<IDictionary<string, object>[]> GetLldpNeighborsAsync(this IDevice o) => o.GetAsync<IDictionary<string, object>[]>("LldpNeighbors");
        public static Task<bool> GetRealAsync(this IDevice o) => o.GetAsync<bool>("Real");
        public static Task<uint> GetIp4ConnectivityAsync(this IDevice o) => o.GetAsync<uint>("Ip4Connectivity");
        public static Task<uint> GetIp6ConnectivityAsync(this IDevice o) => o.GetAsync<uint>("Ip6Connectivity");
        public static Task<uint> GetInterfaceFlagsAsync(this IDevice o) => o.GetAsync<uint>("InterfaceFlags");
        public static Task<string> GetHwAddressAsync(this IDevice o) => o.GetAsync<string>("HwAddress");
        public static Task<ObjectPath[]> GetPortsAsync(this IDevice o) => o.GetAsync<ObjectPath[]>("Ports");
        public static Task SetManagedAsync(this IDevice o, bool val) => o.SetAsync("Managed", val);
        public static Task SetAutoconnectAsync(this IDevice o, bool val) => o.SetAsync("Autoconnect", val);
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device.Wired")]
    public interface IWired : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<WiredProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class WiredProperties
    {
        private string _hwAddress = default(string);
        public string HwAddress
        {
            get
            {
                return _hwAddress;
            }

            set
            {
                _hwAddress = (value);
            }
        }

        private string _permHwAddress = default(string);
        public string PermHwAddress
        {
            get
            {
                return _permHwAddress;
            }

            set
            {
                _permHwAddress = (value);
            }
        }

        private uint _speed = default(uint);
        public uint Speed
        {
            get
            {
                return _speed;
            }

            set
            {
                _speed = (value);
            }
        }

        private string[] _s390Subchannels = default(string[]);
        public string[] S390Subchannels
        {
            get
            {
                return _s390Subchannels;
            }

            set
            {
                _s390Subchannels = (value);
            }
        }

        private bool _carrier = default(bool);
        public bool Carrier
        {
            get
            {
                return _carrier;
            }

            set
            {
                _carrier = (value);
            }
        }
    }

    public static class WiredExtensions
    {
        public static Task<string> GetHwAddressAsync(this IWired o) => o.GetAsync<string>("HwAddress");
        public static Task<string> GetPermHwAddressAsync(this IWired o) => o.GetAsync<string>("PermHwAddress");
        public static Task<uint> GetSpeedAsync(this IWired o) => o.GetAsync<uint>("Speed");
        public static Task<string[]> GetS390SubchannelsAsync(this IWired o) => o.GetAsync<string[]>("S390Subchannels");
        public static Task<bool> GetCarrierAsync(this IWired o) => o.GetAsync<bool>("Carrier");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device.Bridge")]
    public interface IBridge : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<BridgeProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class BridgeProperties
    {
        private string _hwAddress = default(string);
        public string HwAddress
        {
            get
            {
                return _hwAddress;
            }

            set
            {
                _hwAddress = (value);
            }
        }

        private bool _carrier = default(bool);
        public bool Carrier
        {
            get
            {
                return _carrier;
            }

            set
            {
                _carrier = (value);
            }
        }

        private ObjectPath[] _slaves = default(ObjectPath[]);
        public ObjectPath[] Slaves
        {
            get
            {
                return _slaves;
            }

            set
            {
                _slaves = (value);
            }
        }
    }

    public static class BridgeExtensions
    {
        public static Task<string> GetHwAddressAsync(this IBridge o) => o.GetAsync<string>("HwAddress");
        public static Task<bool> GetCarrierAsync(this IBridge o) => o.GetAsync<bool>("Carrier");
        public static Task<ObjectPath[]> GetSlavesAsync(this IBridge o) => o.GetAsync<ObjectPath[]>("Slaves");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device.Generic")]
    public interface IGeneric : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<GenericProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class GenericProperties
    {
        private string _hwAddress = default(string);
        public string HwAddress
        {
            get
            {
                return _hwAddress;
            }

            set
            {
                _hwAddress = (value);
            }
        }

        private string _typeDescription = default(string);
        public string TypeDescription
        {
            get
            {
                return _typeDescription;
            }

            set
            {
                _typeDescription = (value);
            }
        }
    }

    public static class GenericExtensions
    {
        public static Task<string> GetHwAddressAsync(this IGeneric o) => o.GetAsync<string>("HwAddress");
        public static Task<string> GetTypeDescriptionAsync(this IGeneric o) => o.GetAsync<string>("TypeDescription");
    }

    [DBusInterface("org.freedesktop.NetworkManager.AgentManager")]
    public interface IAgentManager : IDBusObject
    {
        Task RegisterAsync(string Identifier);
        Task RegisterWithCapabilitiesAsync(string Identifier, uint Capabilities);
        Task UnregisterAsync();
    }

    [DBusInterface("org.freedesktop.NetworkManager.DnsManager")]
    public interface IDnsManager : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<DnsManagerProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class DnsManagerProperties
    {
        private string _mode = default(string);
        public string Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                _mode = (value);
            }
        }

        private string _rcManager = default(string);
        public string RcManager
        {
            get
            {
                return _rcManager;
            }

            set
            {
                _rcManager = (value);
            }
        }

        private IDictionary<string, object>[] _configuration = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] Configuration
        {
            get
            {
                return _configuration;
            }

            set
            {
                _configuration = (value);
            }
        }
    }

    public static class DnsManagerExtensions
    {
        public static Task<string> GetModeAsync(this IDnsManager o) => o.GetAsync<string>("Mode");
        public static Task<string> GetRcManagerAsync(this IDnsManager o) => o.GetAsync<string>("RcManager");
        public static Task<IDictionary<string, object>[]> GetConfigurationAsync(this IDnsManager o) => o.GetAsync<IDictionary<string, object>[]>("Configuration");
    }

    [DBusInterface("org.freedesktop.NetworkManager.IP6Config")]
    public interface IIP6Config : IDBusObject
    {
        Task<T> GetAsync<T>(string prop);
        Task<IP6ConfigProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class IP6ConfigProperties
    {
        private (byte[], uint, byte[])[] _addresses = default((byte[], uint, byte[])[]);
        public (byte[], uint, byte[])[] Addresses
        {
            get
            {
                return _addresses;
            }

            set
            {
                _addresses = (value);
            }
        }

        private IDictionary<string, object>[] _addressData = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] AddressData
        {
            get
            {
                return _addressData;
            }

            set
            {
                _addressData = (value);
            }
        }

        private string _gateway = default(string);
        public string Gateway
        {
            get
            {
                return _gateway;
            }

            set
            {
                _gateway = (value);
            }
        }

        private (byte[], uint, byte[], uint)[] _routes = default((byte[], uint, byte[], uint)[]);
        public (byte[], uint, byte[], uint)[] Routes
        {
            get
            {
                return _routes;
            }

            set
            {
                _routes = (value);
            }
        }

        private IDictionary<string, object>[] _routeData = default(IDictionary<string, object>[]);
        public IDictionary<string, object>[] RouteData
        {
            get
            {
                return _routeData;
            }

            set
            {
                _routeData = (value);
            }
        }

        private byte[][] _nameservers = default(byte[][]);
        public byte[][] Nameservers
        {
            get
            {
                return _nameservers;
            }

            set
            {
                _nameservers = (value);
            }
        }

        private string[] _domains = default(string[]);
        public string[] Domains
        {
            get
            {
                return _domains;
            }

            set
            {
                _domains = (value);
            }
        }

        private string[] _searches = default(string[]);
        public string[] Searches
        {
            get
            {
                return _searches;
            }

            set
            {
                _searches = (value);
            }
        }

        private string[] _dnsOptions = default(string[]);
        public string[] DnsOptions
        {
            get
            {
                return _dnsOptions;
            }

            set
            {
                _dnsOptions = (value);
            }
        }

        private int _dnsPriority = default(int);
        public int DnsPriority
        {
            get
            {
                return _dnsPriority;
            }

            set
            {
                _dnsPriority = (value);
            }
        }
    }

    public static class IP6ConfigExtensions
    {
        public static Task<(byte[], uint, byte[])[]> GetAddressesAsync(this IIP6Config o) => o.GetAsync<(byte[], uint, byte[])[]>("Addresses");
        public static Task<IDictionary<string, object>[]> GetAddressDataAsync(this IIP6Config o) => o.GetAsync<IDictionary<string, object>[]>("AddressData");
        public static Task<string> GetGatewayAsync(this IIP6Config o) => o.GetAsync<string>("Gateway");
        public static Task<(byte[], uint, byte[], uint)[]> GetRoutesAsync(this IIP6Config o) => o.GetAsync<(byte[], uint, byte[], uint)[]>("Routes");
        public static Task<IDictionary<string, object>[]> GetRouteDataAsync(this IIP6Config o) => o.GetAsync<IDictionary<string, object>[]>("RouteData");
        public static Task<byte[][]> GetNameserversAsync(this IIP6Config o) => o.GetAsync<byte[][]>("Nameservers");
        public static Task<string[]> GetDomainsAsync(this IIP6Config o) => o.GetAsync<string[]>("Domains");
        public static Task<string[]> GetSearchesAsync(this IIP6Config o) => o.GetAsync<string[]>("Searches");
        public static Task<string[]> GetDnsOptionsAsync(this IIP6Config o) => o.GetAsync<string[]>("DnsOptions");
        public static Task<int> GetDnsPriorityAsync(this IIP6Config o) => o.GetAsync<int>("DnsPriority");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Settings")]
    public interface ISettings : IDBusObject
    {
        Task<ObjectPath[]> ListConnectionsAsync();
        Task<ObjectPath> GetConnectionByUuidAsync(string Uuid);
        Task<ObjectPath> AddConnectionAsync(IDictionary<string, IDictionary<string, object>> Connection);
        Task<ObjectPath> AddConnectionUnsavedAsync(IDictionary<string, IDictionary<string, object>> Connection);
        Task<(ObjectPath path, IDictionary<string, object> result)> AddConnection2Async(IDictionary<string, IDictionary<string, object>> Settings, uint Flags, IDictionary<string, object> Args);
        Task<(bool status, string[] failures)> LoadConnectionsAsync(string[] Filenames);
        Task<bool> ReloadConnectionsAsync();
        Task SaveHostnameAsync(string Hostname);
        Task<IDisposable> WatchNewConnectionAsync(Action<ObjectPath> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchConnectionRemovedAsync(Action<ObjectPath> handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<SettingsProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class SettingsProperties
    {
        private ObjectPath[] _connections = default(ObjectPath[]);
        public ObjectPath[] Connections
        {
            get
            {
                return _connections;
            }

            set
            {
                _connections = (value);
            }
        }

        private string _hostname = default(string);
        public string Hostname
        {
            get
            {
                return _hostname;
            }

            set
            {
                _hostname = (value);
            }
        }

        private bool _canModify = default(bool);
        public bool CanModify
        {
            get
            {
                return _canModify;
            }

            set
            {
                _canModify = (value);
            }
        }
    }

    public static class SettingsExtensions
    {
        public static Task<ObjectPath[]> GetConnectionsAsync(this ISettings o) => o.GetAsync<ObjectPath[]>("Connections");
        public static Task<string> GetHostnameAsync(this ISettings o) => o.GetAsync<string>("Hostname");
        public static Task<bool> GetCanModifyAsync(this ISettings o) => o.GetAsync<bool>("CanModify");
    }

    [DBusInterface("org.freedesktop.NetworkManager.Settings.Connection")]
    public interface IConnection : IDBusObject
    {
        Task UpdateAsync(IDictionary<string, IDictionary<string, object>> Properties);
        Task UpdateUnsavedAsync(IDictionary<string, IDictionary<string, object>> Properties);
        Task DeleteAsync();
        Task<IDictionary<string, IDictionary<string, object>>> GetSettingsAsync();
        Task<IDictionary<string, IDictionary<string, object>>> GetSecretsAsync(string SettingName);
        Task ClearSecretsAsync();
        Task SaveAsync();
        Task<IDictionary<string, object>> Update2Async(IDictionary<string, IDictionary<string, object>> Settings, uint Flags, IDictionary<string, object> Args);
        Task<IDisposable> WatchUpdatedAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchRemovedAsync(Action handler, Action<Exception> onError = null);
        Task<T> GetAsync<T>(string prop);
        Task<ConnectionProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class ConnectionProperties
    {
        private bool _unsaved = default(bool);
        public bool Unsaved
        {
            get
            {
                return _unsaved;
            }

            set
            {
                _unsaved = (value);
            }
        }

        private uint _flags = default(uint);
        public uint Flags
        {
            get
            {
                return _flags;
            }

            set
            {
                _flags = (value);
            }
        }

        private string _filename = default(string);
        public string Filename
        {
            get
            {
                return _filename;
            }

            set
            {
                _filename = (value);
            }
        }
    }

    public static class ConnectionExtensions
    {
        public static Task<bool> GetUnsavedAsync(this IConnection o) => o.GetAsync<bool>("Unsaved");
        public static Task<uint> GetFlagsAsync(this IConnection o) => o.GetAsync<uint>("Flags");
        public static Task<string> GetFilenameAsync(this IConnection o) => o.GetAsync<string>("Filename");
    }
}