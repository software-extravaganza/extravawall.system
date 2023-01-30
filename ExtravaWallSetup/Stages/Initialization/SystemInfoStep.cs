using ExtravaWallSetup.Commands;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup.Stages.Initialization {
    public class SystemInfoStep : StepBase {
        public override string Name => "Gathering System Info";

        public override StageType Stage => StageType.Initialize;

        public override short StepOrder => 0;

        protected override async Task Execute() {
            Console.Add(new BannerView());

            Install.AddOrUpdateSystemInfo("Architecture", RuntimeInformation.OSArchitecture.ToString());
            Install.AddOrUpdateSystemInfo("Dotnet", RuntimeInformation.FrameworkDescription.ToString());
            Install.AddOrUpdateSystemInfo("OS Type", RuntimeInformation.OSDescription.ToString());
            Install.AddOrUpdateSystemInfo("OS Id", RuntimeInformation.RuntimeIdentifier.ToString());
            Install.AddOrUpdateSystemInfo("Current Dir", AppContext.BaseDirectory.ToString());


            await Task.CompletedTask;
        }
    }
}
