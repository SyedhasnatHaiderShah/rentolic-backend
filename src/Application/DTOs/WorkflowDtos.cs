namespace Rentolic.Application.DTOs;

public class SubUserCreateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Permissions { get; set; } // JSON string
    public Guid? ParentId { get; set; }
}

public class PaymentIntentRequest
{
    public Guid InvoiceId { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class PaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class MrzExtractionRequest
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "EMIRATES_ID";
}

public class MrzExtractionResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
}

public class OtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Code { get; set; }
}
