using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EducatorGrades
{
    class Program
    {
        static void Main(string[] args)
        {
            var exe = Environment.GetCommandLineArgs()[0]; // Command invocation part
            var rawCmd = Environment.CommandLine;          // Complete command
            var argsOnly = rawCmd.Remove(rawCmd.IndexOf(exe), exe.Length).TrimStart('"').Substring(1);
            if (string.IsNullOrEmpty(argsOnly))
            {
                Console.WriteLine("Please provide an Educator curl command...");
                Console.Read();
                return;
            }

            // Remove the "curl " and " --compressed" parts of the command, because we're not using them.
            var command = argsOnly
                .RemovePreFix("curl ")
                .Replace(" --compressed", string.Empty)
                .EnsureStartsWith('\"');
            // Make sure the the string starts with a quote and the first occurence of a space gets replaced by another quote.
            var regex = new Regex(Regex.Escape(" "));
            command = regex.Replace(command, "\" ", 1);
            command = $"-L {command}";

            // Run the curl command.
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Windows\System32\curl.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.SystemDirectory
                }
            };
            p.Start();

            StringBuilder sb = new StringBuilder();
            while (!p.StandardOutput.EndOfStream)
            {
                sb.AppendLine(p.StandardOutput.ReadLine());
            }
            p.Dispose();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(sb.ToString());

            var averageGrade = doc.DocumentNode
                .SelectNodes("//dl[@class=\"exam-unit exam-unit__grade exam-unit-combined__grade\"]")
                .Select(dl => dl.ChildNodes[1].ChildNodes[1].Attributes["data-content"].Value)
                .Where(c => !c.Contains("Voldaan"))
                .Average(v =>
                {
                    if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                    {
                        return result;
                    }

                    return float.NaN;
                });

            Console.WriteLine(averageGrade);
            Console.Read();
        }
    }
}
