using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtravaWall.Network;
using Tmds.DBus;
using NetworkManager.DBus;
using ReactiveUI;
using Terminal.Gui;
using ExtravaWallSetup.GUI.Components;
using EnumFastToStringGenerated;

namespace ExtravaWallSetup.Stages.Initialization {
    public class SystemInfoStep : StepBase {
        public override string Name => "Gathering System Info";

        public override StageType Stage => StageType.Initialize;

        public override short StepOrder => 0;



        public override void Initialize() {

            _isMonitoringNetwork.Subscribe(async (isMonitoringNetwork) => {
                if (!isMonitoringNetwork) {
                    await MonitorNetworkState();
                }
            });

            Install.InitializedTask.ContinueWith(task => _isMonitoringNetwork.OnNext(false));
        }


        protected override async Task Execute() {
            Console?.Add(new BannerView(BannerType.Welcome));

            Install.AddOrUpdateSystemInfo("Architecture", RuntimeInformation.OSArchitecture.ToString());
            Install.AddOrUpdateSystemInfo("Dotnet", RuntimeInformation.FrameworkDescription.ToString());
            Install.AddOrUpdateSystemInfo("OS Type", RuntimeInformation.OSDescription.ToString());
            Install.AddOrUpdateSystemInfo("OS Id", RuntimeInformation.RuntimeIdentifier.ToString());
            Install.AddOrUpdateSystemInfo("Current Dir", AppContext.BaseDirectory.ToString());

            await Task.CompletedTask;
        }

        private Subject<bool> _isMonitoringNetwork = new Subject<bool>();

        public SystemInfoStep(InstallManager installManager) : base(installManager) {
        }

        private async Task MonitorNetworkState() {
            if (Install.NetworkManager is null) {
                return;
            }

            var devices = await Install.NetworkManager.GetDevicesAsync();
            if (devices != null) {
                foreach (var devicePath in devices) {
                    var device = Install.DbusSystem.CreateProxy<IDevice>("org.freedesktop.NetworkManager", devicePath);
                    //var deviceType = await device.GetDeviceTypeAsync();

                    var interfaceName = await device.GetInterfaceAsync();
                    var propertyKeyPrefix = $"net {interfaceName}";
                    var updateState = async (IDevice device, DeviceState state) => {
                        var stateDescription = DeviceStateEnumExtensions.ToDisplayFast(state);
                        var ipv4Config = await device.GetIp4ConfigAsync();
                        var ips = await ipv4Config.GetAddressesAsync();
                        var addresses = NetworkHelpers.ConvertUintToIp4Addresses(ips);


                        if (addresses.Count > 0) {
                            for (var index = 0; index < addresses.Count; index++) {
                                var address = addresses[index];
                                var propertyKey = addresses.Count > 1 ? $"{propertyKeyPrefix} ({index})" : propertyKeyPrefix;
                                var descriptionPostfix = state == DeviceState.Activated ? string.Empty : $" ({stateDescription})";
                                var description = $"{address}{descriptionPostfix}";
                                Install.AddOrUpdateSystemInfo(propertyKey, description);
                            }
                        } else {
                            Install.AddOrUpdateSystemInfo(propertyKeyPrefix, stateDescription);
                        }
                    };

                    var currentState = await device.GetStateAsync();
                    await updateState(device, currentState);
                    //var IDisposable currentListener;
                    _isMonitoringNetwork.OnNext(true);
                    await device.WatchStateChangedAsync(
                        change => updateState(device, change.newState),
                        (ex) => {
                            /* todo: log exception */
                            Application.MainLoop.Invoke(() => Install.RemoveSystemInfoByPattern(propertyKeyPrefix));
                            _isMonitoringNetwork.OnNext(false);
                        });

                    // await device.WatchPropertiesAsync(changes => {
                    //     ulong txBytes = changes.Changed.Where(c => c.Key == "TxBytes").Select(c => (ulong)c.Value).FirstOrDefault();
                    //     ulong rxBytes = changes.Changed.Where(c => c.Key == "RxBytes").Select(c => (ulong)c.Value).FirstOrDefault();
                    //     Install.AddOrUpdateSystemInfo("TxBytes", txBytes.ToString());
                    //     Install.AddOrUpdateSystemInfo( "RxBytes", rxBytes.ToString());
                    // });
                }
            }
        }
    }
}