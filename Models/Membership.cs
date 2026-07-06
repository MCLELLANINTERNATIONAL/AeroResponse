namespace AeroResponse.Models;

public class Membership
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string PlanName { get; set; } = "Basic";

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    public string PaymentStatus { get; set; } = "Pending";
}