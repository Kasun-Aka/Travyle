using Travyle.Api.DTOs;

namespace Travyle.Api.Services;

public interface ISupportService
{
    Task<TicketResponseDto> CreateTicketAsync(CreateTicketDto dto, CancellationToken cancellationToken = default);
    Task<PaginatedListDto<TicketResponseDto>> GetTicketsAsync(TicketListQueryDto query, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> GetTicketByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> UpdateTicketAsync(Guid id, UpdateTicketDto dto, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> UpdateTicketStatusAsync(Guid id, UpdateTicketStatusDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteTicketAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AutoResolveResultDto?> AutoResolveClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> UploadAttachmentAsync(IFormFile file, HttpRequest request, CancellationToken cancellationToken = default);

    Task<VoucherResponseDto> IssueVoucherAsync(CreateVoucherDto dto, CancellationToken cancellationToken = default);
    Task<VoucherResponseDto?> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VoucherResponseDto?> ApproveVoucherAsync(Guid voucherId, ApproveVoucherDto dto, CancellationToken cancellationToken = default);
    Task<VoucherResponseDto?> RejectVoucherAsync(Guid voucherId, RejectVoucherDto dto, CancellationToken cancellationToken = default);
    Task<List<VoucherResponseDto>> GetAllVouchersAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<List<VoucherResponseDto>> GetVouchersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteVoucherAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ReviewResponseDto>> GetReviewsByTourIdAsync(Guid? tourId = null, CancellationToken cancellationToken = default);
    Task<ReviewResponseDto> CreateReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default);
    Task<ReviewResponseDto?> ToggleReviewVerificationAsync(Guid id, bool isVerified, CancellationToken cancellationToken = default);
    Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SupportAnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<UserSupportActivityDto> GetUserActivityAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> CancelTicketAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
