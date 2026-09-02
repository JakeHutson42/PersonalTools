using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Entities;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[Authorize(Policy = AppAuthorizationPolicies.CaseTycoonAccess)]
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private static readonly HashSet<AppSettingKey> GuestSettingKeys =
    [
        AppSettingKey.AppearanceTheme,
        AppSettingKey.AppearanceMode,
        AppSettingKey.CaseProfileEmoji,
    ];

    private readonly IAppSettingsFuncs _settings;
    public SettingsController(IAppSettingsFuncs settings) => _settings = settings;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsGuest => User.HasClaim(AppAuthorizationPolicies.AccountTypeClaim, AppAuthorizationPolicies.GuestAccount);

    [HttpGet]
    public async Task<ActionResult<List<AppSettingView>>> Get(CancellationToken cancellationToken)
    {
        List<AppSettingView> settings = await _settings.Get(UserId, cancellationToken);
        return Ok(IsGuest ? settings.Where(setting => GuestSettingKeys.Contains(setting.Definition.Key)).ToList() : settings);
    }
    [HttpPut]
    public async Task<ActionResult<ApiResponse>> Set([FromBody] AppSettingRequest request, CancellationToken cancellationToken)
    {
        if (IsGuest && !GuestSettingKeys.Contains(request.Key)) return Forbid();

        // Appearance and dashboard preferences remain per-user controls even though the full
        // Settings page is administrator-only. Server secrets are never user-editable.
        if ((request.Key == AppSettingKey.SteamWebApiKey || request.Key == AppSettingKey.CSFloatApiKey) && !User.IsUserAllowedHere(AppRole.Admin))
        {
            return Forbid();
        }

        try { await _settings.Set(UserId, request.Key, request.Value, cancellationToken); return Ok(new ApiResponse(true, "Setting saved.")); }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse(false, exception.Message)); }
    }
}
public sealed record AppSettingRequest(AppSettingKey Key, string? Value);
