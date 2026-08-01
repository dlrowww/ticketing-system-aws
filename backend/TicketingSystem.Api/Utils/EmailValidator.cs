using System.Net.Mail;

namespace TicketingSystem.Api.Utils;

public static class EmailValidator
{
    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
