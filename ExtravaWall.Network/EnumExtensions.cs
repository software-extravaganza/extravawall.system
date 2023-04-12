namespace ExtravaWall.Network; 

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

        if (attributes != null && attributes.Length > 0)
        {
            return attributes[0].Description;
        }

        return value.ToString();
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class DescriptionAttribute : Attribute
{
    public string Description { get; private set; }

    public DescriptionAttribute(string description)
    {
        Description = description;
    }
}