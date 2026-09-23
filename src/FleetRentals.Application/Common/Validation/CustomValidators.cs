using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FluentValidation;

namespace FleetRentals.Application.Common.Validation;

public static class CustomValidators
{
    public static IRuleBuilderOptionsConditions<T, TElement> MustBeValueObject<T, TElement, TValueObject>(
        this IRuleBuilder<T, TElement> ruleBuilder,
        Func<TElement, Result<TValueObject, Error>> factoryMethod)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = factoryMethod(value);

            if (result.IsFailure)
                context.AddFailure(result.Error.Serialize());
        });
    }

    public static IRuleBuilderOptionsConditions<T, TElement> MustBeValueObject<T, TElement, TValueObject>(
        this IRuleBuilder<T, TElement> ruleBuilder,
        Func<TElement, Result<TValueObject, Error>> factoryMethod,
        Action<T, TValueObject> assignToCommand)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var instance = (T)context.InstanceToValidate;
            var result = factoryMethod(value);

            if (result.IsSuccess)
                assignToCommand(instance, result.Value);
            else
                context.AddFailure(result.Error.Serialize());
        });
    }

    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, Error error)
    {
        return rule.WithMessage(error.Serialize());
    }

    public static IRuleBuilderOptionsConditions<T, IEnumerable<TElement>>
        MapToValueObjects<T, TElement, TValueObject>(
            this IRuleBuilder<T, IEnumerable<TElement>> rule,
            Func<TElement, Result<TValueObject, Error>> factory,
            Action<T, IReadOnlyList<TValueObject>> assign,
            bool requireNotEmpty = true,
            bool ensureDistinct = true,
            string? displayName = null)
    {
        return rule.Custom((values, ctx) =>
        {
            var instance = (T)ctx.InstanceToValidate;
            var prop = displayName ?? ctx.PropertyPath ?? "Items";

            if (values is null)
            {
                ctx.AddFailure(prop, Errors.Validation.ValueIsRequired(prop).Serialize());
                return;
            }

            var src = values.ToList();
            if (requireNotEmpty && src.Count == 0)
            {
                ctx.AddFailure(prop, Errors.Validation.ValueIsRequired(prop).Serialize());
                return;
            }

            var result = new List<TValueObject>(src.Count);
            HashSet<TElement>? seen = ensureDistinct ? new HashSet<TElement>() : null;

            for (int i = 0; i < src.Count; i++)
            {
                var v = src[i];

                if (ensureDistinct && seen is not null && !seen.Add(v))
                {
                    ctx.AddFailure($"{prop}[{i}]", "Duplicate value");
                    continue;
                }

                var r = factory(v);
                if (r.IsFailure)
                    ctx.AddFailure($"{prop}[{i}]", r.Error.Serialize());
                else
                    result.Add(r.Value);
            }

            if (result.Count == src.Count)
                assign(instance, result);
        });
    }
}