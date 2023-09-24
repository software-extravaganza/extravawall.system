using Tmds.DBus;

namespace PrivilegedActionHelper;

[DBusInterface("software.extravaganza.extravawall.PrivilegedHelper")]
public interface IPrivilegedHelper : IDBusObject {
    Task<string> PerformPrivilegedTask(string input);
}


public class PrivilegedHelper : IPrivilegedHelper {
    public ObjectPath ObjectPath => new ObjectPath("/software/extravaganza/extravawall/PrivilegedHelper");
    public async Task<string> PerformPrivilegedTask(string input) {
        // Implement the privileged task logic here
        // ...
        await Task.CompletedTask;
        return $"Privileged task completed {input}";
    }
}