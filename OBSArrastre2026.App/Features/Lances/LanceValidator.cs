using FluentValidation;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Features.Lances;

public sealed class LanceValidator : AbstractValidator<LanceEditViewModel>
{
    public LanceValidator()
    {
        RuleFor(x => x.NroLance).GreaterThan(0).WithMessage("El número de lance debe ser mayor a 0");
        RuleFor(x => x.Fecha).NotEmpty().WithMessage("La fecha es requerida");
        
        // Se pueden añadir más reglas según requerimientos técnicos
    }
}
