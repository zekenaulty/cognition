using System;
using System.ComponentModel.DataAnnotations;

namespace Cognition.Api.Infrastructure.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is Guid guid && guid == Guid.Empty)
        {
            var message = ErrorMessage ?? $"{validationContext.MemberName} must not be empty.";
            return new ValidationResult(message);
        }

        return ValidationResult.Success;
    }
}
