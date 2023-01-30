using DynamicData;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Initialization;
using ExtravaWallSetup.Stages.Install;
using ExtravaWallSetup.Stages.InstallCheckSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup.Stages.Framework {
    public class StageManager {
        private IDictionary<StageType, ICollection<IStep>> _allSteps = new Dictionary<StageType, ICollection<IStep>>();
        private IDictionary<StageType, IEnumerator<IStep>> _stageEnumerators = new Dictionary<StageType, IEnumerator<IStep>>();
        private InstallManager _installManager;
        private DefaultScreen _defaultScreen;
        private string _exitError = string.Empty;

        public StageType CurrentStage { get; private set; }
        public IStep CurrentStep { get; private set; } = new EmptyStep();
        public bool CurrentStageIsOnLastStep { get; private set; }

        private List<StageType> _remainingStages;

        public StageManager(InstallManager installManager, DefaultScreen defaultScreen) {
            _installManager = installManager;
            _defaultScreen = defaultScreen;
            _remainingStages = ((StageType[])Enum.GetValues(typeof(StageType))).OrderBy(x => x).ToList();
            setCurrentStage(StageType.Initialize);

            // Add steps
            addStep(new SystemInfoStep());
            addStep(new MenuStageStep());
            addStep(new InstallBeginStep());
            addStep(new InstallCheckSystemStep());

            addStep(new EndStage());
        }

        public void RequestEndOnNextStep(string error) {
            _exitError = error;
        }

        public async Task AdvanceStepsUntilTheEndOfTheStage() {
            do {
                await AdvanceToNextStep();
            } while (!CurrentStageIsOnLastStep);
        }

        public async Task AdvanceToNextStep() {
            if (!string.IsNullOrWhiteSpace(_exitError)) {
                await SkipToStageAndRemoveSkipped(StageType.End, true);
                return;
            }

            await CurrentStep.BeginComplete();
            CurrentStep = enumerateNextStep();
            await beginStep();
        }

        public async Task AdvanceToStage(StageType stage) {
            do {
                await AdvanceToNextStep();
            } while (CurrentStage != stage);
        }

        public async Task SkipToStageAndRemoveSkipped(StageType stage, bool executeFirstStepOfStage = true) {
            if (!string.IsNullOrWhiteSpace(_exitError) && stage != StageType.End) {
                return;
            }

            do {
                await CurrentStep.BeginComplete();
                CurrentStep = enumerateNextStep();
            } while (CurrentStage != stage);

            if (executeFirstStepOfStage) {
                await beginStep();
            }
        }

        private void setCurrentStage(StageType stageType) {
            while (_remainingStages.FirstOrDefault() != stageType) {
                if (!setNextStage()) {
                    return;
                }
            }
        }

        private bool setNextStage() {
            if (_remainingStages.Count > 0) {
                _remainingStages.RemoveAt(0);
                CurrentStage = _remainingStages.FirstOrDefault();
                return true;
            }

            return false;
        }

        private StageType? getCurrentStage() {
            if (_remainingStages.Count > 0) {
                return _remainingStages[0];
            }

            return null;
        }

        private IStep enumerateNextStep() {
            var currentStage = getCurrentStage();
            if (currentStage == null) {
                var end = new EndStage();
                end.Initialize(_installManager);
                CurrentStageIsOnLastStep = true;
                return end;
            }

            if (!_stageEnumerators.ContainsKey(currentStage.Value)) {
                _stageEnumerators.Add(currentStage.Value, stageEnumerator(currentStage.Value));
            }

            var enumerator = _stageEnumerators[currentStage.Value];
            if (!enumerator.MoveNext()) {
                setNextStage();
                return enumerateNextStep();
            }

            var stage = enumerator.Current;
            CurrentStageIsOnLastStep = _allSteps[currentStage.Value].IndexOf(stage) == _allSteps[currentStage.Value].Count - 1;
            return stage;
        }

        private IEnumerator<IStep?> stageEnumerator(StageType stageType) {
            if (!_allSteps.ContainsKey(stageType)) {
                yield break;
            }

            foreach (var stage in _allSteps[stageType]) {
                yield return stage;
            }
        }

        private void addStep(IStep step) {
            if (!_allSteps.ContainsKey(step.Stage)) {
                _allSteps.Add(step.Stage, new SortedSet<IStep>(new StepComparer()));
            }

            step.Initialize(_installManager);
            var stage = _allSteps[step.Stage];
            stage.Add(step);
        }

        private async Task beginStep() {
            if (!string.IsNullOrWhiteSpace(_exitError) && CurrentStep is EndStage endStep) {
               endStep.ExitError = _exitError;
            }

            await CurrentStep.BeginExecute();
            CurrentStep.StepTask.ToObservable().Subscribe(async unit => await currentStepCompleteAsync(unit));
        }

        private async Task currentStepCompleteAsync(Unit obj) {
            await AdvanceToNextStep();
        }
    }
}
