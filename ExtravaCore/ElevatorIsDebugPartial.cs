namespace ExtravaCore;
public partial class Elevator{
    [ExcludeFromCodeCoverage]
public static bool IsDebug {
    get {
#if DEBUG
        return true;
#else
        return false;
#endif
        }
    }
}