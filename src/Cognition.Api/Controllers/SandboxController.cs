using Cognition.Api.Infrastructure.Security;
using Cognition.Clients.Tools.Sandbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/sandbox")]
public sealed class SandboxController : ControllerBase
{
    private readonly IToolSandboxApprovalQueue _queue;

    public SandboxController(IToolSandboxApprovalQueue queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    [HttpGet("queue")]
    public ActionResult<IReadOnlyCollection<ToolSandboxWorkRequest>> GetQueue()
    {
        return Ok(_queue.Snapshot());
    }

    [HttpPost("approve")]
    public ActionResult ApproveNext()
    {
        if (_queue.TryDequeue(out var request) && request is not null)
        {
            return Ok(new { approved = true, request.ToolId, request.ClassPath });
        }

        return NotFound(new { approved = false, message = "No pending sandbox requests." });
    }
}
