namespace ExtravaWallSetup.GUI.Framework
{
    public interface ICommandView
    {
        public void WriteStandardLine(string output);
        public void WriteErrorLine(string output);
        public void WriteExceptionLine(string output);
    }
}