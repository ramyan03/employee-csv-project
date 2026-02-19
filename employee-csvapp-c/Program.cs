using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
// System for core utilities like console, string, exception
// Generic for Lists, Dictionary
// Globalization for culture invariant parsing for CSV -> decimal.Parse(1234.56, CultureInfo.InvariantCulture) always works regardless of country
// IO for file operations
// LINQ queries
/*
They import namespaces so I can use their classes without fully qualifying them.
System gives core utilities,
Collections.Generic provides typed collections like List, like List<Employee> employees = new List<Employee>();
Globalization handles culture formatting, TryParse
IO supports file handling,
and Linq enables functional querying over collections.
*/

/*
========================================
INTERVIEW NOTES — Employee CSV Processor
========================================

What this program does:
- Reads an employee CSV file (path from args or prompt)
- Validates format (required headers) and parses rows into Employee objects
- Performs analysis:
  - total salary per department (GroupBy + Sum)
  - highest salary employee
  - average salary
- Allows appending a new employee back into the CSV

Why this design:
- Strongly typed model (Employee) => safer than working with raw strings
- Error handling strategy:
  - "Fail fast" for fatal errors (missing file, empty file, missing columns)
  - "Continue with warnings" for bad rows (skip malformed rows, report line number)
- LINQ used for analytics for clarity/maintainability

Follow-up answers (backend style):
- async/await: not necessary here (CLI + local file), but for APIs/DB calls I'd use async I/O
- DI: not used in this single-file console app; in ASP.NET I'd inject services/repositories via constructors
- concurrency: if two users update the same record in DB, use optimistic concurrency (rowversion) or transactions
- auth: if this were an API, I'd add JWT auth + [Authorize] on endpoints

If production:
- Add logging (structured), unit tests, better CSV parsing (quoted commas), and persistent storage (DB).
*/


public class Program
{
    private static readonly string[] RequiredColumns =
        { "EmployeeId", "FirstName", "LastName", "Department", "Salary" };
    
    // Prevents accidental modification

    // The program's entry point. 
    // Displays a menu loop where users can analyze employee data, list employees, add new ones, or reload the CSV file.
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Employee CSV Processor ===");

        string csvPath =  args.Length > 0 ? args[0] : Prompt("Enter path to employees.csv", "employees.csv");

        // Load once at startup.
        // Supports both command-line arg and interactive prompt
        // Fatal validation errors stop the program; non-fatal row issues become warnings.
        // Validate required columns up front so later parsing can rely on headerIndex.
        // This is "fail fast" on incorrect CSV format.

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
    // Reads the CSV file, validates headers, parses each row into Employee objects, and collects any errors. 
    // Returns true if successful, false for fatal errors.
    public static bool TryLoadEmployees(string? path, out List<Employee> employees, out List<string> errors)
    {
        employees = new List<Employee>();
        errors = new List<string>();
        // Must be assigned
        try
        {
            if (string.IsNullOrWhiteSpace(path)) // Checks if string is empty or null
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
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // Opens existing file at specified path for reading only by current process while allowing other processes to open file for read/write
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
                // Validate all columns so we can fail fast. No point parsing rows if headers are wrong. Can also determine which headers failed.
                if (errors.Count > 0) return false; // Fatal if any column missing

                int lineNumber = 1; // Tracks current line for error messages
                string? line; // Can be null
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue; // Skip blank lines

                    var fields = SplitCsvSimple(line);

                    // declares an output variable inline and passes it to the method so the method can assign a value to it. 
                    // This is commonly used in C# Try-pattern methods to return multiple values while also indicating success or failure.
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

        // Specific exceptions first (UnauthorizedAccessException, IOException)
        // Generic exception last (catch all)
        // Each provides context-specific message
    }

    // Analytics are expressed as LINQ queries for readability and correctness.
    // Performs data analysis using LINQ: 
    // Calculates total salary per department, finds the highest-paid employee, and computes average salary.
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

            // Groups employees by department (ignores case). Returns key=department, value=employee
            // Order by department name alphabetically
            // Projects each group to anonymous object. Department is key. Total is sum of all salaries in that group

            /*
            SQL Equivalent:
            SELECT Department, SUM(Salary) as Total
            FROM employees
            GROUP BY Department
            ORDER BY Department
            */

        foreach (var item in totalsByDept)
        {
            Console.WriteLine($"- {item.Department}: {item.Total.ToString("C", CultureInfo.CurrentCulture)}"); // Culture is regional settings. CurrentCulture determines how data is formatted and interpreted
        }

        // Highest salary employee
        var highest = employees
            .OrderByDescending(e => e.Salary)
            .ThenBy(e => e.LastName)
            .ThenBy(e => e.FirstName) // In case of ties
            .First();

        Console.WriteLine("\nHighest salary employee:");
        Console.WriteLine($"{highest.EmployeeId} - {highest.FirstName} {highest.LastName} ({highest.Department}) => {highest.Salary.ToString("C", CultureInfo.CurrentCulture)}");

        var avg = employees.Average(e => e.Salary);
        Console.WriteLine($"\nAverage salary: {avg.ToString("C", CultureInfo.CurrentCulture)}");
        Console.WriteLine($"Employee count: {employees.Count}");
    }

    // Displays the first N employees from the list with their details formatted.
    private static void ListEmployees(List<Employee> employees, int take)
    {
        Console.WriteLine("\n=== Employees ===");
        foreach (var e in employees.Take(take))
        {
            Console.WriteLine($"{e.EmployeeId}: {e.FirstName} {e.LastName} | {e.Department} | {e.Salary.ToString("C", CultureInfo.CurrentCulture)}");
        }
        if (employees.Count > take) Console.WriteLine($"... and {employees.Count - take} more");
    }

    // Interactive workflow to add a new employee. 
    // Prompts for all fields, validates the ID is unique, then appends to the CSV file and in-memory list.
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
    // Creates a case-insensitive dictionary mapping column names to their positions in the CSV header row.
    public static Dictionary<string, int> BuildHeaderIndex(string[] headers)
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
    // Trims white space
    // Skips duplicates (ContainsKey(key)) so we only keep first one
    // Skip empty columns (keyLength > 0)

    // Converts a CSV row into an Employee object. 
    // Validates that ID is positive, names/department are non-empty, and salary is non-negative.
    public static bool TryParseEmployee(
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
            return idx < fields.Length ? fields[idx].Trim() : ""; // Looks up column index from header. Returns trimmed value or empty string if row is too short. Ensures not out of bounds
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
        // NumberStyles.Integer allows 123, -123, +123, etc. Rejects decimal and thousands separator
        // id<=0 check since ID should be positive integers

        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(dept))
        {
            error = "FirstName/LastName/Department must be non-empty";
            return false;
        }
        // Rejects white space "  "

        if (!decimal.TryParse(salStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal salary) || salary < 0)
        {
            error = $"Invalid Salary '{salStr}'";
            return false;
        }

        // NumberStyles.Number allows for 75000, 75000.50, -100, etc. InvariantCulture always uses . as decimal, not ,
        // Salary < 0 since salary may be 0, but never negative

        employee = new Employee(id, first, last, dept, salary);
        return true;
    }

    // NOTE: Simple CSV splitting works for this challenge input.
    // In production, use a real CSV parser to support quoted fields/embedded commas.

    // Splits a CSV line by commas (basic implementation, doesn't handle quoted fields).
    // Doesn't handle embeddeed commas -> Engineering, Software
    // CSVHelper Library is alternative
    public static string[] SplitCsvSimple(string line)
        => line.Split(',');

    // Removes commas from a string to prevent breaking CSV format when writing.
    // Better approach would be to quote the field -> "Engineering Software"
    public static string EscapeCsvSimple(string value)
        => value.Replace(",", " ").Trim();

    // ===== Console Input Helpers =====

    // Displays a prompt and reads user input, returning the default if input is empty.
    private static string Prompt(string label, string defaultValue = "")
    {
        Console.Write($"{label}{(string.IsNullOrWhiteSpace(defaultValue) ? "" : $" [{defaultValue}]")}: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    //  Repeatedly prompts until user provides a non-empty string. For department/name/etc.
    private static string PromptNonEmpty(string label)
    {
        while (true)
        {
            var s = Prompt(label).Trim();
            if (!string.IsNullOrWhiteSpace(s)) return s;
            Console.WriteLine($"{label} cannot be empty.");
        }
    }

    // Repeatedly prompts until user provides a valid integer >= the minimum value. For ID
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

    // Repeatedly prompts until user provides a valid decimal number >= the minimum value. For salary
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

// Record instead of class because:
/*
** Value based equality (two employees with same id, name, etc. will be equal)
** Immutability (doesn't override older entities)
** Concise syntax
** Thread safety -> Immutable objects are thread safe. No need for locks when sharing between threads
**
** Tradeoffs are that records are reference types (heap storage), slightly slower than structs for smaller data
*/

/*

Error handling:
    Distinguish between fatal errors and recoverable warnings. 
        Fatal (fail fast): Missing file, empty file, missing columng -> Stop execution
        Recoverable: Warn and continue -> Skip it, log line number, continue processing
    Gives actionable feedback

Try pattern:
    Idiomatic C# for opertions that can fail without throwing exceptions. Returns bool for success/failure and uses out parameters for result and error message
    Avoids exception overhead for expected failures (malformed CSV rows)
    Makes error handling explicit in control flow

Decimal over float/double to ensure single decimal places. Slower but correctness and prevents 0.400000004 for example

LINQ over For loop
    More declarative (what we want)
    More readable for complex queries and less error prone
    Easier to modify (if we want to add another layer of filtering/sorting without refactoring loop logic)

Building header index
    Reason is to make parsing column-order-independent. If CSV has columns in any order, we can still find right value by name
    Also has case-insensitive matching, handles extra columns gracefully and validates required columns exist upfront

Use CSV library
    Use for quoted fileds -> "Last, First"
    Escaped quotes
    Multi line values
    Different delimiters/encodings

Why append with File.AppendAllText?
    Appending is more efficient for large files than read/write entire file
    Won't work as well if file is sorted and needs to stay sorted, or if we need to validate uniqueness across all rows before writing

*/
