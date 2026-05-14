using FluentValidation;

namespace OBSArrastre2026.App.ViewModels;

public sealed class ProduccionEditViewModelValidator : AbstractValidator<ProduccionEditViewModel>
{
    public ProduccionEditViewModelValidator()
    {
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria.");

        RuleFor(x => x.SelectedProducto)
            .NotNull().WithMessage("Debe seleccionar un producto.");

        RuleFor(x => x.SelectedEtapa)
            .NotNull().WithMessage("Debe seleccionar una etapa de marea.");

        RuleFor(x => x.Kg)
            .NotEmpty().WithMessage("Los kilogramos son obligatorios.")
            .GreaterThan(0).WithMessage("Los kilogramos deben ser mayores a 0.");
            
        RuleFor(x => x.Factor)
            .GreaterThanOrEqualTo(0).When(x => x.Factor.HasValue).WithMessage("El factor debe ser mayor o igual a 0.");
    }
}
