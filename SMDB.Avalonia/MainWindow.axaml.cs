using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Data;
using System.IO;
using SMDB.Core;
using SMDB.Core.Parsing;

namespace SMDB.Avalonia
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseEngine _engine;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new DatabaseEngine();
            LoadTables();
        }

        private void ExecuteQuery(object? sender, RoutedEventArgs e)
        {
            var query = QueryBox.Text ?? "";
            var output = _engine.Execute(query);
            OutputBox.Text = output;
            LoadTables();

            if (TablesList.SelectedItem is string table)
                ShowTable(table);

            QueryBox.Text = "";
        }

        private void ClearQuery(object? sender, RoutedEventArgs e)
        {
            QueryBox.Text = "";
            OutputBox.Text = "";
        }

        private void OnTableSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem is string table)
            {
                ShowTable(table);
                IndexInfoBox.Text = _engine.GetIndexInfo(table);
            }
        }

        private void LoadTables()
        {
            TablesList.ItemsSource = null;

            string storage = Path.Combine(AppContext.BaseDirectory, "Storage");
            if (!Directory.Exists(storage))
                return;

            var tables = Directory.GetFiles(storage, "*.meta");
            for (int i = 0; i < tables.Length; i++)
                tables[i] = Path.GetFileNameWithoutExtension(tables[i]);

            TablesList.ItemsSource = tables;
        }

        private void ShowTable(string table)
        {
            TableInfoText.Text =
                _engine.Execute($"TABLEINFO {table}");

            string selectOutput =
                _engine.Execute($"SELECT * FROM {table}");

            TableGrid.Text = selectOutput;
        }
    }
}