using ExtravaWallSetup.GUI;
using ExtravaWallSetup.GUI.Components;
using ExtravaWallSetup.Stages.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup.Stages.Initialization {
    public class MenuStageStep : StepBase {
        private StartMenuView? _view;

        public MenuStageStep(InstallManager installManager) : base(installManager) {
        }

        public override string Name => "Menu";

        public override StageType Stage => StageType.Menu;

        public override short StepOrder => 0;

        protected override async Task Complete() {
            if (_view != null) {
                _view.Enabled = false;
            }

            await Task.CompletedTask;
        }

        protected override async Task Execute() {
            _view = new StartMenuView(Install);
            Console?.Add(_view);

            await Task.CompletedTask;
        }
    }
}
