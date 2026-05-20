using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentValidation;

namespace ControlMareas.App.ViewModels;

public abstract class ValidatableViewModelBase<T> : ObservableObject, INotifyDataErrorInfo where T : class
{
    private readonly IValidator<T> _validator;
    private readonly ConcurrentDictionary<string, List<string>> _errors = new();

    protected ValidatableViewModelBase(IValidator<T> validator)
    {
        _validator = validator;
    }

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.Values.SelectMany(x => x);

        return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
    }

    protected void ValidatePropertyWithFluent(object? value, string propertyName)
    {
        var context = new ValidationContext<T>((T)(object)this);
        var result = _validator.Validate(context);

        _errors.TryRemove(propertyName, out _);
        
        var propertyErrors = result.Errors
            .Where(e => e.PropertyName == propertyName)
            .Select(e => e.ErrorMessage)
            .ToList();

        if (propertyErrors.Any())
        {
            _errors.TryAdd(propertyName, propertyErrors);
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    public bool ValidateAll()
    {
        var context = new ValidationContext<T>((T)(object)this);
        var result = _validator.Validate(context);

        _errors.Clear();

        var groupedErrors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList());

        foreach (var entry in groupedErrors)
        {
            _errors.TryAdd(entry.Key, entry.Value);
        }

        // Notify all properties changed to refresh errors
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
        OnPropertyChanged(nameof(HasErrors));

        return !HasErrors;
    }
}
