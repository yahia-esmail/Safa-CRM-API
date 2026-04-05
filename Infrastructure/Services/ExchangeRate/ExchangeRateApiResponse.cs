namespace Infrastructure.Services.ExchangeRate;

public class ExchangeRateApiResponse
{
    public string Result { get; set; } = string.Empty;
    public string Base_Code { get; set; } = "USD";
    public Dictionary<string, decimal> Conversion_Rates { get; set; } = [];
}
