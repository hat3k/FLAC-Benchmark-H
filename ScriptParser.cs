using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FLAC_Benchmark_H
{
    public static class ScriptParser
    {
        /// <summary>
        /// Expands script lines like "-{0..8} -j{1..4}" into all possible parameter combinations.
        /// </summary>
        /// <param name="input">Input script string</param>
        /// <returns>List of expanded strings, naturally sorted</returns>
        public static List<string> ExpandScriptLine(string input)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
                return result; // Nothing to expand

            // Check for balanced braces
            if (input.Count(c => c == '{') != input.Count(c => c == '}'))
            {
                Debug.WriteLine("Mismatched braces in script: " + input);
                return result; // Invalid syntax
            }

            // Find all {content} blocks
            var matches = Regex.Matches(input, @"\{([^}]*)\}");
            if (matches.Count == 0)
            {
                result.Add(input); // No ranges, return original
                return result;
            }

            // Split input around each {block}
            var parts = new List<(string prefix, string content, int index)>();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                string prefix = input.Substring(lastIndex, match.Index - lastIndex);
                string content = match.Groups[1].Value;
                parts.Add((prefix, content, match.Index));
                lastIndex = match.Index + match.Length;
            }

            string suffix = input.Substring(lastIndex); // After last }
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
                    sb.Append(combination[i]);
                }
                sb.Append(suffix);
                result.Add(sb.ToString());
            }

            // Remove duplicates and sort naturally (e.g. -j2 before -j10)
            return result.Distinct().OrderBy(x => x, new NaturalStringComparer()).ToList();
        }

        /// <summary>
        /// Parses a range string like "1, 3..7..2, 10" into a list of integers.
        /// </summary>
        /// <param name="content">Comma-separated values and ranges</param>
        /// <returns>List of integers from parsed values/ranges</returns>
        private static List<int> ParseRange(string content)
        {
            var values = new List<int>();

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
                    int.TryParse(segments[0], out int start) &&
                    int.TryParse(segments[1], out int end))
                    {
                        int step = 1;

                        // Optional step: {start..end..step}
                        if (segments.Length > 2 && int.TryParse(segments[2], out int parsedStep))
                        {
                            step = Math.Abs(parsedStep);
                            if (step == 0)
                            {
                                Debug.WriteLine($"Step cannot be zero: {trimmed}, using 1");
                                step = 1;
                            }
                        }

                        // Forward or reverse range
                        if (start <= end)
                        {
                            for (int i = start; i <= end; i += step)
                                values.Add(i);
                        }
                        else
                        {
                            for (int i = start; i >= end; i -= step)
                                values.Add(i);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Invalid range format: {trimmed}");
                    }
                }
                else if (int.TryParse(trimmed, out int val))
                {
                    values.Add(val); // Single number
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
        /// Generates Cartesian product of multiple integer lists.
        /// Used to combine values from multiple {ranges}.
        /// </summary>
        /// <param name="sequences">List of integer lists</param>
        /// <returns>All combinations as lists of integers</returns>
        private static IEnumerable<List<int>> CartesianProduct(List<List<int>> sequences)
        {
            IEnumerable<List<int>> result = new[] { new List<int>() };

            foreach (var sequence in sequences)
            {
                result = from seq in result
                         from item in sequence
                         select new List<int>(seq) { item };
            }

            return result;
        }

        /// <summary>
        /// Compares strings using natural sorting (e.g., "file2.txt" before "file10.txt").
        /// Ensures parameters like -j2 appear before -j10 in output.
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
                    if (char.IsDigit(strA[i1]) && char.IsDigit(strB[i2]))
                    {
                        long n1 = 0, n2 = 0;
                        while (i1 < strA.Length && char.IsDigit(strA[i1]))
                            n1 = n1 * 10 + (strA[i1++] - '0');
                        while (i2 < strB.Length && char.IsDigit(strB[i2]))
                            n2 = n2 * 10 + (strB[i2++] - '0');

                        if (n1 != n2)
                            return n1.CompareTo(n2); // Numeric comparison
                    }
                    else
                    {
                        int result = char.ToLowerInvariant(strA[i1]).CompareTo(char.ToLowerInvariant(strB[i2]));
                        if (result != 0)
                            return result; // Case-insensitive char comparison
                        i1++;
                        i2++;
                    }
                }

                return strA.Length.CompareTo(strB.Length); // Shorter strings first
            }
        }
    }
}