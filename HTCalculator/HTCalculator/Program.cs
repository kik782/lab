using System;
using System.Collections.Generic;
class Program
{
    const int LowercaseCount = 26; 
    const int UppercaseCount = 26;
    const int DigitCount = 10;
    const int SymbolCount = 33;        

    static readonly string[] CommonPasswords =
    {
        "123456", "123456789", "qwerty", "password", "111111",
        "12345678", "abc123", "1234567", "password1", "12345"
    };

    static void Main()
    {
        Console.WriteLine("=== Password Crack Time Calculator ===");
        Console.WriteLine("Empty line to exit.");

        List<AttackScenario> scenarios = CreateScenarios();

        while (true)
        {
            Console.WriteLine();
            Console.Write("Password: ");
            string password = Console.ReadLine() ?? "";

            if (password == "")
            {
                break;
            }

            if (IsCommon(password))
            {
                Console.WriteLine("This is one of the most common passwords.");
                Console.WriteLine("A dictionary attack finds it instantly.");
            }
            else
            {
                PrintEstimate(password, scenarios);
            }
        }

        Console.WriteLine("GOODBYE!");
    }

    static List<AttackScenario> CreateScenarios()
    {
        return new List<AttackScenario>
        {
            new AttackScenario("Online, attempts limited", 100.0 / 3600),
            new AttackScenario("Online, no limits", 10),
            new AttackScenario("Stolen database, slow hash", 10_000),
            new AttackScenario("Stolen database, fast hash", 10_000_000_000),
            // по заданию со звездочкой добавил 5 сценарий
            new AttackScenario("One videocard, MD5", 100_000_000_000)
        };
    }

    static void PrintEstimate(string password, List<AttackScenario> scenarios)
    {
        int poolSize = GetPoolSize(password);
        
        double combinations = Math.Pow(poolSize, password.Length);

        Console.WriteLine($"Length: {password.Length}, character pool: {poolSize}");
        Console.WriteLine($"Combinations: {FormatBigNumber(combinations)}");

        foreach (AttackScenario scenario in scenarios)
        {
            double seconds = combinations / scenario.GuessesPerSecond;
            // по зданию добавил колонку со средним временем
            double average = seconds / 2;
            Console.WriteLine($"  {scenario.Name,-28} {FormatDuration(seconds),-36} {FormatDuration(average)}");
        }
        
    }
    // этот метод определяет набор символов пароля.
    static int GetPoolSize(string password)
    {
        bool hasLower = false;
        bool hasUpper = false;
        bool hasDigit = false;
        bool hasSymbol = false;

        foreach (char c in password)
        {
            if (c >= 'a' && c <= 'z')
            {
                hasLower = true;
            }
            else if (c >= 'A' && c <= 'Z')
            {
                hasUpper = true;
            }
            else if (c >= '0' && c <= '9')
            {
                hasDigit = true;
            }
            else
            {
                hasSymbol = true;
            }
        }

        int poolSize = 0;

        if (hasLower)
        {
            poolSize += LowercaseCount;
        }

        if (hasUpper)
        {
            poolSize += UppercaseCount;
        }

        if (hasDigit)
        {
            poolSize += DigitCount;
        }

        if (hasSymbol)
        {
            poolSize += SymbolCount;
        }

        return poolSize;
    }
    // этот метод сравнивает ввод со списком и игнорирует регистр.
    static bool IsCommon(string password)
    {
        foreach (string common in CommonPasswords)
        {
            if (string.Equals(password, common, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    //этот метод записывает большие числа в виде "мантисса × 10 в степени".
    static string FormatBigNumber(double number)
    {
        if (number < 1_000_000)
        {
            
            return number.ToString("N0");
        }

        int exponent = (int)Math.Floor(Math.Log10(number));
        double mantissa = number / Math.Pow(10, exponent);
        return $"{mantissa:0.0} x 10^{exponent}";
    }

    //этот метод проверяет каждое условие, не меньше ли время следующей единицы измерения
    static string FormatDuration(double seconds)
    {
        const double Minute = 60;
        const double Hour = 60 * Minute;
        const double Day = 24 * Hour;
        const double Year = 365.25 * Day;
        const double UniverseAgeYears = 13_800_000_000;

        if (seconds < 1)
        {
            return "less than a second";
        }

        if (seconds < Minute)
        {
            return $"{seconds:0} seconds";
        }

        if (seconds < Hour)
        {
            return $"{seconds / Minute:0} minutes";
        }

        if (seconds < Day)
        {
            return $"{seconds / Hour:0} hours";
        }

        if (seconds < Year)
        {
            return $"{seconds / Day:0} days";
        }

        double years = seconds / Year;

        if (years < 1_000)
        {
            return $"{years:0} years";
        }

        if (years < 1_000_000)
        {
            return $"{years / 1_000:0} thousand years";
        }

        if (years < 1_000_000_000)
        {
            return $"{years / 1_000_000:0} million years";
        }

        if (years < UniverseAgeYears)
        {
            return $"{years / 1_000_000_000:0} billion years";
        }

        return "longer than the age of the Universe";
    }
}
// этот класс собирает два связанных значения в один объект.
class AttackScenario
{
    public string Name { get; }
    public double GuessesPerSecond { get; }
    public AttackScenario(string name, double guessesPerSecond)
    {
        Name = name;
        GuessesPerSecond = guessesPerSecond;
    }
}

