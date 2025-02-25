using System.Windows.Input;

namespace EasySave.Monitoring.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is null)
            {
                return _canExecute == null;
            }
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T validParameter)
            {
                _execute(validParameter);
            }
            else
            {
                throw new ArgumentException($"Invalid parameter type. Expected {typeof(T)}, but got {parameter?.GetType()}.", nameof(parameter));
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}