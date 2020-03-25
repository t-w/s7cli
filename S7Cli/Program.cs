namespace S7Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            var parser = new OptionParser();
            parser.Parse(args);
            return parser.ReturnValue;
        }
    }
}
