﻿namespace Caliburn.FluentValidation
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using Core.Validation;
    using global::FluentValidation;
    using global::FluentValidation.Validators;
    using Microsoft.Practices.ServiceLocation;
    using IValidator = global::FluentValidation.IValidator;

    /// <summary>
    /// An implementation of <see cref="Core.Validation.IValidator"/> that uses FluentValidation.
    /// </summary>
    public class FluentValidationValidator : ValidatorFactoryBase, Core.Validation.IValidator
    {
        private readonly IServiceLocator _serviceLocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationValidator"/> class.
        /// </summary>
        /// <param name="serviceLocator">The service locator.</param>
        public FluentValidationValidator(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="validatorType">Type of the validator.</param>
        /// <returns></returns>
        public override IValidator CreateInstance(Type validatorType)
        {
            return _serviceLocator.GetInstance(validatorType) as IValidator;
        }

        /// <summary>
        /// Inidcates whether the specified property should be validated.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// true if should be validated; otherwise false
        /// </returns>
        public bool ShouldValidate(PropertyInfo property)
        {
            var validatorType = typeof(IValidator<>)
                .MakeGenericType(property.DeclaringType);

            var validator = GetValidator(validatorType);

            return validator != null && validator.CreateDescriptor().GetValidatorsForMember(property.Name).Any();
        }

        /// <summary>
        /// Validates the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The validation errors.</returns>
        public IEnumerable<IValidationError> Validate(object instance)
        {
            var validator = CreateValidatorForInstance(instance);
            if (validator == null)
                yield break;

            var result = validator.Validate(instance);

            foreach(var failure in result.Errors)
            {
                yield return new ValidationErrorAdapter(instance, failure);
            }
        }

        /// <summary>
        /// Validates the specified property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The validation errors.</returns>
        public IEnumerable<IValidationError> Validate(object instance, string propertyName)
        {
            var instanceValidator = CreateValidatorForInstance(instance);
            if(instanceValidator == null)
                yield break;

            var descriptor = instanceValidator.CreateDescriptor();
            var propertyValue = instance.GetType().GetProperty(propertyName).GetValue(instance, null);
            var validators = descriptor.GetValidatorsForMember(propertyName);
            var context = new PropertyValidatorContext(propertyName, instance, propertyValue, propertyName);

            foreach(var validator in validators)
            {
                var failures = validator.Validate(context);

                foreach(var failure in failures)
                {
                    yield return new ValidationErrorAdapter(instance, failure);
                }
            }
        }

        private IValidator CreateValidatorForInstance(object instance) 
        {
            if (instance == null)
                return null;

            var validatorType = typeof(IValidator<>)
                .MakeGenericType(instance.GetType());

            return GetValidator(validatorType);
        }
    }
}