using System;
using System.IO;
using System.Numerics;

class Program
{
    const int MinGrade = 1;
    const int MaxGrade = 5;

    static void Main()
    {
        Console.WriteLine("=== Gradebook ===");

        string path = Path.Combine(AppContext.BaseDirectory, "grades.txt");
        Student[] students = LoadStudents(path);

        if (students.Length == 0)
        {
            Console.WriteLine("No students to show.");
            return;
        }

        Console.WriteLine();
        PrintReport(students);
        PrintNeedsHelp(students);
        SortByAverage(students);

        Student best = FindBest(students);

        Console.WriteLine();
        Console.WriteLine($"Best student: {best.Name} ({best.Average():0.00})");
        Console.WriteLine($"Group average: {GroupAverage(students):0.00}");
    }

    static Student[] LoadStudents(string path)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File not found: {path}");
            return Array.Empty<Student>();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Could not read the file: {ex.Message}");
            return Array.Empty<Student>();
        }

        Student[] buffer = new Student[lines.Length];
        int count = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            Student? student = ParseStudent(lines[i], i + 1);

            if (student != null)

            {
                buffer[count] = student;
                count++;
            }
        }

        Student[] students = new Student[count];

        for (int i = 0; i < count; i++)
        {
            students[i] = buffer[i];
        }

        return students;
    }

    static Student? ParseStudent(string line, int lineNumber)
    {
        if (line.Trim() == "")
        {
            return null;
        }

        string[] parts = line.Split(';');
        string name = parts[0].Trim();

        if (name == "" || parts.Length < 2)
        {
            Console.WriteLine($"Line {lineNumber}: no name or no grades.");
            return null;
        }

        int[] grades = new int[parts.Length - 1];

        for (int i = 1; i < parts.Length; i++)
        {
            int grade;

            try
            {
                grade = int.Parse(parts[i].Trim());
            }
            catch (FormatException)
            {
                Console.WriteLine($"Line {lineNumber}: \"{parts[i]}\" is not a number.");
                return null;
            }
            catch (OverflowException)
            {
                Console.WriteLine($"Line {lineNumber}: \"{parts[i]}\" is too big.");
                return null;
            }

            if (grade < MinGrade || grade > MaxGrade)
            {
                Console.WriteLine($"Line {lineNumber}: grade {grade} is out of range.");
                return null;
            }

            grades[i - 1] = grade;

        }

        return new Student(name, grades);
    }

    static void PrintReport(Student[] students)
    {
        foreach (Student student in students)
        {
            string grades = string.Join(" ", student.Grades);
            Console.WriteLine($"{student.Name,-20} {grades,-12} {student.Average():0.00}");
        }
    }

    static Student FindBest(Student[] students)
    {
        Student best = students[0];

        for (int i = 1; i < students.Length; i++)
        {
            if (students[i].Average() > best.Average())
            {
                best = students[i];
            }
        }

        return best;
    }

    static double GroupAverage(Student[] students)
    {
        double sum = 0;

        foreach (Student student in students)
        {
            sum += student.Average();
        }

        return sum / students.Length;
    }
    static void PrintNeedsHelp(Student[] students)
    {
        Console.WriteLine();
        Console.Write("Needs help: ");

        bool found = false;

        foreach (Student student in students)
        {
            if (student.Average() <= 3.5)
            {
                Console.WriteLine($"{student.Name} ({student.Average():0.00})");
                found = true;
            }
        }
        if (!found)
        {
            Console.WriteLine("Nobody needs help.");
        }
    }
        static void SortByAverage(Student[] students)
        {
            for (int i = 0; i < students.Length - 1; i++)
            {
                for (int j = 0; j < students.Length - 1 - i; j++)
                {
                    if (students[j].Average() < students[j + 1].Average())
                    {
                        Student temp = students[j];
                        students[j] = students[j + 1];
                        students[j + 1] = temp;
                    }
                }
            }
        }
    }

class Student
{
    public string Name { get; }
    public int[] Grades { get; }

    public Student(string name, int[] grades)
    {
        Name = name;
        Grades = grades;
    }

    public double Average()
    {
        int sum = 0;

        foreach (int grade in Grades)
        {
            sum += grade;
            
        }
        return (double)sum / Grades.Length;
    }
}