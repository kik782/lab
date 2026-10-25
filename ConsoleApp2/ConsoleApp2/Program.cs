using System;

class Program
{
    static readonly int[] RiskyPorts = { 21, 23, 445, 3389, 3306, 5900 };
    static readonly string[] RiskyNames = { "FTP", "Telnet", "SMB", "RDP", "MySQL", "VNC" };
    static readonly string[] RiskyReasons =
    {
        "sends passwords in plain text",
        "sends everything in plain text; the Mirai botnet spread through it",
        "the WannaCry worm spread through this port in 2017",
        "remote desktop, a common target for password guessing",
        "a database should never be reachable from the internet",
        "remote screen access, often with a weak password"
        };
    static readonly int[] RiskPoints = { 2, 3, 3, 3, 2, 2 };
    static readonly int[] SafePorts = { 22, 80, 443 };
    static readonly string[] SafeNames = { "SSH", "HTTP", "HTTPS" };
    static void Main()
    {
        Console.WriteLine("=== Port Risk Analyzer ===");
        Console.WriteLine("Enter open ports separated by commas. Empty line to exit.");
        while (true)
        {
            Console.WriteLine();
            Console.Write("Ports: ");
            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }
            int[] ports = ParsePorts(input);
            if (ports.Length == 0)
            {
                Console.WriteLine("No ports to analyze. Try again.");
            }
            else
            {
                AnalyzePorts(ports);
            }
        }
        Console.WriteLine("Goodbye!");
    }

    static int[] ParsePorts(string input)
    {
        string[] parts = input.Split(
        ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int[] ports = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int port) || port < 1 || port > 65535)
            {
                Console.WriteLine($"Invalid port: \"{parts[i]}\".");
                Console.WriteLine("A port is a whole number from 1 to 65535.");
                return Array.Empty<int>();
            }
            ports[i] = port;
        }
        return ports;
    }

    static void AnalyzePorts(int[] ports)
    {
        int riskyCount = 0;
        int safeCount = 0;
        int unknownCount = 0;
        int riskScore = 0;
        foreach (int port in ports)
        {
            int riskIndex = FindRiskIndex(port);
            int safeIndex = FindSafeIndex(port);
            if (riskIndex != -1)
            {
                string name = RiskyNames[riskIndex];
                string reason = RiskyReasons[riskIndex];
                Console.WriteLine($"[RISK] {port} ({name}): {reason}");
                riskyCount++;
                riskScore += RiskPoints[riskIndex];
            }
            else if (safeIndex != -1)
            {
                string name = SafeNames[safeIndex];
                Console.WriteLine($"[ OK ] {port} ({name})");
                safeCount++;
            }
            else
            {
                Console.WriteLine($"[ ?? ] {port}: unknown service, check it manually");
                unknownCount++;
            }
        }
        Console.WriteLine($"Risky ports: {riskyCount} of {ports.Length}");
        Console.WriteLine($"Safe ports: {safeCount} of {ports.Length}");
        Console.WriteLine($"Unknown ports: {unknownCount} of {ports.Length}");
        Console.WriteLine($"Risk score: {riskScore}");
        Console.WriteLine($"Verdict: {GetVerdict(riskScore)}");
    }

    static int FindRiskIndex(int port)
    {
        for (int i = 0; i < RiskyPorts.Length; i++)
        {
            if (RiskyPorts[i] == port)
            {
                return i;
            }
        }
        return -1;
    }


    static string GetVerdict(int riskScore)
    {
        if (riskScore == 0)
        {
            return "NO KNOWN RISKS";
        }
        if (riskScore <= 2)
        {
            return "NEEDS ATTENTION";
        }
        return "CRITICAL";
    }

    static int FindSafeIndex(int port)
    {
        for (int i = 0; i < SafePorts.Length; i++)
        {
            if (SafePorts[i] == port)
            {
                return i;
            }
        }
        return -1;
    }
}

