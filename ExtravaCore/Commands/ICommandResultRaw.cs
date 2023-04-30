namespace ExtravaCore.Commands;
public interface ICommandResultRaw : ICommandResult {
    string StandardOutput { get; set; }
    string ErrorOutput { get; set; }
    string ExceptionOutput { get; set; }
}