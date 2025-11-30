using Cognition.Data.Relational.Modules.Tools;

namespace Cognition.Clients.Tools.Sandbox;

public interface ISandboxPolicyEvaluator
{
    SandboxDecision Evaluate(Tool tool, ToolContext context);
}
