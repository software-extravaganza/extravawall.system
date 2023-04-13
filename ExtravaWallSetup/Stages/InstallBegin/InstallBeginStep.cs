using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup.Stages.Install {
    public class InstallBeginStep : StepBase {
        public InstallBeginStep(InstallManager installManager) : base(installManager) {
        }

        public override string Name => "Install Starting...";

        public override StageType Stage => StageType.InstallBegin;

        public override short StepOrder => 0;

        public override bool AutoComplete => true;

        protected override async Task Execute() {
            Console.GetNewWriter().WriteLine("Beginning installation...");

            await Task.Delay(2000);
        }
    }
}
