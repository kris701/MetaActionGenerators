namespace MetaActionGenerators.ArgumentSystem
{
    public class ArgsHandler
    {
        private List<Arg> Args { get; }

        public ArgsHandler()
        {
            Args = new List<Arg>();
        }

        public ArgsHandler(List<Arg> args, Dictionary<string, string> generatorArgs)
        {
            Args = args;
            HandleArgs(generatorArgs);
        }
        private void HandleArgs(Dictionary<string, string> generatorArgs)
        {
            var toSet = Args.Where(x => x.Value == null).ToList();
            foreach (var key in generatorArgs.Keys)
            {
                var target = Args.FirstOrDefault(x => x.Key == key);
                if (target != null)
                {
                    target.Value = generatorArgs[key];
                    toSet.RemoveAll(x => x.Key == key);
                }
            }
            if (toSet.Count > 0)
            {
                foreach (var set in toSet)
                    Console.WriteLine($"Missing argument: '{toSet[0].Key}', {toSet[0].Description}");
                throw new Exception("Missing Arguments");
            }
        }

        public T GetArgument<T>(string key)
        {
            var target = Args.FirstOrDefault(x => x.Key == key);
            if (target == null)
                throw new ArgumentNullException($"No argument with the key '{key}'!");
            if (target.Value == null)
                throw new ArgumentNullException($"Argument '{key}' was not set!");
            return (T)Convert.ChangeType(target.Value, typeof(T));
        }
    }
}
