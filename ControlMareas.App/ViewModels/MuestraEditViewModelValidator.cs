using FluentValidation;

namespace ControlMareas.App.ViewModels;

public sealed class MuestraEditViewModelValidator : AbstractValidator<MuestraEditViewModel>
{
    public MuestraEditViewModelValidator()
    {
        RuleFor(x => x.EspecieId)
            .NotEmpty().WithMessage("La especie es obligatoria.");

        RuleFor(x => x.PesoMuestraKg)
            .GreaterThan(0).When(x => x.PesoMuestraKg.HasValue)
            .WithMessage("El peso debe ser mayor a 0.");
    }
}
