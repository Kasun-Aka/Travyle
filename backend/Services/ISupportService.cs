using Travyle.Api.DTOs;

namespace Travyle.Api.Services;

public interface ISupportService
{
    Task<TicketResponseDto> CreateTicketAsync(CreateTicketDto dto, CancellationToken cancellationToken = default);
    Task<PaginatedListDto<TicketResponseDto>> GetTicketsAsync(TicketListQueryDto query, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> GetTicketByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TicketResponseDto?> UpdateTicketStatusAsync(Guid id, UpdateTicketStatusDto dto, CancellationToken cancellationToken = default);
    Task<AutoResolveResultDto?> AutoResolveClaimAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VoucherResponseDto> IssueVoucherAsync(CreateVoucherDto dto, CancellationToken cancellationToken = default);
    Task<VoucherResponseDto?> ApproveVoucherAsync(Guid voucherId, ApproveVoucherDto dto, CancellationToken cancellationToken = default);
    Task<List<VoucherResponseDto>> GetAllVouchersAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<List<VoucherResponseDto>> GetVouchersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<ReviewResponseDto>> GetReviewsByTourIdAsync(Guid tourId, CancellationToken cancellationToken = default);
    Task<ReviewResponseDto> CreateReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default);
}
