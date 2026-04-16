namespace E_ShoppingManagement.ViewModels
{
    public class EmployeeGoalViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public decimal CurrentGoal { get; set; }
        public int Year { get; set; }
    }
}
