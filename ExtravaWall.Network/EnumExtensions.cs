namespace ExtravaWall.Network;

public static class EnumExtensions {
    public static string GetDescription(this Enum value) {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) {
            return value.ToString();
        }

        if (fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Length > 0) {
            return attributes[0].Description;
        }

        return value.ToString();
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class DescriptionAttribute : Attribute {
    public string Description { get; }

    public DescriptionAttribute(string description) {
        Description = description;
    }
}