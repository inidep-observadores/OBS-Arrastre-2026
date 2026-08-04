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

        When(x => x.EsLangostino, () =>
        {
            RuleForEach(x => x.FrecuenciasTallas).ChildRules(f =>
            {
                f.RuleFor(x => x.NroLangostinosMachoMaduros)
                    .Must((item, machosMaduros) => machosMaduros <= item.NroMachos)
                    .WithMessage(item => $"El Nro de machos maduros ({item.NroLangostinosMachoMaduros}) no debe superar el total de machos ({item.NroMachos}) para la talla {item.Talla}.");

                f.RuleFor(x => x)
                    .Must(item => (item.NroLangostinosHembraMaduras + item.NroLangostinosHembraImpregnadas) <= item.NroHembras)
                    .WithMessage(item => $"La suma de hembras maduras e impregnadas ({item.NroLangostinosHembraMaduras + item.NroLangostinosHembraImpregnadas}) no debe superar el total de hembras ({item.NroHembras}) para la talla {item.Talla}.");
            });
        });
    }
}
