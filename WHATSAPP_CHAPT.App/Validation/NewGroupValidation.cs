#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace WHATSAPP_CHAPT.App.Validation
{
    /// <summary>
    /// Validation rules for creating a new group.
    /// </summary>
    public static class NewGroupValidation
    {
        public static ValidationResult Validate(string title, IList<string> members)
        {
            if (string.IsNullOrWhiteSpace(title))
                return ValidationResult.Failure("Le titre du groupe ne doit pas être vide.");

            if (members == null || members.Count < 2)
                return ValidationResult.Failure("Un groupe doit contenir au moins deux membres.");

            if (members.Distinct().Count() != members.Count)
                return ValidationResult.Failure("Des membres en double ont été détectés.");

            return ValidationResult.Success();
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        private ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string msg) => new(false, msg);
    }
}
