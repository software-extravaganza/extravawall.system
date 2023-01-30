namespace ExtravaWallSetup.Commands.Framework
{
    internal class NoCommandResultConversionToTypeException : Exception
    {
        private string _conversionTypeName;

        public NoCommandResultConversionToTypeException(string conversionTypeName)
        {
            _conversionTypeName = conversionTypeName;
        }
        public override string Message => $"No CommandResult conversion exists for this type {_conversionTypeName}.";
    }
}