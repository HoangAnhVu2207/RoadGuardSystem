using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Authentication;

public sealed class GmailIdentityMessageSender : IIdentityMessageSender
{
    private readonly IdentityOnboardingOptions _options;

    public GmailIdentityMessageSender(IOptions<IdentityOnboardingOptions> options)
    {
        _options = options.Value;
    }

    public Task<bool> SendReporterOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            $"[{_options.BrandName}] Mã xác minh tài khoản Reporter",
            $"<p><img src=\"cid:hoanghai-logo\" alt=\"Hoàng Hải\" style=\"max-width:180px\"></p><p>Chào bạn,</p><p>Bạn đang thực hiện đăng ký tài khoản Reporter trên hệ thống {_options.BrandName}.</p><p>Đây là mã xác minh (OTP) của bạn:</p><p style=\"font-size:32px;font-weight:700;letter-spacing:8px\">{WebUtility.HtmlEncode(otp)}</p><p>Mã này có hiệu lực trong vòng <strong>{_options.OtpLifetimeMinutes} phút</strong> và chỉ sử dụng được 1 lần. Tuyệt đối không chia sẻ mã này cho bất kỳ ai.</p>",
            $"Chào bạn,\n\nMã xác minh tài khoản Reporter trên hệ thống {_options.BrandName}: {otp}\n\nMã có hiệu lực trong {_options.OtpLifetimeMinutes} phút và chỉ sử dụng một lần.",
            cancellationToken);

    public Task<bool> SendInvitationAsync(
        string email,
        string displayName,
        string roleName,
        string invitationToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
        {
            return Task.FromResult(false);
        }

        var setupUrl = $"{_options.FrontendBaseUrl.TrimEnd('/')}/auth/setup-password?token={Uri.EscapeDataString(invitationToken)}";
        var safeName = WebUtility.HtmlEncode(displayName);
        var safeRole = WebUtility.HtmlEncode(roleName);
        var safeUrl = WebUtility.HtmlEncode(setupUrl);
        return SendAsync(
            email,
            $"Lời mời tham gia {_options.BrandName}",
            $"<p><img src=\"cid:hoanghai-logo\" alt=\"Hoàng Hải\" style=\"max-width:180px\"></p><p>Chào <strong>{safeName}</strong>,</p><p>Bạn đã được Supervisor mời tham gia hệ thống quản lý bảo hành {_options.BrandName} với vai trò <strong>{safeRole}</strong>.</p><p>Vui lòng nhấn vào nút bên dưới để thiết lập mật khẩu và kích hoạt tài khoản của bạn. Link này có hiệu lực trong vòng 72 giờ.</p><p><a href=\"{safeUrl}\" style=\"display:inline-block;padding:12px 20px;background:#163b70;color:#fff;text-decoration:none;border-radius:4px\">Thiết lập mật khẩu</a></p><p>Nếu bạn không mong đợi email này, vui lòng bỏ qua.</p>",
            $"Chào {displayName},\n\nBạn đã được Supervisor mời tham gia hệ thống {_options.BrandName} với vai trò {roleName}.\n\nThiết lập mật khẩu: {setupUrl}\nLink có hiệu lực đến {expiresAt:yyyy-MM-dd HH:mm} UTC.\n\nNếu bạn không mong đợi email này, vui lòng bỏ qua.",
            cancellationToken);
    }

    private async Task<bool> SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpUsername) ||
            string.IsNullOrWhiteSpace(_options.SmtpPassword) ||
            string.IsNullOrWhiteSpace(recipient))
        {
            return false;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SmtpUsername, _options.SenderDisplayName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = textBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipient));
        var html = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
        var logoPath = ResolveLogoPath();
        if (File.Exists(logoPath))
        {
            var logo = new LinkedResource(logoPath, "image/png") { ContentId = "hoanghai-logo" };
            html.LinkedResources.Add(logo);
        }

        message.AlternateViews.Add(html);
        using var smtp = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword)
        };
        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (SmtpException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string ResolveLogoPath() =>
        Path.IsPathRooted(_options.LogoPath)
            ? _options.LogoPath
            : Path.Combine(AppContext.BaseDirectory, _options.LogoPath);
}
