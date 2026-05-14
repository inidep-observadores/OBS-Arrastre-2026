using FluentValidation;

namespace OBSArrastre2026.App.ViewModels;

public sealed class SubmuestraEditViewModelValidator : AbstractValidator<SubmuestraEditViewModel>
{
    public SubmuestraEditViewModelValidator()
    {
        RuleForEach(x => x.Submuestras).ChildRules(item =>
        {
            item.RuleFor(x => x.ReplecionGastrica)
                .InclusiveBetween(0, 4)
                .WithMessage("La repleción gástrica debe estar entre 0 y 4.");
        });
    }
}
