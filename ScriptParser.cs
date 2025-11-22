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
        /// Expands script lines like "-preset[fast,medium] -j[1..4]" into all possible parameter combinations.
        /// Uses square brackets [ ] to avoid conflict with FLAC's { } syntax (e.g. --picture={...}).
        /// Supports both explicit text values (e.g. [fast, medium, slow]) and numeric ranges (e.g. [1..4]).
        /// Handles nested brackets correctly.
        /// </summary>
        /// <param name="input">Input script string</param>
        /// <returns>List of expanded strings</returns>
        public static List<string> ExpandScriptLine(string input)
        {
            // Base case: if there are no brackets, return the input as is
            if (string.IsNullOrWhiteSpace(input) || !input.Contains('[') || !input.Contains(']'))
            {
                return new List<string> { input };
            }

            // Check for balanced brackets
            if (input.Count(c => c == '[') != input.Count(c => c == ']'))
            {
                Debug.WriteLine("Mismatched brackets in script: " + input);
                return new List<string>(); // Invalid syntax
            }

            // Find the innermost [...] block
            int lastOpenIndex = -1;
            int firstCloseIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                {
                    lastOpenIndex = i;
                }
                else if (input[i] == ']')
                {
                    firstCloseIndex = i;
                    break; // Found the first closing bracket after the last open
                }
            }

            // If no valid pair found, return as is
            if (lastOpenIndex == -1 || firstCloseIndex == -1 || lastOpenIndex > firstCloseIndex)
            {
                return new List<string> { input };
            }

            // Extract parts: prefix, content, suffix
            string prefix = input.Substring(0, lastOpenIndex);
            string content = input.Substring(lastOpenIndex + 1, firstCloseIndex - lastOpenIndex - 1);
            string suffix = input.Substring(firstCloseIndex + 1);

            // Parse the innermost content
            List<string> parsedValues = ParseRange(content);

            var result = new List<string>();

            // For each possible value from the innermost range, recursively expand the full string
            foreach (string value in parsedValues)
            {
                string newInput = prefix + value + suffix;
                List<string> expanded = ExpandScriptLine(newInput); // Recursive call
                result.AddRange(expanded);
            }

            // Remove duplicates and sort naturally
            return result.Distinct().OrderBy(x => x, new NaturalStringComparer()).ToList();
        }

        /// <summary>
        /// Parses a range string like "fast, medium..slow, 4.5" into a list of strings.
        /// Handles explicit values and numeric ranges.
        /// Text values are processed as-is. Numeric ranges are expanded.
        /// </summary>
        /// <param name="content">Comma-separated values and ranges</param>
        /// <returns>List of strings from parsed values/ranges</returns>
        private static List<string> ParseRange(string content)
        {
            var values = new List<string>();

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

                    if (segments.Length >= 2)
                    {
                        // Try to parse as numeric range
                        if (double.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double start) &&
                            double.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double end))
                        {
                            // It's a numeric range, expand it
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
                                    values.Add(Math.Round(i, 6).ToString(CultureInfo.InvariantCulture));
                                }
                            }
                            else
                            {
                                for (double i = start; Math.Round(i, 6) >= end; i -= step)
                                {
                                    values.Add(Math.Round(i, 6).ToString(CultureInfo.InvariantCulture));
                                }
                            }
                        }
                        else
                        {
                            // It's not a numeric range, treat each segment as a separate text value
                            foreach (var segment in segments)
                            {
                                if (!string.IsNullOrWhiteSpace(segment))
                                {
                                    values.Add(segment);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Edge case: only one segment after splitting by "..", treat as text
                        values.Add(trimmed);
                    }
                }
                else
                {
                    // Explicit text or number value, add as-is
                    values.Add(trimmed);
                }
            }

            // Remove duplicates
            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Generates Cartesian product of multiple string lists.
        /// Used to combine values from multiple [ranges].
        /// </summary>
        /// <param name="sequences">List of string lists</param>
        /// <returns>All combinations as lists of strings</returns>
        private static IEnumerable<List<string>> CartesianProduct(List<List<string>> sequences)
        {
            IEnumerable<List<string>> result = new[] { new List<string>() };

            foreach (var sequence in sequences)
            {
                result = from seq in result
                         from item in sequence
                         select new List<string>(seq) { item };
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