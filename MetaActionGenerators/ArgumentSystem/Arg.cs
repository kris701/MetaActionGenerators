namespace MetaActionGenerators.ArgumentSystem
{
    public class Arg
    {
        public string Key { get; set; }
        public object? Value { get; set; } = null;

        public Arg(string key, object defaultValue)
        {
            Key = key;
            Value = defaultValue;
        }

        public Arg(string key)
        {
            Key = key;
            Value = null;
        }
    }
}
