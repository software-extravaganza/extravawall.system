namespace ExtravaCore.Commands;

internal class NoCommandResultConversionToTypeException : Exception {
    private readonly string _conversionTypeName;

    public NoCommandResultConversionToTypeException(string conversionTypeName) {
        _conversionTypeName = conversionTypeName;
    }

    public NoCommandResultConversionToTypeException()
        : base() { }

    public NoCommandResultConversionToTypeException(string? message, Exception? innerException)
        : base(message, innerException) { }

    public override string Message =>
        $"No CommandResult conversion exists for this type {_conversionTypeName}.";
}
