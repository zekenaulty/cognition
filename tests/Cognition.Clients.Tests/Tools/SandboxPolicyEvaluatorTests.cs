using System;
using System.Collections.Generic;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Sandbox;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class SandboxPolicyEvaluatorTests
{
    private static ToolContext CreateContext() =>
        new ToolContext(AgentId: Guid.NewGuid(), ConversationId: Guid.NewGuid(), PersonaId: Guid.NewGuid(), Services: new ServiceCollection().BuildServiceProvider(), Ct: default);

    [Fact]
    public void Evaluate_allows_when_disabled()
    {
        var options = Options.Create(new ToolSandboxOptions { Mode = SandboxMode.Disabled });
        var evaluator = new SandboxPolicyEvaluator(new OptionsMonitorStub<ToolSandboxOptions>(options.Value), NullLogger<SandboxPolicyEvaluator>.Instance);
        var decision = evaluator.Evaluate(new Tool { Id = Guid.NewGuid(), ClassPath = "Test.Tool" }, CreateContext());

        decision.IsAllowed.Should().BeTrue();
        decision.AuditOnly.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_denies_when_enforce_and_not_allowlisted()
    {
        var options = Options.Create(new ToolSandboxOptions { Mode = SandboxMode.Enforce });
        var evaluator = new SandboxPolicyEvaluator(new OptionsMonitorStub<ToolSandboxOptions>(options.Value), NullLogger<SandboxPolicyEvaluator>.Instance);
        var decision = evaluator.Evaluate(new Tool { Id = Guid.NewGuid(), ClassPath = "Test.Tool" }, CreateContext());

        decision.IsAllowed.Should().BeFalse();
        decision.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Evaluate_allows_allowlisted_classpath()
    {
        var classPath = "Test.Tool.Allow";
        var options = Options.Create(new ToolSandboxOptions
        {
            Mode = SandboxMode.Enforce,
            AllowedUnsafeClassPaths = new[] { classPath }
        });
        var evaluator = new SandboxPolicyEvaluator(new OptionsMonitorStub<ToolSandboxOptions>(options.Value), NullLogger<SandboxPolicyEvaluator>.Instance);
        var decision = evaluator.Evaluate(new Tool { Id = Guid.NewGuid(), ClassPath = classPath }, CreateContext());

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_sets_audit_only_in_audit_mode()
    {
        var options = Options.Create(new ToolSandboxOptions { Mode = SandboxMode.Audit });
        var evaluator = new SandboxPolicyEvaluator(new OptionsMonitorStub<ToolSandboxOptions>(options.Value), NullLogger<SandboxPolicyEvaluator>.Instance);
        var decision = evaluator.Evaluate(new Tool { Id = Guid.NewGuid(), ClassPath = "Test.Tool" }, CreateContext());

        decision.IsAllowed.Should().BeTrue();
        decision.AuditOnly.Should().BeTrue();
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public OptionsMonitorStub(T value) => _value = value;

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
