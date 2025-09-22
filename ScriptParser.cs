using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FLAC_Benchmark_H
{
    public static class ScriptParser
    {
        /// <summary>
        /// Expands script lines like "-[0.5..3.5..0.1] -j[1..4]" into all possible parameter combinations.
        /// Uses square brackets [ ] to avoid conflict with FLAC's { } syntax (e.g. --picture={...}).
        /// Supports integers and floating-point numbers.
        /// </summary>
        /// <param name="input">Input script string</param>
        /// <returns>List of expanded strings, naturally sorted</returns>
        public static List<string> ExpandScriptLine(string input)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
                return result; // Nothing to expand

            // Check for balanced brackets
            if (input.Count(c => c == '[') != input.Count(c => c == ']'))
            {
                Debug.WriteLine("Mismatched brackets in script: " + input);
                return result; // Invalid syntax
            }

            // Find all [content] blocks
            var matches = Regex.Matches(input, @"\[([^\]]*)\]");
            if (matches.Count == 0)
            {
                result.Add(input); // No ranges, return original
                return result;
            }

            // Split input around each [block]
            var parts = new List<(string prefix, string content, int index)>();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                string prefix = input.Substring(lastIndex, match.Index - lastIndex);
                string content = match.Groups[1].Value;
                parts.Add((prefix, content, match.Index));
                lastIndex = match.Index + match.Length;
            }

            string suffix = input.Substring(lastIndex); // After last ]
            var valueLists = parts.Select(p => ParseRange(p.content)).ToList(); // Parse each range

            if (valueLists.Any(l => l.Count == 0))
            {
                Debug.WriteLine("One or more ranges produced no values: " + input);
                return result; // Skip invalid ranges
            }

            // Generate all combinations of values
            var combinations = CartesianProduct(valueLists).ToList();

            foreach (var combination in combinations)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < parts.Count; i++)
                {
                    sb.Append(parts[i].prefix);
                    sb.Append(combination[i].ToString(CultureInfo.InvariantCulture)); // Ensure consistent decimal point
                }
                sb.Append(suffix);
                result.Add(sb.ToString());
            }

            // Remove duplicates and sort naturally (e.g. 1.2 before 1.10)
            return result.Distinct().OrderBy(x => x, new NaturalStringComparer()).ToList();
        }

        /// <summary>
        /// Parses a range string like "1.5, 2.0..3.0..0.1, 4.5" into a list of doubles.
        /// Uses invariant culture for parsing (accepts "." as decimal separator).
        /// </summary>
        /// <param name="content">Comma-separated values and ranges</param>
        /// <returns>List of doubles from parsed values/ranges</returns>
        private static List<double> ParseRange(string content)
        {
            var values = new List<double>();

            foreach (var part in content.Split(','))
            {
                string trimmed = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue; // Skip empty

                if (trimmed.Contains(".."))
                {
                    // Split by ".." and trim spaces
                    var segments = trimmed.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray();

                    if (segments.Length >= 2 &&
                        double.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double start) &&
                        double.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double end))
                    {
                        double step = 1.0;

                        // Optional step: [start..end..step]
                        if (segments.Length > 2 &&
                            double.TryParse(segments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedStep))
                        {
                            step = Math.Abs(parsedStep);
                            if (step == 0)
                            {
                                Debug.WriteLine($"Step cannot be zero: {trimmed}, using 1.0");
                                step = 1.0;
                            }
                        }

                        // Forward or reverse range
                        if (start <= end)
                        {
                            for (double i = start; Math.Round(i, 6) <= end; i += step)
                            {
                                values.Add(Math.Round(i, 6)); // Avoid floating-point errors
                            }
                        }
                        else
                        {
                            for (double i = start; Math.Round(i, 6) >= end; i -= step)
                            {
                                values.Add(Math.Round(i, 6));
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Invalid range format: {trimmed}");
                    }
                }
                else if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                {
                    values.Add(Math.Round(val, 6)); // Normalize precision
                }
                else
                {
                    Debug.WriteLine($"Invalid number in range: {trimmed}");
                }
            }

            // Remove duplicates and sort
            return values.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Generates Cartesian product of multiple double lists.
        /// Used to combine values from multiple [ranges].
        /// </summary>
        /// <param name="sequences">List of double lists</param>
        /// <returns>All combinations as lists of doubles</returns>
        private static IEnumerable<List<double>> CartesianProduct(List<List<double>> sequences)
        {
            IEnumerable<List<double>> result = new[] { new List<double>() };

            foreach (var sequence in sequences)
            {
                result = from seq in result
                         from item in sequence
                         select new List<double>(seq) { item };
            }

            return result;
        }

        /// <summary>
        /// Compares strings using natural sorting (e.g., "file2.txt" before "file10.txt").
        /// Ensures parameters like 1.2 appear before 1.10 in output.
        /// </summary>
        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return CompareNatural(x, y);
            }

            private static int CompareNatural(string strA, string strB)
            {
                int i1 = 0, i2 = 0;
                while (i1 < strA.Length && i2 < strB.Length)
                {
                    if (char.IsDigit(strA[i1]) || char.IsDigit(strB[i2]))
                    {
                        // Parse numbers (including decimals)
                        bool hasDecimalA = false, hasDecimalB = false;
                        int dotCountA = 0, dotCountB = 0;
                        long wholeA = 0, wholeB = 0;
                        long fracA = 0, fracB = 0;
                        int fracDigitsA = 0, fracDigitsB = 0;

                        // Parse first number
                        while (i1 < strA.Length)
                        {
                            char c = strA[i1];
                            if (c == '.' && dotCountA == 0)
                            {
                                hasDecimalA = true;
                                dotCountA++;
                                i1++;
                                break;
                            }
                            else if (char.IsDigit(c))
                            {
                                if (hasDecimalA)
                                {
                                    fracA = fracA * 10 + (c - '0');
                                    fracDigitsA++;
                                }
                                else
                                {
                                    wholeA = wholeA * 10 + (c - '0');
                                }
                                i1++;
                            }
                            else break;
                        }
                        while (i1 < strA.Length && char.IsDigit(strA[i1]) && hasDecimalA)
                        {
                            fracA = fracA * 10 + (strA[i1] - '0');
                            fracDigitsA++;
                            i1++;
                        }

                        // Parse second number
                        while (i2 < strB.Length)
                        {
                            char c = strB[i2];
                            if (c == '.' && dotCountB == 0)
                            {
                                hasDecimalB = true;
                                dotCountB++;
                                i2++;
                                break;
                            }
                            else if (char.IsDigit(c))
                            {
                                if (hasDecimalB)
                                {
                                    fracB = fracB * 10 + (c - '0');
                                    fracDigitsB++;
                                }
                                else
                                {
                                    wholeB = wholeB * 10 + (c - '0');
                                }
                                i2++;
                            }
                            else break;
                        }
                        while (i2 < strB.Length && char.IsDigit(strB[i2]) && hasDecimalB)
                        {
                            fracB = fracB * 10 + (strB[i2] - '0');
                            fracDigitsB++;
                            i2++;
                        }

                        // Normalize fractional parts to same scale
                        long scaledA = wholeA * (long)Math.Pow(10, Math.Max(fracDigitsA, fracDigitsB)) +
                                      fracA * (long)Math.Pow(10, Math.Max(fracDigitsA, fracDigitsB) - fracDigitsA);
                        long scaledB = wholeB * (long)Math.Pow(10, Math.Max(fracDigitsA, fracDigitsB)) +
                                      fracB * (long)Math.Pow(10, Math.Max(fracDigitsA, fracDigitsB) - fracDigitsB);

                        if (scaledA != scaledB)
                            return scaledA.CompareTo(scaledB);
                    }
                    else
                    {
                        int result = char.ToLowerInvariant(strA[i1]).CompareTo(char.ToLowerInvariant(strB[i2]));
                        if (result != 0)
                            return result;
                        i1++;
                        i2++;
                    }
                }

                return strA.Length.CompareTo(strB.Length);
            }
        }
    }
}