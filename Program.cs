using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

class Program
{
    // ===== Настройки смены =====

    // Смена 20 января 2027 года, с 08:00 до 20:00.
    static readonly DateTime ShiftStart = new DateTime(2027, 1, 20, 8, 0, 0);
    static readonly DateTime ShiftEnd = new DateTime(2027, 1, 20, 20, 0, 0);

    // В каком виде записано время в журнале: год-месяц-день часы:минуты.
    const string DateFormat = "yyyy-MM-dd HH:mm";

    // ===== Данные, которые программа копит, читая журнал =====

    // Кто сломан прямо сейчас: код станка -> время, когда он сломался.
    static Dictionary<string, DateTime> brokenNow = new Dictionary<string, DateTime>();

    // Сколько всего простоял каждый станок за смену.
    static Dictionary<string, TimeSpan> downtime = new Dictionary<string, TimeSpan>();

    // Сколько раз ломался каждый станок.
    static Dictionary<string, int> failureCount = new Dictionary<string, int>();

    // Станки, которые так и не починили до конца смены.
    static List<string> stillBroken = new List<string>();

    // Строки, которые не удалось прочитать.
    static List<string> rejected = new List<string>();

    // Строки, которые прочитать удалось, но в них что-то подозрительное.
    static List<string> warnings = new List<string>();

    // Самое позднее время, которое уже встречалось в журнале.
    static DateTime lastTime = DateTime.MinValue;

    static void Main()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "events.log");
        string[] lines = ReadLog(path);

        if (lines.Length == 0)
        {
            Console.WriteLine("Нечего обрабатывать.");
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            // i + 1, потому что люди считают строки с 1, а массив — с 0.
            ProcessLine(lines[i], i + 1);
        }

        CloseShift();

        string report = BuildReport();
        Console.WriteLine(report);

        SaveReports(report);
    }

    // Читает журнал. Если прочитать не вышло — сообщает и возвращает пустой массив.
    static string[] ReadLog(string path)
    {
        try
        {
            return File.ReadAllLines(path);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не удалось прочитать журнал: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    // Разбирает одну строку журнала: проверяет её и учитывает событие.
    static void ProcessLine(string line, int lineNumber)
    {
        if (line.Trim() == "")
        {
            return;
        }

        string[] parts = line.Split(';');

        if (parts.Length != 3)
        {
            rejected.Add($"Строка {lineNumber}: должно быть 3 части, а их {parts.Length}");
            return;
        }

        string timeText = parts[0].Trim();
        string code = parts[1].Trim();
        string eventType = parts[2].Trim();

        bool isDate = DateTime.TryParseExact(
            timeText, DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime time);

        if (!isDate)
        {
            rejected.Add($"Строка {lineNumber}: неверная дата «{timeText}»");
            return;
        }

        if (!IsValidCode(code))
        {
            rejected.Add($"Строка {lineNumber}: неверный код станка «{code}»");
            return;
        }

        if (eventType != "ОТКАЗ" && eventType != "ПОЧИНЕН")
        {
            rejected.Add($"Строка {lineNumber}: неизвестное событие «{eventType}»");
            return;
        }

        // Журнал пишется по порядку, время не может идти назад.
        // Если пошло — возможно, журнал исправляли вручную. Предупреждаем, но строку учитываем.
        if (time < lastTime)
        {
            warnings.Add($"Строка {lineNumber}: время пошло назад ({time:HH:mm} после {lastTime:HH:mm})");
        }
        else
        {
            lastTime = time;
        }

        if (eventType == "ОТКАЗ")
        {
            RegisterFailure(code, time, lineNumber);
        }
        else
        {
            RegisterRepair(code, time, lineNumber);
        }
    }

    // Правильный код: ровно 6 символов, начинается с "EQ-", дальше число от 1 до 15.
    static bool IsValidCode(string code)
    {
        if (code.Length != 6 || !code.StartsWith("EQ-", StringComparison.Ordinal))
        {
            return false;
        }

        bool isNumber = int.TryParse(code.Substring(3), out int number);
        return isNumber && number >= 1 && number <= 15;
    }

    // Станок сломался: запоминаем, когда, и прибавляем к счётчику отказов.
    static void RegisterFailure(string code, DateTime time, int lineNumber)
    {
        if (brokenNow.ContainsKey(code))
        {
            warnings.Add($"Строка {lineNumber}: {code} снова сломался, хотя ещё не починен");
            return;
        }

        brokenNow[code] = time;

        if (failureCount.ContainsKey(code))
        {
            failureCount[code]++;
        }
        else
        {
            failureCount[code] = 1;
        }
    }

    // Станок починили: считаем, сколько он простоял, и убираем из сломанных.
    static void RegisterRepair(string code, DateTime time, int lineNumber)
    {
        if (!brokenNow.TryGetValue(code, out DateTime brokenAt))
        {
            warnings.Add($"Строка {lineNumber}: {code} починен, хотя не ломался");
            return;
        }

        if (time < brokenAt)
        {
            warnings.Add($"Строка {lineNumber}: {code} починен раньше, чем сломался");
            return;
        }

        AddDowntime(code, time - brokenAt);
        brokenNow.Remove(code);
    }

    // Прибавляет простой к сумме станка. Если станка ещё нет в словаре — заводит его.
    static void AddDowntime(string code, TimeSpan duration)
    {
        if (downtime.ContainsKey(code))
        {
            downtime[code] += duration;
        }
        else
        {
            downtime[code] = duration;
        }
    }

    // Кто остался сломанным к концу смены, простоял до самого конца.
    static void CloseShift()
    {
        foreach (KeyValuePair<string, DateTime> pair in brokenNow)
        {
            AddDowntime(pair.Key, ShiftEnd - pair.Value);
            stillBroken.Add(pair.Key);
        }

        brokenNow.Clear();
    }

    // Собирает текст отчёта.
    static string BuildReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine($"ОТЧЁТ ЗА СМЕНУ {ShiftStart:dd.MM.yyyy} ({ShiftStart:HH:mm}–{ShiftEnd:HH:mm})");
        report.AppendLine();
        report.AppendLine($"{"Станок",-11}{"Отказов",-10}Простой");

        TimeSpan total = TimeSpan.Zero;
        string worstCode = "";
        TimeSpan worstTime = TimeSpan.Zero;

        foreach (string code in GetSortedCodes())
        {
            TimeSpan time = downtime[code];
            int failures = failureCount[code];

            string mark = "";

            if (stillBroken.Contains(code))
            {
                mark = "  ДО СИХ ПОР СЛОМАН";
            }

            report.AppendLine($"{code,-11}{failures,-10}{FormatDuration(time)}{mark}");

            total += time;

            if (time > worstTime)
            {
                worstTime = time;
                worstCode = code;
            }
        }

        report.AppendLine();
        report.AppendLine($"Общий простой: {FormatDuration(total)}");

        if (worstCode != "")
        {
            report.AppendLine($"Хуже всех: {worstCode} ({FormatDuration(worstTime)})");
        }

        report.AppendLine();
        report.AppendLine("Нужна проверка (2 отказа и больше)");
        bool print = false;
        foreach (string code in GetSortedCodes())
        {
            if (failureCount[code] >= 2)
            {
                report.AppendLine($"{code} - отказов: {failureCount[code]}");
                print = true;
            }
        }
        if (!print)
        {
            report.AppendLine("нет");
        }

        AppendList(report, "Отброшено строк", rejected);
        AppendList(report, "Предупреждения", warnings);

        return report.ToString();
    }

    // Собирает таблицу CSV для Excel.
    static string BuildCsv()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("станок;отказов;простой_часов");

        foreach (string code in GetSortedCodes())
        {
            // InvariantCulture: дробная часть всегда через точку (2.17), на любом компьютере.
            string hours = downtime[code].TotalHours.ToString("0.00", CultureInfo.InvariantCulture);
            csv.AppendLine($"{code};{failureCount[code]};{hours}");
        }

        return csv.ToString();
    }

    // Сохраняет отчёт и CSV в папку reports рядом с программой.
    static void SaveReports(string report)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "reports");
        string date = ShiftStart.ToString("yyyy-MM-dd");

        string reportPath = Path.Combine(folder, $"shift-report-{date}.txt");
        string csvPath = Path.Combine(folder, $"shift-summary-{date}.csv");

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(reportPath, report);
            File.WriteAllText(csvPath, BuildCsv());

            Console.WriteLine($"Сохранено: {reportPath}");
            Console.WriteLine($"Сохранено: {csvPath}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Нет прав на запись в папку reports.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не удалось сохранить отчёт: {ex.Message}");
        }
    }

    // У словаря нет порядка, поэтому коды станков сортируем перед выводом.
    static List<string> GetSortedCodes()
    {
        List<string> codes = new List<string>(downtime.Keys);
        codes.Sort();
        return codes;
    }

    // Простой в виде "часы:минуты". TotalHours, а не Hours: Hours обнуляется после суток.
    static string FormatDuration(TimeSpan time)
    {
        return $"{(int)time.TotalHours}:{time.Minutes:00}";
    }

    // Дописывает в отчёт заголовок и список строк под ним.
    static void AppendList(StringBuilder report, string title, List<string> items)
    {
        report.AppendLine();
        report.AppendLine($"{title}: {items.Count}");

        foreach (string item in items)
        {
            report.AppendLine($"  {item}");
        }
    }
}