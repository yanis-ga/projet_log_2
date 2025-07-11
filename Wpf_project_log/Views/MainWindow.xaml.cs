using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf_project_log.ViewModels;

namespace Wpf_project_log.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // for save the history search
        private readonly string _historyFilePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\search_history.txt"));

        private readonly List<string> _searchHistory = new List<string>();
        private const int MaxHistoryCount = 5;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
            SearchHistoryListBox.Visibility = Visibility.Collapsed;
            LoadSearchHistory();
        }


        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e) // display the ListBox history
        {
                SearchHistoryListBox.Visibility = Visibility.Visible;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e) // hide the history
        {
            // Short delay to allow clicking on elements
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(200);
                if (SearchHistoryListBox != null)
                    SearchHistoryListBox.Visibility = Visibility.Collapsed;  // hide the history
            });
        }

        private void SearchHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchHistoryListBox.SelectedItem is string keyword)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.FilterKeyword = keyword;
                    vm.FilterCommand.Execute(null);
                }

                SearchHistoryListBox.Visibility = Visibility.Collapsed;
                SaveSearchHistory();
            }
        }

        private void SaveSearchHistory()
        {
            try
            {
                File.WriteAllLines(_historyFilePath, _searchHistory);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving history : " + ex.Message);
            }
        }

        private void LoadSearchHistory() // load the last search
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    string[] linesArray = File.ReadAllLines(_historyFilePath);
                    List<string> linesList = new List<string>();

                    foreach (string line in linesArray)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !linesList.Contains(line))
                        {
                            linesList.Add(line);
                        }

                        if (linesList.Count >= MaxHistoryCount)
                            break;
                    }

                    _searchHistory.Clear();
                    _searchHistory.AddRange(linesList);

                    SearchHistoryListBox.ItemsSource = null;
                    SearchHistoryListBox.ItemsSource = _searchHistory;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error when loading history : " + ex.Message);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Force TextBox binding update
            BindingExpression binding = SearchTextBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();

            string keyword = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Add FilterWord in history
                if (!_searchHistory.Contains(keyword))
                {
                    _searchHistory.Insert(0, keyword);
                    if (_searchHistory.Count > MaxHistoryCount)
                        _searchHistory.RemoveAt(_searchHistory.Count - 1);
                }
                else
                {
                    // avoid duplication
                    _searchHistory.Remove(keyword);
                    _searchHistory.Insert(0, keyword);
                }

                // Refresh
                SearchHistoryListBox.ItemsSource = null;
                SearchHistoryListBox.ItemsSource = _searchHistory;

                // Save
                SaveSearchHistory();
            }
        }
    }
}