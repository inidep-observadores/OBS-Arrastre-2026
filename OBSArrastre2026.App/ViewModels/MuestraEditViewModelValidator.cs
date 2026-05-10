using FluentValidation;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MuestraEditViewModelValidator : AbstractValidator<MuestraEditViewModel>
{
    public MuestraEditViewModelValidator()
    {
        RuleFor(x => x.EspecieId)
            .NotEmpty().WithMessage("La especie es obligatoria.");

        RuleFor(x => x.PesoMuestraGramos)
            .GreaterThan(0).When(x => x.PesoMuestraGramos.HasValue)
            .WithMessage("El peso debe ser mayor a 0.");
    }
}
