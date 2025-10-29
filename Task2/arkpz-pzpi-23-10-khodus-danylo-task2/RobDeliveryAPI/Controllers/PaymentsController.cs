using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessorService _paymentProcessorService;

    public PaymentsController(IPaymentProcessorService paymentProcessorService)
    {
        _paymentProcessorService = paymentProcessorService;
    }

    /// <summary>
    /// Process a payment using the specified payment method (PayPal, GooglePay, or Stripe)
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<PaymentResultDTO>> ProcessPayment([FromBody] PaymentRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    TransactionId = string.Empty,
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorMessage = "Payment amount must be greater than zero"
                });
            }

            var result = await _paymentProcessorService.ProcessPaymentAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Refund a previously processed payment
    /// </summary>
    [HttpPost("refund")]
    public async Task<ActionResult<object>> RefundPayment([FromBody] RefundRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Refund amount must be greater than zero" });
            }

            var result = await _paymentProcessorService.RefundPaymentAsync(
                request.PaymentMethod,
                request.TransactionId,
                request.Amount
            );

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Refund processed successfully",
                    transactionId = request.TransactionId,
                    amount = request.Amount,
                    processedAt = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Refund processing failed"
                });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }
}

public class RefundRequestDTO
{
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
