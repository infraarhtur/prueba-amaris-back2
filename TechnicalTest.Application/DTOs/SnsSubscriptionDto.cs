namespace TechnicalTest.Application.DTOs;

public class SnsSubscriptionDto
{
    public string SubscriptionArn { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string TopicArn { get; set; } = string.Empty;
}

