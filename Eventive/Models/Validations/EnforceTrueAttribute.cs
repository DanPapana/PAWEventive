﻿using System.ComponentModel.DataAnnotations;

namespace Eventive.Models.Validations
{
    public class EnforceTrueAttribute : ValidationAttribute
    {
        public EnforceTrueAttribute() { }

        public string GetErrorMessage() => $"More information required";

        protected override ValidationResult IsValid(object value,
        ValidationContext validationContext)
        {
            if (value is bool valueAsBool && valueAsBool)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(GetErrorMessage());
        }
    }
}
