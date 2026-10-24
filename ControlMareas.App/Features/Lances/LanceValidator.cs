using FluentValidation;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Features.Lances;

public sealed class LanceValidator : AbstractValidator<LanceEditViewModel>
{
    public LanceValidator()
    {
        RuleFor(x => x.NroLance).GreaterThan(0).WithMessage("El número de lance debe ser mayor a 0");
        RuleFor(x => x.Fecha).NotEmpty().WithMessage("La fecha es requerida");

        When(x => !x.EsPalangre, () =>
        {
            RuleFor(x => x.ProfundidadInicioM)
                .InclusiveBetween(0, 2000)
                .When(x => x.ProfundidadInicioM.HasValue)
                .WithMessage("La profundidad inicial debe estar entre 0 y 2000 m");

            RuleFor(x => x.ProfundidadFinalM)
                .InclusiveBetween(0, 2000)
                .When(x => x.ProfundidadFinalM.HasValue)
                .WithMessage("La profundidad final debe estar entre 0 y 2000 m");
        });

        When(x => x.EsPalangre, () =>
        {
            RuleFor(x => x.ProfundidadInicioFiladoM)
                .InclusiveBetween(50, 5000)
                .When(x => x.ProfundidadInicioFiladoM.HasValue)
                .WithMessage("La profundidad inicial de filado debe estar entre 50 y 5000 m");

            RuleFor(x => x.ProfundidadFinFiladoM)
                .InclusiveBetween(50, 5000)
                .When(x => x.ProfundidadFinFiladoM.HasValue)
                .WithMessage("La profundidad final de filado debe estar entre 50 y 5000 m");

            RuleFor(x => x.ProfundidadInicioViradoM)
                .InclusiveBetween(50, 5000)
                .When(x => x.ProfundidadInicioViradoM.HasValue)
                .WithMessage("La profundidad inicial de virado debe estar entre 50 y 5000 m");

            RuleFor(x => x.ProfundidadFinViradoM)
                .InclusiveBetween(50, 5000)
                .When(x => x.ProfundidadFinViradoM.HasValue)
                .WithMessage("La profundidad final de virado debe estar entre 50 y 5000 m");
        });
    }
}
