using LoginAnomaly.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LoginAnomaly.Api.Controllers;

[ApiController]
[Route("api/simulate")]
public class SimulatorController : ControllerBase
{
    private readonly AttackSimulator _simulator;

    public SimulatorController(AttackSimulator simulator) => _simulator = simulator;

    // POST /api/simulate/{scenario}?username=test@mail.com
    [HttpPost("{scenario}")]
    public async Task<IActionResult> Run(string scenario, [FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Query 'username' wajib diisi." });

        var result = await _simulator.RunAsync(scenario, username);
        if (result.EventsGenerated == 0)
            return BadRequest(result);

        return Ok(result);
    }
}