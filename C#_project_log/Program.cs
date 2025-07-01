using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.VisualBasic.FileIO;


namespace C__project_log
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string filePath = "log_Entity.csv";
            string filePath = ConfigurationManager.AppSettings["CsvLogFile"];

            string[] columnsToExtract = ConfigurationManager.AppSettings["ColumnsToExtract"]
                .Split(',')
                .Select(c => c.Trim().ToUpper())
                .ToArray();


            // check if the file exist
            if (!File.Exists(filePath))
            {
                Console.WriteLine(" File not found : " + filePath);
                Console.ReadLine();
                return;
            }
            else
            {
                Console.WriteLine(" File found : " + filePath);
            }


            List<LogEntry> logs = new List<LogEntry>();
            Dictionary<string, int> headerIndexes = null;

            // parsing for csv file (;)
            using (TextFieldParser parser = new TextFieldParser(filePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");
                parser.HasFieldsEnclosedInQuotes = true;


                // read the index of the column
                if (!parser.EndOfData)
                {
                    string[] headers = parser.ReadFields();
                   
                    // to show the headers read
                    Console.WriteLine("[DEBUG] Headers read : \n");
                    foreach (String h in headers)
                    {
                        Console.WriteLine("-> '" + h + "'");
                    }

                    //use the dictionary declared before
                    headerIndexes = headers
                        .Select((name, index) => new { name, index })
                        .ToDictionary(h => h.name.Trim().ToUpper(), h => h.index);

                }

                // read the data line
                while (!parser.EndOfData)
                {
                    string[] fields;

                    // skip the empty line in the csv
                    try
                    {
                        fields = parser.ReadFields();
                    }
                    catch (MalformedLineException)
                    {
                        continue;
                    }

                    if (fields.Length == 0) continue;

                    int tsIndex = headerIndexes.TryGetValue("TIMESTAMP", out int t) ? t : -1;
                    int cIndex = headerIndexes.TryGetValue("CALLSTACK", out int c) ? c : -1;
                    int csIndex = headerIndexes.TryGetValue("ERRORCALLSTACK", out int cs) ? cs : -1;
                    int mIndex = headerIndexes.TryGetValue("EVENTMESSAGE", out int m) ? m : -1;
                    int tnIndex = headerIndexes.TryGetValue("TASKNAME", out int tn) ? tn : -1;

                    // for convert the date
                    DateTime timestamp;
                    DateTime.TryParse(
                        tsIndex >= 0 && tsIndex < fields.Length ? fields[tsIndex] : "",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out timestamp
                    );

                    // call my Object LogEntry
                    var log = new LogEntry
                    {
                        TimeStamp = tsIndex >= 0 && tsIndex < fields.Length ? timestamp : default,
                        CallStack = cIndex >= 0 && cIndex < fields.Length ? fields[cIndex] : "",
                        ErrorCallStack = csIndex >= 0 && csIndex < fields.Length ? fields[csIndex] : "",
                        EventMessage = mIndex >= 0 && mIndex < fields.Length ? fields[mIndex] : "",
                        TaskName = tnIndex >= 0 && tnIndex < fields.Length ? fields[tnIndex] : ""
                    };

                    logs.Add(log);
                }
            }


            //display the order
            IEnumerable<LogEntry> allLogs = logs;


            // loop to display without stopping the program
            while (true)
            {
                Console.WriteLine();
                Console.Write("Enter a keyword to search for (or type ‘exit’ to quit) : ");
                string filterKeyword = Console.ReadLine();

                // for close the program
                if (string.Equals(filterKeyword, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("End of program.");
                    break;
                }

                Console.Clear();
                Console.WriteLine("\n === RESULTS FOR : \"" + filterKeyword + "\" === \n");

                IEnumerable<LogEntry> filteredLogs = allLogs;

                // Filter with LINQ
                if (!string.IsNullOrWhiteSpace(filterKeyword))
                {
                    filteredLogs = allLogs
                        .Where(log =>
                            (log.EventMessage != null && log.EventMessage.ToLower().Contains(filterKeyword.ToLower())) ||
                            (log.CallStack != null && log.CallStack.ToLower().Contains(filterKeyword.ToLower())) ||
                            (log.ErrorCallStack != null && log.ErrorCallStack.ToLower().Contains(filterKeyword.ToLower())) ||
                            (log.TaskName != null && log.TaskName.ToLower().Contains(filterKeyword.ToLower()))
                        );
                }
                // method for count the result 
                int count = filteredLogs.Count();
                Console.WriteLine($"[Results found : {count}] \n");


                // for return the selected data after

                Console.WriteLine("Colonnes à afficher : " + string.Join(", ", columnsToExtract));
                Console.WriteLine("\n === DEBUT AFFICHAGE CALLSTACK UNIQUEMENT ===");

                foreach (LogEntry log in filteredLogs)
                {
                    List<string> output = new List<string>();

                    foreach (string col in columnsToExtract)
                    {
                        switch (col)
                        {
                            case "TIMESTAMP":
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write(log.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                Console.ResetColor();
                                output.Add("");
                                break;
                            case "CALLSTACK":
                                output.Add(log.CallStack);
                                break;
                            case "ERRORCALLSTACK":
                                output.Add(log.ErrorCallStack);
                                break;
                            case "EVENTMESSAGE":
                                output.Add(log.EventMessage);
                                break;
                            case "TASKNAME":
                                output.Add(log.TaskName);
                                break;
                            default:
                                output.Add($"[Unknow: {col}]");
                                break;
                        }
                    }

                    Console.WriteLine(string.Join(" | ", output));
                }

                Console.WriteLine("=== FIN AFFICHAGE ===");

                // Export csv option
                Console.Write("\n Do you want to export this data? (o/n) : ");
                string exportChoice = Console.ReadLine();

                if (exportChoice.Equals("o", StringComparison.OrdinalIgnoreCase))
                {
                    // name of the csv file and the location to save
                    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string exportFileName = $"filtered_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string exportPath = Path.Combine(downloadsPath, exportFileName);

                    // library streamWriter
                    using (StreamWriter writer = new StreamWriter(exportPath, false, Encoding.UTF8))
                    {
                        // Writing the headers
                        writer.WriteLine(string.Join(";", columnsToExtract));

                        // Writing the filters rows
                        foreach (LogEntry log in filteredLogs)
                        {
                            List<string> exportValues = new List<string>();

                            foreach (string col in columnsToExtract)
                            {
                                switch (col)
                                {
                                    case "TIMESTAMP":
                                        exportValues.Add(log.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                        break;
                                    case "CALLSTACK":
                                        exportValues.Add(log.CallStack);
                                        break;
                                    case "ERRORCALLSTACK":
                                        exportValues.Add(log.ErrorCallStack);
                                        break;
                                    case "EVENTMESSAGE":
                                        exportValues.Add(log.EventMessage);
                                        break;
                                    case "TASKNAME":
                                        exportValues.Add(log.TaskName);
                                        break;
                                    default:
                                        exportValues.Add("");
                                        break;
                                }
                            }

                            writer.WriteLine(string.Join(";", exportValues.Select(val => $"\"{val}\""))); // prevent character errors
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nRésultats exportés dans : {exportPath}");
                    Console.ResetColor();
                }

            }
        }
    }
}
