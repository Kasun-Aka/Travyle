using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;
using Travyle.Api.Services.Auth;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/support/vouchers")]
public class VouchersController : SupportControllerBase
{
    private readonly ISupportService _supportService;
    private readonly ILogger<VouchersController> _logger;

    public VouchersController(
        ISupportService supportService,
        IFirebaseIdentityService identityService,
        ILogger<VouchersController> logger) : base(identityService)
    {
        _supportService = supportService;
        _logger = logger;
    }

    /// <summary>
    /// Issue a goodwill discount voucher manually. Staff only.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VoucherResponseDto>> IssueVoucher([FromBody] CreateVoucherDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var voucher = await _supportService.IssueVoucherAsync(dto, cancellationToken);
            return Ok(voucher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing voucher");
            return StatusCode(500, new { message = "An error occurred while issuing voucher." });
        }
    }

    /// <summary>
    /// Human approval gate: an authorized Admin/Operator approves a drafted goodwill voucher — updates status to Active,
    /// notifies customer, and resolves linked ticket. The approver identity is taken from the verified token.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<VoucherResponseDto>> ApproveVoucher(Guid id, [FromBody] ApproveVoucherDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        dto.AdminId = user.Id;

        try
        {
            var voucher = await _supportService.ApproveVoucherAsync(id, dto, cancellationToken);
            if (voucher == null)
            {
                return NotFound(new { message = $"Voucher with ID {id} was not found." });
            }
            return Ok(voucher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving voucher {VoucherId}", id);
            return StatusCode(500, new { message = "An error occurred while approving the voucher." });
        }
    }

    /// <summary>
    /// Human approval gate: an authorized Admin/Operator rejects a drafted goodwill voucher — updates status to Revoked and updates linked support ticket.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    public async Task<ActionResult<VoucherResponseDto>> RejectVoucher(Guid id, [FromBody] RejectVoucherDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        dto.AdminId = user.Id;

        try
        {
            var voucher = await _supportService.RejectVoucherAsync(id, dto, cancellationToken);
            if (voucher == null)
            {
                return NotFound(new { message = $"Voucher with ID {id} was not found." });
            }
            return Ok(voucher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting voucher {VoucherId}", id);
            return StatusCode(500, new { message = "An error occurred while rejecting the voucher." });
        }
    }

    /// <summary>
    /// Retrieve all goodwill vouchers with optional status filter for the Admin Voucher Sign-off portal. Staff only.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VoucherResponseDto>>> GetAllVouchers([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        var vouchers = await _supportService.GetAllVouchersAsync(status, cancellationToken);
        return Ok(vouchers);
    }

    /// <summary>
    /// Retrieve digital vouchers for a given traveler (for Mobile Digital Voucher Wallet).
    /// Travelers may only read their own vouchers; staff may read any.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<VoucherResponseDto>>> GetVouchersByUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (user.Id != userId && !IsStaff(user)) return Forbid();

        var vouchers = await _supportService.GetVouchersByUserIdAsync(userId, cancellationToken);
        return Ok(vouchers);
    }

    /// <summary>
    /// Retrieve details for a single goodwill voucher by ID. Owner or staff only.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherResponseDto>> GetVoucherById(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        var voucher = await _supportService.GetVoucherByIdAsync(id, cancellationToken);
        if (voucher == null || (voucher.UserId != user.Id && !IsStaff(user)))
        {
            return NotFound(new { message = $"Voucher with ID {id} was not found." });
        }
        return Ok(voucher);
    }

    /// <summary>
    /// Soft delete a goodwill voucher. Staff only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVoucher(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        var success = await _supportService.DeleteVoucherAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Voucher with ID {id} was not found." });
        }
        return NoContent();
    }
}
