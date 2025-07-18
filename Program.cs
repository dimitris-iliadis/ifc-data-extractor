namespace IfcDataExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Δεν έχει οριστεί αρχείο IFC.");
                Environment.Exit(-1);
            }

            var ifcFile = args[0];
            var debug = args.Contains("--debug");

            SpaceAnalyzer.ProcessIfcFile(ifcFile, debug);
        }
    }
}