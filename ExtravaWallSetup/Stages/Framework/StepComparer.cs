namespace ExtravaWallSetup.Stages.Framework {
    public class StepComparer : IComparer<IStep> {
        public int Compare(IStep? x, IStep? y) {
            if (x is null && y is null) {
                return 0;
            }
            else if (x is null) {
                return -1;
            }
            else if (y is null) {
                return 1;
            }

            return x.StepOrder.CompareTo(y.StepOrder);
        }
    }
}
