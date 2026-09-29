using System;
using System.Text;

namespace GradeTracker
{
    // ─────────────────────────────────────────────────────────────
    // CUSTOM EXCEPTION
    // Built-in exceptions (FormatException, ArgumentException) describe
    // generic problems. A custom one describes a problem in YOUR domain,
    // and can carry extra data (here: the bad score).
    // ─────────────────────────────────────────────────────────────
    public class InvalidScoreException : Exception
    {
        public int Score { get; }

        public InvalidScoreException(string message, int score) : base(message) //handles the exception error for score of negative or above 100
        {
            Score = score;
        }
    }

    public class Program
    {
        // ── DATA TYPES / VARIABLES: constants ──────────────────────
        // const = fixed at compile time, can never change.
        const int MaxScore = 100;
        const double PassMark = 40.0;
        const int MinSubjectScore = 35;
        const decimal ScholarshipPerPoint = 125.50m;   // decimal for money ('m' suffix)

        // static readonly = set once at startup, then fixed.
        static readonly string[] Subjects = { "Math", "Physics", "Chemistry", "English" };

        public static void Main()
        {
            // Raw input, as if it came from a CSV file or a web form.
            // Messy on purpose: extra spaces, mixed case, and some bad rows.
            string rawData =
                "  ravi : 85, 92, 78, 88\n" +
                "PRIYA: 95, 89, 94, 91\n" +
                " arjun:45, 38, 52, 30\n" +
                "Meena : 70, abc, 65, 72\n" +     // "abc" is not a number
                "kiran: 60, 105, 55, 58\n" +      // 105 > MaxScore
                "Dev: 70, -5, 60, 65\n" +         // negative score
                "Sita 80, 80, 80, 80\n";          // missing ':'

            // ── STRINGS: Split into lines ──────────────────────────
            string[] lines = rawData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // ── ARRAYS ─────────────────────────────────────────────
            int count = lines.Length;
            string[] names = new string[count];              // 1-D array
            int[,] scores = new int[count, Subjects.Length]; // 2-D array: [student, subject]
            bool[] valid = new bool[count];                  // defaults to all false

            // ── VARIABLES of different types ───────────────────────
            int processed = 0;
            int bestTotal = -1;
            string? bestName = null;   // '?' = this string is allowed to be null

            // ── EXCEPTION HANDLING: parse each line safely ─────────
            for (int i = 0; i < count; i++)
            {
                try
                {
                    names[i] = ParseLine(lines[i], scores, i);
                    valid[i] = true;

                    int total = RowTotal(scores, i);
                    UpdateTopper(total, names[i], ref bestTotal, ref bestName);
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"[Line {i + 1}] Skipped - bad number: {ex.Message}");
                }
                catch (InvalidScoreException ex) when (ex.Score > MaxScore)   // exception filter
                {
                    Console.WriteLine($"[Line {i + 1}] Skipped - {ex.Message}: {ex.Score} is above {MaxScore}");
                }
                catch (InvalidScoreException ex)
                {
                    Console.WriteLine($"[Line {i + 1}] Skipped - {ex.Message}: {ex.Score} is negative");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"[Line {i + 1}] Skipped - {ex.Message}");
                }
                finally
                {
                    processed++;   // runs whether the try succeeded or failed
                }
            }

            // ── STRINGS: StringBuilder for building a big report ───
            var report = new StringBuilder();   // 'var' = compiler infers the type
            report.AppendLine();
            report.AppendLine($"{"Name",-8}|{"Total",6} |{"Avg",7} |{"Min",4} |{"Max",4} | Grade | Result | Bar");
            report.AppendLine(new string('-', 72));

            double[] averages = new double[count];
            int validCount = 0;
            int passed = 0;

            for (int i = 0; i < count; i++)
            {
                if (!valid[i]) continue;   // '!' = logical NOT

                int total = RowTotal(scores, i);
                double avg = Stats(scores, i, out int min, out int max);
                char grade = GetGrade(avg);

                // ── OPERATORS: logical AND + comparison ────────────
                bool pass = avg >= PassMark && min >= MinSubjectScore;

                // ── OPERATORS: ternary ─────────────────────────────
                string result = pass ? "PASS" : "FAIL";
                if (pass) passed++;

                averages[validCount++] = avg;   // use current value, THEN increment

                // ── OPERATORS: division + modulus ──────────────────
                // Each '#' = 10 marks; add '+' if the leftover is 5 or more.
                int whole = (int)avg / 10;
                bool half = (int)avg % 10 >= 5;
                string bar = new string('#', whole) + (half ? "+" : "");

                report.AppendLine(
                    $"{names[i],-8}|{total,6} |{avg,7:F2} |{min,4} |{max,4} |   {grade}   |  {result}  | {bar}");
            }

            Console.Write(report.ToString());

            // ── ARRAYS: copy only the filled part ──────────────────
            double[] validAverages = new double[validCount];
            Array.Copy(averages, validAverages, validCount);

            // ── OPERATORS: integer vs floating-point division ──────
            int passRateInt = validCount == 0 ? 0 : passed * 100 / validCount;          // whole number
            double passRate = validCount == 0 ? 0 : (double)passed / validCount * 100;  // exact

            // ── DATA TYPES: int * decimal -> decimal ───────────────
            decimal scholarship = bestTotal > 0 ? bestTotal * ScholarshipPerPoint : 0m;

            Console.WriteLine(new string('-', 72));
            Console.WriteLine($"Lines processed : {processed} ({validCount} valid, {processed - validCount} skipped)");
            Console.WriteLine($"Class average   : {Average(validAverages):F2}");
            Console.WriteLine($"Pass rate       : {passRate:F1}% (integer maths would say {passRateInt}%)");
            Console.WriteLine($"Topper          : {bestName ?? "none"} with {bestTotal} marks");   // ?? = null-coalescing
            Console.WriteLine($"Scholarship     : Rs. {scholarship:N2}");
            Console.WriteLine($"Subjects        : {string.Join(", ", Subjects).ToUpper()}");
        }

        // ═════════════════════════════════════════════════════════════
        // METHODS
        // ═════════════════════════════════════════════════════════════

        // Parses "  ravi : 85, 92, 78, 88" -> fills one row of scores, returns "Ravi".
        // Throws exceptions instead of returning garbage.
        static string ParseLine(string line, int[,] scores, int row)
        {
            if (string.IsNullOrWhiteSpace(line))
                throw new ArgumentException("Line is empty");

            int colon = line.IndexOf(':');
            if (colon < 0)
                throw new ArgumentException($"Missing ':' in \"{line.Trim()}\"");

            string name = Capitalize(line.Substring(0, colon).Trim());
            string[] parts = line.Substring(colon + 1).Split(',');

            if (parts.Length != Subjects.Length)
                throw new ArgumentException($"Expected {Subjects.Length} scores, found {parts.Length}");

            for (int j = 0; j < parts.Length; j++)
            {
                int score = int.Parse(parts[j].Trim());   // throws FormatException on "abc"

                if (score < 0 || score > MaxScore)        // '||' = logical OR
                    throw new InvalidScoreException($"{Subjects[j]} score out of range", score);

                scores[row, j] = score;
            }

            return name;
        }

        // "rAVI" -> "Ravi"
        static string Capitalize(string s)
        {
            if (s.Length == 0) return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLower();
        }

        // Sum of one row in the 2-D array.
        static int RowTotal(int[,] grid, int row)
        {
            int sum = 0;
            for (int col = 0; col < grid.GetLength(1); col++)   // GetLength(1) = number of columns
                sum += grid[row, col];                          // compound assignment
            return sum;
        }

        // 'out' parameters: one method, three results (avg returned, min & max via out).
        static double Stats(int[,] grid, int row, out int min, out int max)
        {
            min = int.MaxValue;
            max = int.MinValue;

            for (int col = 0; col < grid.GetLength(1); col++)
            {
                int v = grid[row, col];
                if (v < min) min = v;
                if (v > max) max = v;
            }

            // Cast BEFORE dividing, otherwise 343 / 4 = 85 (integer division drops .75).
            return (double)RowTotal(grid, row) / grid.GetLength(1);
        }

        // 'ref' parameters: the method changes the CALLER's variables directly.
        static void UpdateTopper(int total, string name, ref int bestTotal, ref string? bestName)
        {
            if (total > bestTotal)
            {
                bestTotal = total;
                bestName = name;
            }
        }

        // 'params': accepts an array OR loose values -> Average(1, 2, 3) also works.
        static double Average(params double[] values)
        {
            if (values.Length == 0) return 0;

            double sum = 0;
            foreach (double v in values) sum += v;
            return sum / values.Length;
        }

        // Expression-bodied method + switch expression with relational patterns.
        static char GetGrade(double avg) => avg switch
        {
            >= 90 => 'A',
            >= 75 => 'B',
            >= 60 => 'C',
            >= PassMark => 'D',
            _ => 'F'
        };
    }
}