using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Cognition.Api.Controllers;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Data.Relational.Modules.Common;
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
    public void DataSourceCreateRequest_ShouldFail_WhenNameWhitespace()
    {
        var model = new DataSourcesController.CreateRequest("   ", DataSourceType.JsonStore, null, null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(DataSourcesController.CreateRequest.Name)) == true);
    }

    [Fact]
    public void SystemVariableCreateRequest_ShouldFail_WhenKeyWhitespace()
    {
        var model = new SystemVariablesController.CreateRequest(" ", "type", null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(SystemVariablesController.CreateRequest.Key)) == true);
    }

    [Fact]
    public void CreateToolRequest_ShouldFail_WhenNameAndClassPathInvalid()
    {
        var model = new ToolsController.CreateToolRequest(" ", " ", null, Array.Empty<string>(), null, null, true, Guid.Empty);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(ToolsController.CreateToolRequest.Name)) == true);
        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(ToolsController.CreateToolRequest.ClassPath)) == true);
        Assert.Contains(results, r => r.ErrorMessage == "ClientProfileId must not be empty.");
    }

    [Fact]
    public void CreateParamRequest_ShouldFail_WhenFieldsInvalid()
    {
        var model = new ToolParametersController.CreateParamRequest(Guid.Empty, " ", " ", ToolParamDirection.Input, false, null, null, null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "ToolId must not be empty.");
        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(ToolParametersController.CreateParamRequest.Name)) == true);
        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(ToolParametersController.CreateParamRequest.Type)) == true);
    }

    [Fact]
    public void CreateSupportRequest_ShouldFail_WhenIdsInvalid()
    {
        var model = new ToolProviderSupportsController.CreateSupportRequest(Guid.Empty, Guid.Empty, Guid.Empty, SupportLevel.Full, null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "ToolId must not be empty.");
        Assert.Contains(results, r => r.ErrorMessage == "ProviderId must not be empty.");
        Assert.Contains(results, r => r.ErrorMessage == "ModelId must not be empty.");
    }

    [Fact]
    public void PersonaCreateRequest_ShouldFail_WhenNameWhitespace()
    {
        var model = new PersonasController.PersonaCreateRequest("  ", null, null, null, null, null, null, null, null, null, null, null, null, null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(PersonasController.PersonaCreateRequest.Name)) == true);
    }

    [Fact]
    public void GrantAccessRequest_ShouldFail_WhenUserIdEmpty()
    {
        var model = new PersonasController.GrantAccessRequest(Guid.Empty, false, "label");
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "UserId must not be empty.");
    }

    [Fact]
    public void PersonaUpdateRequest_ShouldFail_WhenNameWhitespace()
    {
        var model = new PersonasController.PersonaUpdateRequest(" ", null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(PersonasController.PersonaUpdateRequest.Name)) == true);
    }

    [Fact]
    public void AddMessageRequest_ShouldFail_WhenContentInvalidOrIdsEmpty()
    {
        var model = new ConversationsController.AddMessageRequest(Guid.Empty, Guid.Empty, ChatRole.Assistant, " ", null);
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "FromPersonaId must not be empty.");
        Assert.Contains(results, r => r.ErrorMessage == "ToPersonaId must not be empty.");
        Assert.Contains(results, r => r.ErrorMessage?.Contains("Content") == true);
    }

    [Fact]
    public void AddVersionRequest_ShouldFail_WhenContentWhitespace()
    {
        var model = new ConversationsController.AddVersionRequest(" ");
        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage?.Contains(nameof(ConversationsController.AddVersionRequest.Content)) == true);
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

