namespace CommunalService.Domain.Enums;

public record CusromerSourceCode()
{
    public static CusromerSourceCode MiniApp { get; } = new(001, "MiniApp", "MP");
    public int Value { get; }
    public string Name { get; }
    public string Code { get; }

    public CusromerSourceCode(int value, string name, string code) : this()
    {
        Value = value;
        Name = name;
        Code = code;
    }

    public static CusromerSourceCode GetSource(int value)
    {
        return value switch
        {
            001 => MiniApp,
            _ => throw new ArgumentException("Invalid value"),
        };
    }

    public static CusromerSourceCode GetSource(string code)
    {
        return code switch
        {
            "MP" => MiniApp,
            _ => throw new ArgumentException("Invalid code"),
        };
    }

    public static bool CheckSource(int value)
    {
        try
        {
            GetSource(value);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
};