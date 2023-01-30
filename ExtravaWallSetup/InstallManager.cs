using DynamicData;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.GUI.Framework;
using ExtravaWallSetup.Stages.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup {
    public class InstallManager {
        public static InstallManager Instance => _instance;
        public VirtualConsoleManager Console { get; private set; }

        private static InstallManager _instance = new InstallManager();
        private DefaultScreen _defaultScreen;
        private StageManager _stageManager;
        private IDictionary<string, System.Data.DataRow> _systemInfo = new Dictionary<string, System.Data.DataRow>();

        public InstallManager() {
            _instance = this;
            var layoutInitializedTaskCompletionSource = new TaskCompletionSource();
            layoutInitializedTaskCompletionSource.Task.ToObservable().Subscribe(async unit => await OnConsoleLayoutInitializedAsync(unit));
            _defaultScreen = new DefaultScreen(layoutInitializedTaskCompletionSource);
            _stageManager = new StageManager(this, DefaultScreen);
        }

        private async Task OnConsoleLayoutInitializedAsync(Unit unit) {
            await Initialize();
        }


        public async Task Initialize() {
            Console = new VirtualConsoleManager(this, _defaultScreen, _defaultScreen.VirtualConsoleView);
            if (_stageManager.CurrentStage == StageType.Initialize) {
                await _stageManager.AdvanceToStage(StageType.Menu);
            }
        }

        public async Task InstallAsync() {
            await _stageManager.SkipToStageAndRemoveSkipped(StageType.InstallBegin);
        }

        public async Task Recover() {

        }

        public void Exit() {
            Terminal.Gui.Application.Shutdown();
        }

        
        public void RequestEndOnNextStep(string reason) {
            _stageManager.RequestEndOnNextStep(reason);
        }

        public DefaultScreen DefaultScreen => _defaultScreen;

        public System.Data.DataRow AddOrUpdateSystemInfo(string property, string value) {
            if (_systemInfo.ContainsKey(property)) {
                var matchIndex = _systemInfo.Keys.IndexOf(property);
                var matchRow = _systemInfo[property];
                matchRow.ItemArray = new object[] { property, value };
                return matchRow;
            }

            List<object> rowData = new List<object>() {
                property,
                value
            };

            var resultRow =  _defaultScreen.InfoTable.Table.Rows.Add(rowData.ToArray());
            _systemInfo.Add(property, resultRow);
            return resultRow;

        }
    }
}
