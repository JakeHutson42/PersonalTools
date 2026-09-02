using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PersonalTools.Classes;
using PersonalTools.Classes.CaseOpening;
using PersonalTools.Entities;
using PersonalTools.Data.CaseOpening;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[ApiController, Route("api/case-tycoon/guest")]
public sealed class CaseTycoonGuestController(IAuthFuncs auth, ICaseOpeningFuncs caseOpening, ICaseOpeningData caseOpeningData, ILogger<CaseTycoonGuestController> logger) : ControllerBase
{
    [AllowAnonymous, EnableRateLimiting("guest-registration"), HttpPost]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CaseTycoonGuestRequest? request)
    {
        if (User.Identity?.IsAuthenticated == true) return Conflict(new ApiResponse(false,"An account is already active in this browser."));
        try
        {
            if (!await caseOpeningData.GetGuestAccessEnabled(HttpContext.RequestAborted))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(false,"Guest access is currently unavailable."));
            (AppUser user, AuthSession session) = await auth.CreateGuest(request?.Username ?? string.Empty, Request.Headers.UserAgent.ToString());
            Claim[] claims =
            [
                new(ClaimTypes.NameIdentifier,user.UserId.ToString("D")),new(ClaimTypes.Name,user.DisplayName),
                new(ClaimTypes.Role,user.Role.ToString()),new(AppAuthorizationPolicies.AccountTypeClaim,AppAuthorizationPolicies.GuestAccount),
                new("session_id",session.SessionId.ToString("D"))
            ];
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties { IsPersistent=true,ExpiresUtc=session.ExpiresUtc,AllowRefresh=false });
            await caseOpening.RecordCaseOpeningLogin(user.UserId);
            Response.Headers.CacheControl="no-store";
            return Ok(new ApiResponse(true,"Your Case Tycoon account is ready."));
        }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse(false,exception.Message)); }
        catch (MySqlConnector.MySqlException exception) when (exception.Number == 1644 && exception.Message.Contains("Guest access", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(false,"Guest access is currently unavailable."));
        }
        catch (Exception exception) { logger.LogError(exception,"Guest Case Tycoon registration failed from {RemoteIp}.",HttpContext.Connection.RemoteIpAddress); return StatusCode(500,new ApiResponse(false,"The Case Tycoon account could not be created.")); }
    }
}

public sealed record CaseTycoonGuestRequest(string Username);
