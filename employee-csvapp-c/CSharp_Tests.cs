// ============================================
// C# EMPLOYEE CSV PROCESSOR - TEST CASES
// ============================================
// Testing Framework: xUnit + FluentAssertions
// File: EmployeeProcessorTests.cs
// ============================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;

namespace EmployeeProcessor.Tests
{
    // ============================================
    // PARSING FUNCTION TESTS
    // ============================================
    
    public class EmployeeParsingTests
    {
        private readonly Dictionary<string, int> _standardHeaderIndex = new()
        {
            ["EmployeeId"] = 0,
            ["FirstName"] = 1,
            ["LastName"] = 2,
            ["Department"] = 3,
            ["Salary"] = 4
        };

        [Fact]
        public void TryParseEmployee_ValidRow_ReturnsTrue()
        {
            // Arrange
            var fields = new[] { "123", "John", "Doe", "Engineering", "75000.50" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out var employee, 
                out var error
            );
            
            // Assert
            success.Should().BeTrue();
            employee.EmployeeId.Should().Be(123);
            employee.FirstName.Should().Be("John");
            employee.LastName.Should().Be("Doe");
            employee.Department.Should().Be("Engineering");
            employee.Salary.Should().Be(75000.50m);
            error.Should().BeEmpty();
        }

        [Theory]
        [InlineData("0", "Invalid EmployeeId")]       // ID is 0
        [InlineData("-5", "Invalid EmployeeId")]      // Negative ID
        [InlineData("abc", "Invalid EmployeeId")]     // Non-numeric
        [InlineData("", "Invalid EmployeeId")]        // Empty
        [InlineData("2147483648", "Invalid EmployeeId")] // Overflow int.MaxValue
        public void TryParseEmployee_InvalidEmployeeId_ReturnsFalse(string id, string expectedErrorPattern)
        {
            // Arrange
            var fields = new[] { id, "John", "Doe", "Engineering", "50000" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out _, 
                out var error
            );
            
            // Assert
            success.Should().BeFalse();
            error.Should().Contain(expectedErrorPattern);
        }

        [Theory]
        [InlineData("", "Doe", "Engineering", "must be non-empty")]     // Empty first name
        [InlineData("John", "", "Engineering", "must be non-empty")]    // Empty last name
        [InlineData("John", "Doe", "", "must be non-empty")]           // Empty department
        [InlineData("   ", "Doe", "Eng", "must be non-empty")]         // Whitespace first name
        [InlineData("John", "   ", "Eng", "must be non-empty")]        // Whitespace last name
        [InlineData("John", "Doe", "   ", "must be non-empty")]        // Whitespace department
        public void TryParseEmployee_EmptyRequiredFields_ReturnsFalse(
            string first, string last, string dept, string expectedErrorPattern)
        {
            // Arrange
            var fields = new[] { "123", first, last, dept, "50000" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out _, 
                out var error
            );
            
            // Assert
            success.Should().BeFalse();
            error.Should().Contain(expectedErrorPattern);
        }


        [Fact]
        public void TryParseEmployee_ZeroSalary_Succeeds()
        {
            // Arrange - Zero salary should be valid (unpaid intern, volunteer)
            var fields = new[] { "123", "John", "Doe", "Engineering", "0" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out var employee, 
                out _
            );
            
            // Assert
            success.Should().BeTrue();
            employee.Salary.Should().Be(0m);
        }

        [Fact]
        public void TryParseEmployee_LargeSalary_Succeeds()
        {
            // Arrange - Large but valid salary
            var fields = new[] { "123", "John", "Doe", "CEO", "999999.99" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out var employee, 
                out _
            );
            
            // Assert
            success.Should().BeTrue();
            employee.Salary.Should().Be(999999.99m);
        }

        [Fact]
        public void TryParseEmployee_MissingFields_HandlesGracefully()
        {
            // Arrange - Row only has 3 fields instead of 5
            var fields = new[] { "123", "John", "Doe" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out _, 
                out var error
            );
            
            // Assert
            success.Should().BeFalse();
            // Should fail on empty department or salary
            error.Should().NotBeEmpty();
        }

        [Fact]
        public void TryParseEmployee_ExtraFields_IgnoresExtra()
        {
            // Arrange - Row has extra fields
            var fields = new[] { "123", "John", "Doe", "Engineering", "50000", "Extra1", "Extra2" };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out var employee, 
                out _
            );
            
            // Assert
            success.Should().BeTrue();
            employee.EmployeeId.Should().Be(123);
        }

        [Fact]
        public void TryParseEmployee_TrimsWhitespace()
        {
            // Arrange - Fields have leading/trailing whitespace
            var fields = new[] { " 123 ", " John ", " Doe ", " Engineering ", " 50000 " };
            
            // Act
            bool success = Program.TryParseEmployee(
                fields, 
                _standardHeaderIndex, 
                out var employee, 
                out _
            );
            
            // Assert
            success.Should().BeTrue();
            employee.FirstName.Should().Be("John");
            employee.LastName.Should().Be("Doe");
            employee.Department.Should().Be("Engineering");
        }

        [Fact]
        public void TryParseEmployee_UsesCultureInvariantParsing()
        {
            // Arrange - Decimal with period (should work in any culture)
            var fields = new[] { "123", "John", "Doe", "Engineering", "12345.67" };
            
            // Act
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // Test with German culture where comma is decimal separator
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                
                bool success = Program.TryParseEmployee(
                    fields, 
                    _standardHeaderIndex, 
                    out var employee, 
                    out _
                );
                
                // Assert
                success.Should().BeTrue();
                employee.Salary.Should().Be(12345.67m);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }

    // ============================================
    // HEADER INDEX TESTS
    // ============================================
    
    public class HeaderIndexTests
    {
        [Fact]
        public void BuildHeaderIndex_StandardHeaders_CreatesCorrectMapping()
        {
            // Arrange
            var headers = new[] { "EmployeeId", "FirstName", "LastName", "Department", "Salary" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().ContainKey("EmployeeId").WhoseValue.Should().Be(0);
            index.Should().ContainKey("FirstName").WhoseValue.Should().Be(1);
            index.Should().ContainKey("LastName").WhoseValue.Should().Be(2);
            index.Should().ContainKey("Department").WhoseValue.Should().Be(3);
            index.Should().ContainKey("Salary").WhoseValue.Should().Be(4);
        }

        [Fact]
        public void BuildHeaderIndex_CaseInsensitive_MapsCorrectly()
        {
            // Arrange
            var headers = new[] { "EMPLOYEEID", "firstname", "LastName", "DEPARTMENT", "salary" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().ContainKey("employeeid");
            index.Should().ContainKey("firstname");
            index.Should().ContainKey("lastname");
            index.Should().ContainKey("department");
            index.Should().ContainKey("salary");
            
            // All keys should work regardless of case
            index.ContainsKey("EmployeeId").Should().BeTrue();
            index.ContainsKey("FIRSTNAME").Should().BeTrue();
        }

        [Fact]
        public void BuildHeaderIndex_ExtraWhitespace_TrimsHeaders()
        {
            // Arrange
            var headers = new[] { " EmployeeId ", "FirstName  ", "  LastName", " Department ", "Salary" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().ContainKey("EmployeeId");
            index.Should().ContainKey("FirstName");
            index.Should().ContainKey("LastName");
        }

        [Fact]
        public void BuildHeaderIndex_DuplicateColumns_KeepsFirst()
        {
            // Arrange
            var headers = new[] { "EmployeeId", "FirstName", "FirstName", "LastName" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().ContainKey("FirstName").WhoseValue.Should().Be(1); // First occurrence
            index.Count.Should().Be(3); // EmployeeId, FirstName, LastName (no duplicate)
        }

        [Fact]
        public void BuildHeaderIndex_EmptyColumns_SkipsThem()
        {
            // Arrange
            var headers = new[] { "EmployeeId", "", "FirstName", "   ", "LastName" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().ContainKey("EmployeeId");
            index.Should().ContainKey("FirstName");
            index.Should().ContainKey("LastName");
            index.Count.Should().Be(3); // Empty columns not included
        }

        [Fact]
        public void BuildHeaderIndex_ExtraColumns_IncludesThem()
        {
            // Arrange
            var headers = new[] { "EmployeeId", "FirstName", "LastName", "Department", "Salary", "HireDate", "Manager" };
            
            // Act
            var index = Program.BuildHeaderIndex(headers);
            
            // Assert
            index.Should().HaveCount(7);
            index.Should().ContainKey("HireDate").WhoseValue.Should().Be(5);
            index.Should().ContainKey("Manager").WhoseValue.Should().Be(6);
        }
    }

    // ============================================
    // FILE LOADING TESTS
    // ============================================
    
    public class FileLoadingTests
    {
        private readonly string _testFilesDirectory;

        public FileLoadingTests()
        {
            _testFilesDirectory = Path.Combine(Path.GetTempPath(), "EmployeeTests");
            Directory.CreateDirectory(_testFilesDirectory);
        }

        [Fact]
        public void TryLoadEmployees_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "valid.csv");
            var csvContent = @"EmployeeId,FirstName,LastName,Department,Salary
123,John,Doe,Engineering,75000
456,Jane,Smith,Marketing,65000
789,Bob,Jones,Sales,55000";
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeTrue();
            employees.Should().HaveCount(3);
            errors.Should().BeEmpty();
            
            employees[0].EmployeeId.Should().Be(123);
            employees[1].EmployeeId.Should().Be(456);
            employees[2].EmployeeId.Should().Be(789);
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_MissingFile_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "nonexistent.csv");
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeFalse();
            employees.Should().BeEmpty();
            errors.Should().Contain(e => e.Contains("not found"));
        }

        [Fact]
        public void TryLoadEmployees_EmptyFile_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "empty.csv");
            File.WriteAllText(filePath, string.Empty);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("empty"));
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_MissingRequiredColumn_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "missing_column.csv");
            var csvContent = @"EmployeeId,FirstName,LastName,Department
123,John,Doe,Engineering"; // Missing Salary column
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("Missing required column") && e.Contains("Salary"));
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_MalformedRow_SkipsRowWithWarning()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "malformed.csv");
            var csvContent = @"EmployeeId,FirstName,LastName,Department,Salary
123,John,Doe,Engineering,75000
456,Jane,Smith,Marketing,invalid_salary
789,Bob,Jones,Sales,55000";
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeTrue(); // Loading succeeds despite bad row
            employees.Should().HaveCount(2); // Only valid rows loaded
            errors.Should().Contain(e => e.Contains("Line 3") && e.Contains("Invalid Salary"));
            
            employees[0].EmployeeId.Should().Be(123);
            employees[1].EmployeeId.Should().Be(789);
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_BlankLines_SkipsBlankLines()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "blank_lines.csv");
            var csvContent = @"EmployeeId,FirstName,LastName,Department,Salary
123,John,Doe,Engineering,75000

456,Jane,Smith,Marketing,65000
   
789,Bob,Jones,Sales,55000";
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeTrue();
            employees.Should().HaveCount(3);
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_ExtraColumns_LoadsSuccessfully()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "extra_columns.csv");
            var csvContent = @"EmployeeId,FirstName,LastName,Department,Salary,HireDate,Manager
123,John,Doe,Engineering,75000,2020-01-15,Alice
456,Jane,Smith,Marketing,65000,2019-06-01,Bob";
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeTrue();
            employees.Should().HaveCount(2);
            errors.Should().BeEmpty();
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_CaseInsensitiveHeaders_LoadsSuccessfully()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "case_insensitive.csv");
            var csvContent = @"EMPLOYEEID,firstname,LastName,DEPARTMENT,salary
123,John,Doe,Engineering,75000";
            File.WriteAllText(filePath, csvContent);
            
            // Act
            bool success = Program.TryLoadEmployees(filePath, out var employees, out var errors);
            
            // Assert
            success.Should().BeTrue();
            employees.Should().HaveCount(1);
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void TryLoadEmployees_EmptyStringPath_ReturnsFalse()
        {
            // Act
            bool success = Program.TryLoadEmployees("", out var employees, out var errors);
            
            // Assert
            success.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("empty"));
        }

        [Fact]
        public void TryLoadEmployees_NullPath_ReturnsFalse()
        {
            // Act
            bool success = Program.TryLoadEmployees(null, out var employees, out var errors);
            
            // Assert
            success.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("empty"));
        }
    }

    // ============================================
    // LINQ ANALYTICS TESTS
    // ============================================
    
    public class AnalyticsTests
    {
        private readonly List<Employee> _testEmployees = new()
        {
            new Employee(1, "John", "Doe", "Engineering", 75000),
            new Employee(2, "Jane", "Smith", "Engineering", 80000),
            new Employee(3, "Bob", "Jones", "Sales", 55000),
            new Employee(4, "Alice", "Brown", "Sales", 60000),
            new Employee(5, "Charlie", "Davis", "Marketing", 65000),
            new Employee(6, "Diana", "Wilson", "Marketing", 70000)
        };

        [Fact]
        public void GroupByDepartment_CalculatesCorrectTotals()
        {
            // Act
            var totalsByDept = _testEmployees
                .GroupBy(e => e.Department, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Department = g.Key,
                    Total = g.Sum(x => x.Salary)
                })
                .ToList();
            
            // Assert
            totalsByDept.Should().HaveCount(3);
            
            totalsByDept[0].Department.Should().Be("Engineering");
            totalsByDept[0].Total.Should().Be(155000); // 75000 + 80000
            
            totalsByDept[1].Department.Should().Be("Marketing");
            totalsByDept[1].Total.Should().Be(135000); // 65000 + 70000
            
            totalsByDept[2].Department.Should().Be("Sales");
            totalsByDept[2].Total.Should().Be(115000); // 55000 + 60000
        }

        [Fact]
        public void GroupByDepartment_CaseInsensitive_GroupsCorrectly()
        {
            // Arrange
            var mixedCaseEmployees = new List<Employee>
            {
                new Employee(1, "John", "Doe", "engineering", 75000),
                new Employee(2, "Jane", "Smith", "Engineering", 80000),
                new Employee(3, "Bob", "Jones", "ENGINEERING", 85000)
            };
            
            // Act
            var totalsByDept = mixedCaseEmployees
                .GroupBy(e => e.Department, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Department = g.Key,
                    Total = g.Sum(x => x.Salary),
                    Count = g.Count()
                })
                .ToList();
            
            // Assert
            totalsByDept.Should().HaveCount(1); // All grouped together
            totalsByDept[0].Count.Should().Be(3);
            totalsByDept[0].Total.Should().Be(240000);
        }

        [Fact]
        public void FindHighestSalary_WithTieBreaker_ReturnsDeterministic()
        {
            // Arrange - Two employees with same salary
            var employees = new List<Employee>
            {
                new Employee(1, "John", "Doe", "Engineering", 100000),
                new Employee(2, "Jane", "Smith", "Sales", 100000),
                new Employee(3, "Bob", "Anderson", "Marketing", 100000) // Should win (alphabetically first)
            };
            
            // Act
            var highest = employees
                .OrderByDescending(e => e.Salary)
                .ThenBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .First();
            
            // Assert
            highest.LastName.Should().Be("Anderson"); // "Anderson" < "Doe" < "Smith"
        }

        [Fact]
        public void CalculateAverage_ReturnsCorrectValue()
        {
            // Act
            var average = _testEmployees.Average(e => e.Salary);
            
            // Assert
            // (75000 + 80000 + 55000 + 60000 + 65000 + 70000) / 6 = 67500
            average.Should().Be(67500);
        }

        [Fact]
        public void FilterByDepartment_ReturnsMatchingEmployees()
        {
            // Act
            var engineers = _testEmployees
                .Where(e => e.Department.Equals("Engineering", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            // Assert
            engineers.Should().HaveCount(2);
            engineers.Should().OnlyContain(e => e.Department == "Engineering");
        }

        [Fact]
        public void CountEmployees_ReturnsCorrectCount()
        {
            // Act
            var count = _testEmployees.Count;
            
            // Assert
            count.Should().Be(6);
        }

        [Fact]
        public void EmptyList_AverageThrows_ShouldHandleGracefully()
        {
            // Arrange
            var emptyList = new List<Employee>();
            
            // Act & Assert
            Action act = () => emptyList.Average(e => e.Salary);
            act.Should().Throw<InvalidOperationException>();
            
            // Better approach - check for empty first
            var average = emptyList.Any() ? emptyList.Average(e => e.Salary) : 0;
            average.Should().Be(0);
        }
    }

    // ============================================
    // EMPLOYEE RECORD TESTS
    // ============================================
    
    public class EmployeeRecordTests
    {
        [Fact]
        public void Employee_ValueEquality_WorksCorrectly()
        {
            // Arrange
            var emp1 = new Employee(1, "John", "Doe", "Engineering", 75000);
            var emp2 = new Employee(1, "John", "Doe", "Engineering", 75000);
            var emp3 = new Employee(2, "Jane", "Smith", "Sales", 60000);
            
            // Assert
            emp1.Should().Be(emp2); // Value equality
            emp1.Should().NotBe(emp3);
            (emp1 == emp2).Should().BeTrue();
            (emp1 == emp3).Should().BeFalse();
        }

        [Fact]
        public void Employee_Immutability_PropertiesCannotBeChanged()
        {
            // Arrange
            var employee = new Employee(1, "John", "Doe", "Engineering", 75000);
            
            // Assert - This won't compile (properties are init-only)
            // employee.FirstName = "Jane"; // Compiler error
            
            // Can only modify using 'with' expression (creates new instance)
            var modified = employee with { FirstName = "Jane" };
            modified.FirstName.Should().Be("Jane");
            employee.FirstName.Should().Be("John"); // Original unchanged
        }

        [Fact]
        public void Employee_WithExpression_CreatesNewInstance()
        {
            // Arrange
            var original = new Employee(1, "John", "Doe", "Engineering", 75000);
            
            // Act
            var modified = original with { Salary = 80000 };
            
            // Assert
            modified.Salary.Should().Be(80000);
            original.Salary.Should().Be(75000); // Original unchanged
            ReferenceEquals(original, modified).Should().BeFalse(); // Different instances
        }

        [Fact]
        public void Employee_ToString_GeneratesReadableOutput()
        {
            // Arrange
            var employee = new Employee(1, "John", "Doe", "Engineering", 75000);
            
            // Act
            var stringRep = employee.ToString();
            
            // Assert
            stringRep.Should().Contain("John");
            stringRep.Should().Contain("Doe");
            stringRep.Should().Contain("75000");
        }

        [Fact]
        public void Employee_GetHashCode_ConsistentForEqualObjects()
        {
            // Arrange
            var emp1 = new Employee(1, "John", "Doe", "Engineering", 75000);
            var emp2 = new Employee(1, "John", "Doe", "Engineering", 75000);
            
            // Assert
            emp1.GetHashCode().Should().Be(emp2.GetHashCode());
        }

        [Fact]
        public void Employee_CanBeUsedInDictionary()
        {
            // Arrange
            var dict = new Dictionary<Employee, string>();
            var emp1 = new Employee(1, "John", "Doe", "Engineering", 75000);
            var emp2 = new Employee(1, "John", "Doe", "Engineering", 75000);
            
            // Act
            dict[emp1] = "First entry";
            
            // Assert - emp2 is "equal" to emp1, so it should retrieve the same value
            dict[emp2].Should().Be("First entry");
        }
    }

    // ============================================
    // CSV HELPER TESTS
    // ============================================
    
    public class CsvHelperTests
    {
        [Fact]
        public void SplitCsvSimple_SplitsOnComma()
        {
            // Arrange
            var line = "123,John,Doe,Engineering,75000";
            
            // Act
            var fields = Program.SplitCsvSimple(line);
            
            // Assert
            fields.Should().HaveCount(5);
            fields[0].Should().Be("123");
            fields[1].Should().Be("John");
            fields[4].Should().Be("75000");
        }

        [Fact]
        public void SplitCsvSimple_DoesNotHandleQuotedCommas()
        {
            // Arrange
            var line = "123,\"Last, First\",Engineering,75000";
            
            // Act
            var fields = Program.SplitCsvSimple(line);
            
            // Assert - This is a KNOWN LIMITATION
            fields.Should().HaveCount(5); // Splits on comma inside quotes
            fields[1].Should().Be("\"Last");
            fields[2].Should().Be(" First\"");
            
            // This test documents the limitation
            // In production, use CsvHelper library
        }

        [Fact]
        public void EscapeCsvSimple_RemovesCommas()
        {
            // Arrange
            var value = "Engineering, Software";
            
            // Act
            var escaped = Program.EscapeCsvSimple(value);
            
            // Assert
            escaped.Should().Be("Engineering  Software");
            escaped.Should().NotContain(",");
        }

        [Fact]
        public void EscapeCsvSimple_TrimsResult()
        {
            // Arrange
            var value = "  Engineering  ";
            
            // Act
            var escaped = Program.EscapeCsvSimple(value);
            
            // Assert
            escaped.Should().Be("Engineering");
        }

        [Fact]
        public void EscapeCsvSimple_HandlesMultipleCommas()
        {
            // Arrange
            var value = "A,B,C,D";
            
            // Act
            var escaped = Program.EscapeCsvSimple(value);
            
            // Assert
            escaped.Should().Be("A B C D");
        }
    }

    // ============================================
    // INTEGRATION TESTS
    // ============================================
    
    public class IntegrationTests
    {
        private readonly string _testFilesDirectory;

        public IntegrationTests()
        {
            _testFilesDirectory = Path.Combine(Path.GetTempPath(), "EmployeeIntegrationTests");
            Directory.CreateDirectory(_testFilesDirectory);
        }

        [Fact]
        public void EndToEnd_LoadAnalyzeAndAddEmployee_WorksCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "integration_test.csv");
            var initialCsv = @"EmployeeId,FirstName,LastName,Department,Salary
1,John,Doe,Engineering,75000
2,Jane,Smith,Sales,60000";
            File.WriteAllText(filePath, initialCsv);
            
            // Act - Load
            bool loadSuccess = Program.TryLoadEmployees(filePath, out var employees, out var loadErrors);
            loadSuccess.Should().BeTrue();
            employees.Should().HaveCount(2);
            
            // Act - Analyze
            var avgSalary = employees.Average(e => e.Salary);
            avgSalary.Should().Be(67500);
            
            var highest = employees.OrderByDescending(e => e.Salary).First();
            highest.EmployeeId.Should().Be(1);
            
            // Act - Add employee (simulate AppendEmployee)
            var newEmployee = new Employee(3, "Bob", "Jones", "Marketing", 65000);
            var newLine = $"{newEmployee.EmployeeId},{newEmployee.FirstName},{newEmployee.LastName},{newEmployee.Department},{newEmployee.Salary}";
            File.AppendAllText(filePath, Environment.NewLine + newLine);
            
            // Act - Reload
            bool reloadSuccess = Program.TryLoadEmployees(filePath, out var reloadedEmployees, out _);
            
            // Assert
            reloadSuccess.Should().BeTrue();
            reloadedEmployees.Should().HaveCount(3);
            reloadedEmployees.Should().Contain(e => e.EmployeeId == 3 && e.FirstName == "Bob");
            
            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void LoadLargeFile_PerformanceTest()
        {
            // Arrange - Create file with 10,000 employees
            var filePath = Path.Combine(_testFilesDirectory, "large_file.csv");
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("EmployeeId,FirstName,LastName,Department,Salary");
                for (int i = 1; i <= 10000; i++)
                {
                    writer.WriteLine($"{i},First{i},Last{i},Dept{i % 10},{50000 + (i % 50) * 1000}");
                }
            }
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool success = Program.TryLoadEmployees(filePath, out var employees, out _);
            stopwatch.Stop();
            
            // Assert
            success.Should().BeTrue();
            employees.Should().HaveCount(10000);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should load in under 5 seconds
            
            // Cleanup
            File.Delete(filePath);
        }
    }
}

// ============================================
// INTERVIEW TALKING POINTS
// ============================================

/*
KEY TESTING CONCEPTS TO DISCUSS:

1. **Test Organization**:
   - Unit tests for individual functions (parsing, analytics)
   - Integration tests for file I/O workflows
   - Performance tests for large datasets

2. **xUnit + FluentAssertions**:
   - xUnit: Modern .NET testing framework
   - FluentAssertions: Readable assertions (Should().Be() style)
   - [Theory] for parameterized tests with [InlineData]

3. **Test Categories**:
   - Parsing tests: Valid/invalid inputs, edge cases
   - File loading tests: Missing files, malformed data
   - LINQ analytics tests: Aggregations, grouping, filtering
   - Record tests: Value equality, immutability
   - Integration tests: End-to-end workflows

4. **Edge Cases Covered**:
   - Empty/null inputs
   - Whitespace handling
   - Culture-invariant parsing
   - Case-insensitive operations
   - Large file performance

5. **What's Tested**:
   - Business logic (parsing, validation)
   - Data transformations (LINQ queries)
   - Error handling (missing files, invalid data)
   - Record behavior (equality, immutability)

6. **What's NOT Tested**:
   - Console UI interactions (hard to test, minimal logic)
   - User input prompts (integration test with E2E framework)
   - File system permissions (environment-specific)

7. **Test Doubles**:
   - No mocking needed (functions are pure)
   - File I/O tested with temporary files
   - Culture tested by changing CurrentCulture

8. **Production Improvements**:
   - Add test coverage reporting (Coverlet)
   - Add mutation testing (Stryker)
   - Add benchmark tests (BenchmarkDotNet)
   - Add integration tests with real CSV library

9. **How to Run**:
   ```bash
   dotnet test
   dotnet test --filter "FullyQualifiedName~EmployeeParsingTests"
   dotnet test --logger "console;verbosity=detailed"
   ```

10. **Key Design Patterns Demonstrated**:
    - Arrange-Act-Assert (AAA) pattern
    - Test data builders (standardHeaderIndex)
    - Parameterized tests for multiple scenarios
    - Fluent assertions for readability
*/
