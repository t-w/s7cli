namespace S7Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new OptionParser();
            parser.Parse(args);
        }
    }
}
