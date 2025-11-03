using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Cognition.Api.Controllers;
using Cognition.Api.Infrastructure.Validation;
using Xunit;

namespace Cognition.Api.Tests.Validation;

public class DataAnnotationTests
{
    [Fact]
    public void RegisterRequest_ShouldFail_WhenFieldsInvalid()
    {
        var model = new UsersController.RegisterRequest("", "short", "bad-email");
        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UsersController.RegisterRequest.Username)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UsersController.RegisterRequest.Password)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UsersController.RegisterRequest.Email)));
    }

    [Fact]
    public void NotEmptyGuidAttribute_ShouldRejectEmptyGuid()
    {
        var attribute = new NotEmptyGuidAttribute();
        var context = new ValidationContext(new object(), null, null) { MemberName = "AgentId" };

        var result = attribute.GetValidationResult(Guid.Empty, context);

        Assert.NotEqual(ValidationResult.Success, result);
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}

