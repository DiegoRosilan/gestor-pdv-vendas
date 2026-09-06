using System.Windows.Input;

namespace GestorPDV.App.ViewModels;

/// <summary>
/// ICommand genérico de MVVM — sem framework externo (Prism/CommunityToolkit),
/// só o suficiente pra ligar botão a ação. Fábricas nomeadas (Create/CreateAsync)
/// em vez de dois construtores sobrecarregados de propósito: um lambda que
/// chama um método "async Task" (`_ => MinhaAcaoAsync()`) é convertível tanto
/// pra Action&lt;object?&gt; (descartando a Task) quanto pra Func&lt;object?,Task&gt; —
/// com dois construtores aceitando cada um, toda chamada com um método async
/// ficaria ambígua (erro de compilação). Nome do método resolve isso sem ambiguidade.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _executando;

    private RelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute)
    {
        _execute = executeAsync;
        _canExecute = canExecute;
    }

    public static RelayCommand Create(Action<object?> execute, Predicate<object?>? canExecute = null)
        => new(param => { execute(param); return Task.CompletedTask; }, canExecute);

    public static RelayCommand CreateAsync(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)
        => new(executeAsync, canExecute);

    public bool CanExecute(object? parameter) => !_executando && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        _executando = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            await _execute(parameter);
        }
        finally
        {
            _executando = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
