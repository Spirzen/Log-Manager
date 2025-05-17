using System;
using System.Windows.Input;

namespace LogManager.ViewModels
{
    /// <summary>
    /// Реализует интерфейс ICommand для выполнения команд в MVVM-приложении.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Инициализирует новый экземпляр класса RelayCommand.
        /// </summary>
        /// <param name="execute">Действие, которое будет выполнено при вызове команды.</param>
        /// <param name="canExecute">Функция, определяющая, может ли команда быть выполнена.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда быть выполнена в текущем состоянии.
        /// </summary>
        /// <param name="parameter">Параметр команды (не используется).</param>
        /// <returns>True, если команда может быть выполнена; иначе False.</returns>
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        /// <summary>
        /// Выполняет команду.
        /// </summary>
        /// <param name="parameter">Параметр команды (не используется).</param>
        public void Execute(object parameter) => _execute();

        /// <summary>
        /// Событие, возникающее при изменении состояния возможности выполнения команды.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}