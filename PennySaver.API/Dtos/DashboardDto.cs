namespace PennySaver.API.Models
{
    public class DashboardDto
    {
        public decimal TotalCash { get; set; }
        public decimal MonthlyBudget { get; set; }
        public decimal RemainingBudget { get; set; }
    }
}