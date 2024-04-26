namespace MetaActionGenerators.ArgumentSystem
{
    public class Arg
    {
        public string Key { get; set; }
        public object? Value { get; set; } = null;
        public string Description { get; set; }

        public Arg(string key, object defaultValue, string description)
        {
            Key = key;
            Value = defaultValue;
            Description = description;
        }

        public Arg(string key, string description)
        {
            Key = key;
            Value = null;
            Description = description;
        }
    }
}
