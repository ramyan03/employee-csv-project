using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public class Program
{
    private static readonly string[] RequiredColumns =
        { "EmployeeId", "FirstName", "LastName", "Department", "Salary" };

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Employee CSV Processor ===");

        string csvPath =  args.Length > 0 ? args[0] : Prompt("Enter path to employees.csv", "employees.csv");

        // Load once at startup
        if (!TryLoadEmployees(csvPath, out var employees, out var loadErrors))
        {
            Console.WriteLine("Failed to load employees:");
            foreach (var err in loadErrors) Console.WriteLine($"- {err}");
            return;
        }

        // Print non-fatal parsing warnings
        if (loadErrors.Count > 0)
        {
            Console.WriteLine("\nWarnings during load:");
            foreach (var err in loadErrors) Console.WriteLine($"- {err}");
        }

        while (true)
        {
            Console.WriteLine("\nMenu:");
            Console.WriteLine("1) Show analysis");
            Console.WriteLine("2) List employees (first 20)");
            Console.WriteLine("3) Add new employee");
            Console.WriteLine("4) Reload from CSV");
            Console.WriteLine("0) Exit");

            var choice = Prompt("Choose an option", "1").Trim();

            if (choice == "0") break;

            switch (choice)
            {
                case "1":
                    ShowAnalysis(employees);
                    break;

                case "2":
                    ListEmployees(employees, take: 20);
                    break;

                case "3":
                    AddEmployeeFlow(csvPath, employees);
                    break;

                case "4":
                    if (!TryLoadEmployees(csvPath, out employees, out loadErrors))
                    {
                        Console.WriteLine("Reload failed:");
                        foreach (var err in loadErrors) Console.WriteLine($"- {err}");
                    }
                    else
                    {
                        Console.WriteLine("Reloaded successfully.");
                        if (loadErrors.Count > 0)
                        {
                            Console.WriteLine("Warnings during reload:");
                            foreach (var err in loadErrors) Console.WriteLine($"- {err}");
                        }
                    }
                    break;

                default:
                    Console.WriteLine("Unknown option. Try again.");
                    break;
            }
        }

        Console.WriteLine("Goodbye!");
    }

    // ===== Core Features =====

    private static bool TryLoadEmployees(string path, out List<Employee> employees, out List<string> errors)
    {
        employees = new List<Employee>();
        errors = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add("CSV path is empty.");
                return false;
            }

            if (!File.Exists(path))
            {
                errors.Add($"File not found: {path}");
                return false;
            }

            // Check accessibility
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string? headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    errors.Add("CSV file is empty.");
                    return false;
                }

                var headers = SplitCsvSimple(headerLine);
                var headerIndex = BuildHeaderIndex(headers);

                // Validate required columns exist
                foreach (var col in RequiredColumns)
                {
                    if (!headerIndex.ContainsKey(col))
                    {
                        errors.Add($"Missing required column: '{col}'. Found columns: {string.Join(", ", headers)}");
                    }
                }
                if (errors.Count > 0) return false;

                int lineNumber = 1; 
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = SplitCsvSimple(line);

                    if (!TryParseEmployee(fields, headerIndex, out var emp, out var rowError))
                    {
                        errors.Add($"Line {lineNumber}: {rowError} | Raw: {line}");
                        continue; 
                    }

                    employees.Add(emp);
                }
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errors.Add($"Access denied when reading: {path}");
            return false;
        }
        catch (IOException ex)
        {
            errors.Add($"I/O error reading file: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    private static void ShowAnalysis(List<Employee> employees)
    {
        Console.WriteLine("\n=== Data Analysis ===");

        if (employees.Count == 0)
        {
            Console.WriteLine("No employees loaded.");
            return;
        }

        Console.WriteLine("\nTotal salary per department:");
        var totalsByDept = employees
            .GroupBy(e => e.Department, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Department = g.Key,
                Total = g.Sum(x => x.Salary)
            });

        foreach (var item in totalsByDept)
        {
            Console.WriteLine($"- {item.Department}: {item.Total.ToString("C", CultureInfo.CurrentCulture)}");
        }

        // Highest salary employee
        var highest = employees
            .OrderByDescending(e => e.Salary)
            .ThenBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .First();

        Console.WriteLine("\nHighest salary employee:");
        Console.WriteLine($"{highest.EmployeeId} - {highest.FirstName} {highest.LastName} ({highest.Department}) => {highest.Salary.ToString("C", CultureInfo.CurrentCulture)}");

        var avg = employees.Average(e => e.Salary);
        Console.WriteLine($"\nAverage salary: {avg.ToString("C", CultureInfo.CurrentCulture)}");
        Console.WriteLine($"Employee count: {employees.Count}");
    }

    private static void ListEmployees(List<Employee> employees, int take)
    {
        Console.WriteLine("\n=== Employees ===");
        foreach (var e in employees.Take(take))
        {
            Console.WriteLine($"{e.EmployeeId}: {e.FirstName} {e.LastName} | {e.Department} | {e.Salary.ToString("C", CultureInfo.CurrentCulture)}");
        }
        if (employees.Count > take) Console.WriteLine($"... and {employees.Count - take} more");
    }

    private static void AddEmployeeFlow(string csvPath, List<Employee> employees)
    {
        Console.WriteLine("\n=== Add New Employee ===");

        int id = PromptInt("EmployeeId", min: 1);

        if (employees.Any(e => e.EmployeeId == id))
        {
            Console.WriteLine($"EmployeeId {id} already exists. Aborting add.");
            return;
        }

        string first = PromptNonEmpty("FirstName");
        string last = PromptNonEmpty("LastName");
        string dept = PromptNonEmpty("Department");
        decimal salary = PromptDecimal("Salary", min: 0);

        var newEmp = new Employee(id, first, last, dept, salary);

        try
        {
            // Ensure file has header; if file exists but empty, write header first
            if (new FileInfo(csvPath).Length == 0)
            {
                File.WriteAllText(csvPath, string.Join(",", RequiredColumns) + Environment.NewLine);
            }

            string line = $"{newEmp.EmployeeId},{EscapeCsvSimple(newEmp.FirstName)},{EscapeCsvSimple(newEmp.LastName)},{EscapeCsvSimple(newEmp.Department)},{newEmp.Salary.ToString(CultureInfo.InvariantCulture)}";
            File.AppendAllText(csvPath, line + Environment.NewLine);

            employees.Add(newEmp);
            Console.WriteLine("Employee added successfully.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Access denied when writing: {csvPath}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"I/O error writing file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    // ===== Parsing Helpers =====

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var key = headers[i].Trim();
            if (!dict.ContainsKey(key) && key.Length > 0)
                dict[key] = i;
        }
        return dict;
    }

    private static bool TryParseEmployee(
        string[] fields,
        Dictionary<string, int> headerIndex,
        out Employee employee,
        out string error)
    {
        employee = default!;
        error = "";

        string Get(string col)
        {
            int idx = headerIndex[col];
            return idx < fields.Length ? fields[idx].Trim() : "";
        }

        var idStr = Get("EmployeeId");
        var first = Get("FirstName");
        var last = Get("LastName");
        var dept = Get("Department");
        var salStr = Get("Salary");

        if (!int.TryParse(idStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
        {
            error = $"Invalid EmployeeId '{idStr}'";
            return false;
        }

        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(dept))
        {
            error = "FirstName/LastName/Department must be non-empty";
            return false;
        }

        if (!decimal.TryParse(salStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal salary) || salary < 0)
        {
            error = $"Invalid Salary '{salStr}'";
            return false;
        }

        employee = new Employee(id, first, last, dept, salary);
        return true;
    }

    private static string[] SplitCsvSimple(string line)
        => line.Split(',');

    private static string EscapeCsvSimple(string value)
        => value.Replace(",", " ").Trim();

    // ===== Console Input Helpers =====

    private static string Prompt(string label, string defaultValue = "")
    {
        Console.Write($"{label}{(string.IsNullOrWhiteSpace(defaultValue) ? "" : $" [{defaultValue}]")}: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    private static string PromptNonEmpty(string label)
    {
        while (true)
        {
            var s = Prompt(label).Trim();
            if (!string.IsNullOrWhiteSpace(s)) return s;
            Console.WriteLine($"{label} cannot be empty.");
        }
    }

    private static int PromptInt(string label, int min)
    {
        while (true)
        {
            var s = Prompt(label).Trim();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= min)
                return value;

            Console.WriteLine($"{label} must be an integer >= {min}.");
        }
    }

    private static decimal PromptDecimal(string label, decimal min)
    {
        while (true)
        {
            var s = Prompt(label).Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= min)
                return value;

            Console.WriteLine($"{label} must be a number >= {min}.");
        }
    }
}

public record Employee(
    int EmployeeId,
    string FirstName,
    string LastName,
    string Department,
    decimal Salary
);
