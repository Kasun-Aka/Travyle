namespace Travyle.Api.Services;

public interface INotificationService
{
    Task<bool> SendVoucherIssuedNotificationAsync(string recipientEmail, string customerName, string voucherCode, decimal amount, string reason);
    Task<bool> SendTicketStatusUpdateNotificationAsync(string recipientEmail, string customerName, string ticketTitle, string status, string? notes);
    Task<bool> SendCustomerReplyNotificationAsync(string recipientEmail, string customerName, string ticketTitle, string replyMessage);
    Task<bool> SendSmsAlertAsync(string phoneNumber, string message);
}
