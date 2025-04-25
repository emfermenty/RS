class Program
{
    static void Main(string[] args)
    {
        // Пример запуска: 5000 127.0.0.1 5001 true
        if (args.Length < 3)
        {
            Console.WriteLine("Использование: <myPort> <nextIp> <nextPort> [true/false для токена]");
            return;
        }

        int myPort = int.Parse(args[0]);
        string nextIp = args[1];
        int nextPort = int.Parse(args[2]);
        bool startWithToken = args.Length > 3 && bool.Parse(args[3]);

        var node = new TokenRingNode(myPort, nextIp, nextPort, startWithToken);
        node.Start();
    }
}