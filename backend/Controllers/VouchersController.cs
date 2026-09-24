using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/support/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly ISupportService _supportService;
    private readonly ILogger<VouchersController> _logger;

    public VouchersController(ISupportService supportService, ILogger<VouchersController> logger)
    {
        _supportService = supportService;
        _logger = logger;
    }

    /// <summary>
    /// Issue a goodwill discount voucher manually or via admin.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VoucherResponseDto>> IssueVoucher([FromBody] CreateVoucherDto dto, CancellationToken cancellationToken)
    {
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
    /// Admin approves a drafted goodwill voucher — updates status to Active, notifies customer, and resolves linked ticket.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<VoucherResponseDto>> ApproveVoucher(Guid id, [FromBody] ApproveVoucherDto dto, CancellationToken cancellationToken)
    {
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
    /// Retrieve all goodwill vouchers with optional status filter for the Admin Voucher Sign-off and Management portal.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VoucherResponseDto>>> GetAllVouchers([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var vouchers = await _supportService.GetAllVouchersAsync(status, cancellationToken);
        return Ok(vouchers);
    }

    /// <summary>
    /// Retrieve digital vouchers for a given traveler (for Mobile Digital Voucher Wallet).
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<VoucherResponseDto>>> GetVouchersByUser(Guid userId, CancellationToken cancellationToken)
    {
        var vouchers = await _supportService.GetVouchersByUserIdAsync(userId, cancellationToken);
        return Ok(vouchers);
    }
}
