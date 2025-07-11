using Microsoft.VisualBasic.FileIO;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Wpf_project_log.Models;
using Wpf_project_log.Helpers;
using Wpf_project_log.Views;

namespace Wpf_project_log.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // === Properties ===

        // it updates when there are modifications
        private ObservableCollection<LogEntry> _filteredLogs = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> FilteredLogs
        {
            get => _filteredLogs;
            set => SetProperty(ref _filteredLogs, value);
        }


        // for save the last search
        private ObservableCollection<string> _searchHistory = new ObservableCollection<string>();
        public ObservableCollection<string> SearchHistory
        {
            get => _searchHistory;
            set => SetProperty(ref _searchHistory, value);
        }


        // === getter and setters for filterKeyword ===
        private string _filterKeyword;
        public string FilterKeyword
        {
            get => _filterKeyword;
            set
            {
                if (SetProperty(ref _filterKeyword, value))
                {
                    CommandManager.InvalidateRequerySuggested(); // activate the button when a filterKeyword was enter
                }
            }
        }

        public List<string> ColumnsToExtract { get; private set; }

        private List<LogEntry> _allLogs = new List<LogEntry>();

        // === Commands ===
        public ICommand FilterCommand { get; }
        public ICommand ExportCommand { get; }

        // === Constructor ===
        public MainViewModel()
        {
            ColumnsToExtract = ConfigurationManager.AppSettings["ColumnsToExtract"]
                .Split(',')
                .Select(c => c.Trim().ToUpper())
                .ToList();


            // use constructor from RelayCommand.cs
            FilterCommand = new RelayCommand(ExecuteFilter);
            ExportCommand = new RelayCommand(param => ExecuteExport(param), param => CanExecuteExport(param));
        }
        private bool CanExecuteExport(object parameter)
        {
            return !string.IsNullOrWhiteSpace(FilterKeyword)
                && FilteredLogs != null
                && FilteredLogs.Any();
        }


        // === for load log from csv ===
        public void LoadLogsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            Dictionary<string, int> headerIndexes;
            List<LogEntry> logs = new List<LogEntry>();

            // parsing csv file function
            using (TextFieldParser parser = new TextFieldParser(filePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");
                parser.HasFieldsEnclosedInQuotes = true;

                if (!parser.EndOfData)
                    headerIndexes = parser.ReadFields()
                        .Select((name, index) => new { name, index })
                        .ToDictionary(h => h.name.Trim().ToUpper(), h => h.index);
                else return;

                while (!parser.EndOfData)
                {
                    string[]? fields;
                    try
                    {
                        fields = parser.ReadFields();
                    }
                    catch (MalformedLineException) { continue; }


                    if (fields != null)
                    {
                        if (fields.Length == 0) continue;
                    }
                    else
                    {
                        break;
                    }

                    int tsIndex = headerIndexes.TryGetValue("TIMESTAMP", out int tsVal) ? tsVal : -1;
                    int csIndex = headerIndexes.TryGetValue("CALLSTACK", out int csVal) ? csVal : -1;
                    int errCsIndex = headerIndexes.TryGetValue("ERRORCALLSTACK", out int errVal) ? errVal : -1;
                    int msgIndex = headerIndexes.TryGetValue("EVENTMESSAGE", out int msgVal) ? msgVal : -1;
                    int taskIndex = headerIndexes.TryGetValue("TASKNAME", out int taskVal) ? taskVal : -1;

                    DateTime.TryParse(
                        tsIndex >= 0 && tsIndex < fields.Length ? fields[tsIndex] : "",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out DateTime timestamp
                    );

                    logs.Add(new LogEntry // use LogEntry object
                    {
                        TimeStamp = timestamp,
                        CallStack = csIndex >= 0 ? fields[csIndex] : "",
                        ErrorCallStack = errCsIndex >= 0 ? fields[errCsIndex] : "",
                        EventMessage = msgIndex >= 0 ? fields[msgIndex] : "",
                        TaskName = taskIndex >= 0 ? fields[taskIndex] : ""
                    });
                }
            }

            _allLogs = logs;
            FilteredLogs = new ObservableCollection<LogEntry>(_allLogs);
        }

        // Icommand action for import csv file
        public ICommand ImportCommand => new RelayCommand(ImportCsvFile);


        // === open download file ===
        private void ImportCsvFile(object parameter)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Select a CSV file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadLogsFromFile(openFileDialog.FileName);
            }
        }


        // === for filter the logs ===
        // ICommand action for ExecuteFilter
        private void ExecuteFilter(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FilterKeyword))
            {
                FilteredLogs = new ObservableCollection<LogEntry>(_allLogs);
                return;
            }

            // search history gestion
            if (!SearchHistory.Contains(FilterKeyword))
            {
                SearchHistory.Insert(0, FilterKeyword);
            }
            else
            {
                // if already present, then move it to the top
                SearchHistory.Remove(FilterKeyword);
                SearchHistory.Insert(0, FilterKeyword);
            }

            // keep the last five
            while (SearchHistory.Count > 5)
            {
                SearchHistory.RemoveAt(SearchHistory.Count - 1);
            }

            string keyword = FilterKeyword.ToLowerInvariant();
            IEnumerable<LogEntry> filtered = _allLogs.Where(log =>
                (!string.IsNullOrEmpty(log.EventMessage) && log.EventMessage.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(log.CallStack) && log.CallStack.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(log.ErrorCallStack) && log.ErrorCallStack.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(log.TaskName) && log.TaskName.ToLower().Contains(keyword))
            );

            // only displays logs that match with the filter
            FilteredLogs = new ObservableCollection<LogEntry>(filtered);
            CommandManager.InvalidateRequerySuggested(); // forces the button to check the status
        }

        // Icommand action for ExecuteExport
        private void ExecuteExport(object parameter) // export the data filtered in csv file
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string exportFileName = $"filtered_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string exportPath = Path.Combine(downloadsPath, exportFileName);

            using (StreamWriter writer = new StreamWriter(exportPath, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(";", ColumnsToExtract));

                foreach (LogEntry log in FilteredLogs)
                {
                    List<string> exportValues = new List<string>();

                    foreach (string column in ColumnsToExtract)
                    {
                        switch (column)
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

                    writer.WriteLine(string.Join(";", exportValues.Select(val => $"\"{val}\"")));
                }
            }

            System.Windows.MessageBox.Show("Export completed :\n" + exportPath, "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}