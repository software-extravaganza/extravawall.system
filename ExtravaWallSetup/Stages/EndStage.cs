using ExtravaWallSetup.GUI;
using ExtravaWallSetup.GUI.Components;
using ExtravaWallSetup.Stages.Framework;
using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup.Stages {
    public class EndStage : StepBase {
        private ExitView? _view;

        public EndStage(InstallManager installManager) : base(installManager) {
        }

        public override string Name => "End";

        public override StageType Stage => StageType.End;

        public override short StepOrder => 0;
        public string? ExitError { get; set; }

        protected override async Task Execute() {
            var isSuccess = string.IsNullOrWhiteSpace(ExitError);
            var banner = new BannerView(isSuccess ? BannerType.ExitSuccess : BannerType.ExitFail);
            var title = isSuccess ? (ustring)"Install Successful" : "Install failed";  //💪 😖
            var content = (ustring)$"{banner.Text}" + (isSuccess || ExitError is null ? string.Empty : $"{ExitError}"); //🙃 😩
            //var msg = isSuccess ? MessageBox.Query(title, content) : MessageBox.ErrorQuery(title, content);
            _view = new ExitView(isSuccess, title, content);
            _view.SetHeight(banner.BannerHeight + 6);
            Console.Add(_view);
            await Task.CompletedTask;
        }
    }
}
