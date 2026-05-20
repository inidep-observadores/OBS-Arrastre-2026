using FluentValidation;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Features.Mareas;

public sealed class MareaValidator : AbstractValidator<MareaEditViewModel>
{
    public MareaValidator()
    {
        RuleFor(x => x.AnioInidep)
            .InclusiveBetween(2000, 2100).WithMessage("El año debe ser válido (2000-2100)");

        RuleFor(x => x.NumeroInidep)
            .GreaterThan(0).WithMessage("El número de marea debe ser mayor a 0");

        RuleFor(x => x.FechaInicio)
            .NotEmpty().WithMessage("La fecha de inicio es requerida");

        RuleFor(x => x.FechaFin)
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .When(x => x.FechaFin.HasValue)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio");

        RuleFor(x => x.SelectedBuque)
            .NotNull().WithMessage("Debe seleccionar un buque");
    }
}
