using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using LogManager.Models;
using Microsoft.Win32;

namespace LogManager.ViewModels
{
    /// <summary>
    /// ViewModel для управления логами в приложении.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Коллекция фильтрованных логов для отображения.
        /// </summary>
        public ObservableCollection<LogEntry> Logs { get; set; } = new();

        /// <summary>
        /// Список уровней логирования для выбора.
        /// </summary>
        public List<string> Levels { get; } = new() { "Все", "INFO", "WARN", "ERROR" };

        /// <summary>
        /// Список вариантов группировки логов.
        /// </summary>
        public List<string> GroupingOptions { get; } = new() { "Без группировки", "По уровню", "По дате" };

        private string _selectedLevel = "Все";
        private string _selectedGrouping = "Без группировки";
        private string _filterText;

        /// <summary>
        /// Выбранный уровень логирования.
        /// </summary>
        public string SelectedLevel
        {
            get => _selectedLevel;
            set { _selectedLevel = value; ApplyFilter(); OnPropertyChanged(); }
        }

        /// <summary>
        /// Выбранный вариант группировки логов.
        /// </summary>
        public string SelectedGrouping
        {
            get => _selectedGrouping;
            set { _selectedGrouping = value; ApplyFilter(); OnPropertyChanged(); }
        }

        /// <summary>
        /// Текст для фильтрации логов по сообщению.
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set { _filterText = value; ApplyFilter(); OnPropertyChanged(); }
        }

        /// <summary>
        /// Команда для открытия файла логов.
        /// </summary>
        public RelayCommand OpenFileCommand { get; }

        /// <summary>
        /// Команда для экспорта логов в CSV.
        /// </summary>
        public RelayCommand ExportCommand { get; }

        private List<LogEntry> _allLogs;

        /// <summary>
        /// Инициализирует новый экземпляр класса MainViewModel.
        /// </summary>
        public MainViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            ExportCommand = new RelayCommand(ExportLogs);
        }

        /// <summary>
        /// Открывает файл логов и загружает его содержимое.
        /// </summary>
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Выбрать файл логов"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _allLogs = ReadLogFile(openFileDialog.FileName);
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Экспортирует отфильтрованные логи в CSV-файл.
        /// </summary>
        private void ExportLogs()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Экспорт логов в CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExportToCsv(saveFileDialog.FileName);
                    MessageBox.Show("Логи успешно экспортированы!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте логов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Применяет фильтры к логам и обновляет коллекцию Logs.
        /// </summary>
        private void ApplyFilter()
        {
            if (_allLogs == null)
                return;

            var filteredLogs = _allLogs
                .Where(log => string.IsNullOrEmpty(FilterText) || log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                .Where(log => SelectedLevel == "Все" || log.Level.Equals(SelectedLevel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (SelectedGrouping == "По уровню")
            {
                filteredLogs = filteredLogs
                    .GroupBy(log => log.Level)
                    .SelectMany(group => group.Prepend(new LogEntry { Message = $"--- {group.Key} ---" }))
                    .ToList();
            }
            else if (SelectedGrouping == "По дате")
            {
                filteredLogs = filteredLogs
                    .GroupBy(log => log.Timestamp.Date)
                    .SelectMany(group => group.Prepend(new LogEntry { Message = $"--- {group.Key.ToShortDateString()} ---" }))
                    .ToList();
            }

            Logs.Clear();
            foreach (var log in filteredLogs)
                Logs.Add(log);
        }

        /// <summary>
        /// Читает файл логов и преобразует его в список записей LogEntry.
        /// </summary>
        /// <param name="filePath">Путь к файлу логов.</param>
        /// <returns>Список записей LogEntry.</returns>
        private List<LogEntry> ReadLogFile(string filePath)
        {
            var logs = new List<LogEntry>();
            using var reader = new StreamReader(filePath);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var entry = ParseLogLine(line);
                if (entry != null)
                    logs.Add(entry);
            }

            return logs;
        }

        /// <summary>
        /// Парсит строку лога и создаёт объект LogEntry.
        /// </summary>
        /// <param name="line">Строка лога.</param>
        /// <returns>Объект LogEntry или null, если строка не соответствует формату.</returns>
        private LogEntry ParseLogLine(string line)
        {
            var match = Regex.Match(line, @"\[(.*?)\]\s+(\w+):\s+(.*)");
            if (!match.Success)
                return null;

            return new LogEntry
            {
                Timestamp = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Level = match.Groups[2].Value,
                Message = match.Groups[3].Value
            };
        }

        /// <summary>
        /// Экспортирует логи в CSV-файл.
        /// </summary>
        /// <param name="filePath">Путь к файлу для экспорта.</param>
        private void ExportToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Дата/время,Уровень,Сообщение");

            foreach (var entry in Logs)
            {
                if (string.IsNullOrEmpty(entry.Level) && entry.Message.StartsWith("---"))
                    continue;

                writer.WriteLine($"{entry.Timestamp},{entry.Level},{entry.Message}");
            }
        }

        /// <summary>
        /// Событие изменения свойств.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывает событие PropertyChanged для указанного свойства.
        /// </summary>
        /// <param name="propertyName">Имя свойства.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}