using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WanderLost.Server.Data;

namespace WanderLost.Server.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<WanderlostUser> _signInManager;
    private readonly UserManager<WanderlostUser> _userManager;
    private readonly IUserStore<WanderlostUser> _userStore;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<WanderlostUser> signInManager,
        UserManager<WanderlostUser> userManager,
        IUserStore<WanderlostUser> userStore,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _logger = logger;
    }

    const string LOGIN_PROVIDER = "Discord";

    public IActionResult OnGet(string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(LOGIN_PROVIDER, redirectUrl);
        return new ChallengeResult(LOGIN_PROVIDER, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (remoteError != null)
        {
            return GetErrorRedirect($"Error from external provider: {remoteError}");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return GetErrorRedirect("Error loading external login information.");
        }

        var loginResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (loginResult.Succeeded)
        {
            //User already has account
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);

            //Check for updated discord ID/discriminator (discriminators may now be zero under new user system)
            var existingUser = await _signInManager.UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            var updatedName = BuildUserNameFromPrincipal(info.Principal);
            if (existingUser is not null && !string.IsNullOrWhiteSpace(updatedName) && !string.Equals(existingUser.UserName, updatedName))
            {
                _logger.LogInformation("Updating name of user {UserId}.", existingUser.Id);
                await _userStore.SetUserNameAsync(existingUser, updatedName, CancellationToken.None);
                await _userManager.UpdateAsync(existingUser);
            }

            return LocalRedirect(returnUrl);
        }

        // If the user does not have an account, automatically create an account
        var verifiedClaim = info.Principal.FindFirst("verified");
        if (verifiedClaim is null || !bool.TryParse(verifiedClaim.Value, out var verified) || !verified)
        {
            return GetErrorRedirect("Discord login requires an account with a Discord account with a verified e-mail.");
        }

        if (string.IsNullOrWhiteSpace(info.Principal.Identity?.Name))
        {
            _logger.LogError("Missing claims from Discord.");
            return GetErrorRedirect("Missing discord claims.");
        }

        var user = Activator.CreateInstance<WanderlostUser>();

        var username = BuildUserNameFromPrincipal(info.Principal);
        await _userStore.SetUserNameAsync(user, username, CancellationToken.None);

        var createUserResult = await _userManager.CreateAsync(user);
        if (createUserResult.Succeeded)
        {
            createUserResult = await _userManager.AddLoginAsync(user, info);
            if (createUserResult.Succeeded)
            {
                _logger.LogInformation("User {userId} an account using {Name} provider.", user.Id, info.LoginProvider);

                await _signInManager.SignInAsync(user, isPersistent: true, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            else
            {
                await _userManager.DeleteAsync(user);
            }
        }

        _logger.LogError("Failed to create user. {reason}", createUserResult.Errors.FirstOrDefault()?.Description);
        return GetErrorRedirect(createUserResult.Errors.FirstOrDefault()?.Description ?? "Error creating user.");
    }

    private IActionResult GetErrorRedirect(string message)
    {
        return Redirect(new PathString($"/ErrorMessage/{message}"));
    }

    private string BuildUserNameFromPrincipal(ClaimsPrincipal principal)
    {
        var discriminator = principal.FindFirst(DiscordAuthenticationConstants.Claims.Discriminator);
        if (discriminator is not null && !string.Equals(discriminator.Value, "0"))
        {
            return $"{principal.Identity?.Name}#{discriminator.Value}";
        }
        return principal.Identity?.Name ?? string.Empty;
    }
}
