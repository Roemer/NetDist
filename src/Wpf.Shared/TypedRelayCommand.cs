using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Wpf.Shared
{
    /// <summary>
    /// Class to easy create ICommands with casting the command parameter to the desired type
    /// Last updated: 15.01.2015
    /// </summary>
    public class TypedRelayCommand<T> : ICommand
    {
        private readonly Action<T> _methodToExecute;
        readonly Func<T, bool> _canExecuteEvaluator;

        public TypedRelayCommand(Action<T> methodToExecute)
            : this(methodToExecute, null) { }

        public TypedRelayCommand(Action<T> methodToExecute, Func<T, bool> canExecuteEvaluator)
        {
            _methodToExecute = methodToExecute;
            _canExecuteEvaluator = canExecuteEvaluator;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecuteEvaluator == null || _canExecuteEvaluator.Invoke((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _methodToExecute.Invoke((T)parameter);
        }
    }
}
