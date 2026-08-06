using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;

namespace ControlMareas.App.ViewModels;

public class RecalcularPesosViewModel : ObservableObject
{
    private readonly IMuestraService _muestraService;
    private readonly Marea _marea;
    private CancellationTokenSource? _cts;

    private bool _isBusy;
    private double _progressValue;
    private string _progressMessage = "Preparando...";
    private bool _isCompleted;
    private string _completionMessage = "";

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public string CompletionMessage
    {
        get => _completionMessage;
        set => SetProperty(ref _completionMessage, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand CancelCommand { get; }
    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public RecalcularPesosViewModel(Marea marea, IMuestraService muestraService)
    {
        _marea = marea;
        _muestraService = muestraService;

        CloseCommand = new RelayCommand(() => DialogResult.TrySetResult(true));
        CancelCommand = new RelayCommand(CancelOperation, () => IsBusy);
    }

    public async Task StartProcessAsync()
    {
        IsBusy = true;
        _cts = new CancellationTokenSource();
        IsCompleted = false;

        var progress = new Progress<(int Current, int Total, string Message)>(UpdateProgress);

        try
        {
            await _muestraService.RecalcularPesosMuestrasAsync(_marea.ID, progress, _cts.Token);
            IsBusy = false;
            IsCompleted = true;
            CompletionMessage = "Recálculo de pesos finalizado con éxito.";
        }
        catch (OperationCanceledException)
        {
            IsBusy = false;
            IsCompleted = true;
            CompletionMessage = "Operación cancelada por el usuario.";
        }
        catch (Exception ex)
        {
            IsBusy = false;
            IsCompleted = true;
            CompletionMessage = $"Error: {ex.Message}";
        }
        finally
        {
            (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void UpdateProgress((int Current, int Total, string Message) info)
    {
        if (info.Total > 0)
        {
            ProgressValue = ((double)info.Current / info.Total) * 100;
        }
        ProgressMessage = info.Message;
    }

    private void CancelOperation()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }
}
