namespace GfxEditor;

public record struct AnimationDef(string Move, int AnimationNumber, float Velocity, bool Override);

public class FileAnm
{
    private List<AnimationDef> _defs;

    public FileAnm(string contents)
    {
        _defs = Parse(contents);
    }

    public IReadOnlyList<AnimationDef> GetDefs() => _defs;

    private static List<AnimationDef> Parse(string text)
    {
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var definitions = new List<AnimationDef>();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("//"))
            {
                continue;
            }

            string[] fields = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            string move = fields[0];
            int animationNumber = int.Parse(fields[1]);
            float velocity = float.Parse(fields[2]);
            bool isOverride = fields.Length >= 4 && fields[3].Equals("override", StringComparison.OrdinalIgnoreCase);

            definitions.Add(new AnimationDef(move, animationNumber, velocity, isOverride));
        }

        return definitions;
    }
}