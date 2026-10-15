using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.Finance
{
    public class ExpenseCategoryDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ExpenseVoucherDto
    {
        public Guid Id { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Payee { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public ExpenseStatus Status { get; set; }
        public string StatusName => Status switch
        {
            ExpenseStatus.Pending => "Chờ phê duyệt",
            ExpenseStatus.Approved => "Đã duyệt chi",
            ExpenseStatus.Paid => "Đã chi tiền",
            ExpenseStatus.Rejected => "Đã từ chối",
            _ => "Không xác định"
        };
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class ExpenseVoucherCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn cửa hàng")]
        public Guid StoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhóm chi phí")]
        public Guid CategoryId { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        [Range(1000, 10000000000, ErrorMessage = "Số tiền chi phí phải lớn hơn 1,000 ₫")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập người nhận tiền / Đơn vị cung cấp")]
        [MaxLength(200)]
        public string Payee { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lý do / nội dung chi phí")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsRecurring { get; set; } = false;
    }

    public class StoreProfitLossDto
    {
        public Guid? StoreId { get; set; }
        public string StoreName { get; set; } = "Toàn hệ thống chuỗi";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Doanh thu
        public decimal GrossRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalItemsSold { get; set; }

        // Giá vốn & Lợi nhuận gộp
        public decimal CostOfGoodsSold { get; set; } // COGS
        public decimal GrossProfit => GrossRevenue - CostOfGoodsSold;
        public decimal GrossMarginPercent => GrossRevenue > 0 ? Math.Round((GrossProfit / GrossRevenue) * 100, 2) : 0;

        // Chi phí vận hành OPEX
        public decimal TotalOperatingExpenses { get; set; }
        public Dictionary<string, decimal> ExpenseBreakdownByCategory { get; set; } = new Dictionary<string, decimal>();

        // Lợi nhuận thuần hoạt động kinh doanh (Operating Profit)
        public decimal OperatingProfit => GrossProfit - TotalOperatingExpenses;
        public decimal OperatingMarginPercent => GrossRevenue > 0 ? Math.Round((OperatingProfit / GrossRevenue) * 100, 2) : 0;
    }

    public class StaffPayrollSlipDto
    {
        public Guid StaffId { get; set; }
        public string StaffCode { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal SalesRevenueGenerated { get; set; }
        public decimal CommissionRate { get; set; } = 0.01m; // 1% hoa hồng doanh thu
        public decimal CommissionAmount => SalesRevenueGenerated * CommissionRate;
        public decimal Allowances { get; set; } = 0;
        public decimal Deductions { get; set; } = 0;
        public decimal NetSalary => BaseSalary + CommissionAmount + Allowances - Deductions;
    }
}
