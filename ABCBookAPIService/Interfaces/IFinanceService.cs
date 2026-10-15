using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Finance;

namespace ABCBookAPIService.Interfaces
{
    public interface IFinanceService
    {
        // 1. Quản lý Nhóm chi phí & Phiếu chi (OPEX)
        Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync();
        Task<Guid> CreateExpenseCategoryAsync(ExpenseCategoryDto dto, string operatorName);
        Task<PagedResult<ExpenseVoucherDto>> GetExpenseVouchersAsync(
            Guid? storeId,
            Guid? categoryId,
            ExpenseStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            int pageIndex,
            int pageSize);
        Task<Guid> CreateExpenseVoucherAsync(ExpenseVoucherCreateDto dto, string operatorName);
        Task<bool> ApproveExpenseVoucherAsync(Guid voucherId, string operatorName);
        Task<bool> RejectExpenseVoucherAsync(Guid voucherId, string reason, string operatorName);

        // 2. Báo cáo Tài chính P&L Cửa hàng & Toàn Chuỗi
        Task<StoreProfitLossDto> GetStoreProfitLossAsync(Guid? storeId, DateTime fromDate, DateTime toDate);
        Task<byte[]> GenerateProfitLossExcelReportAsync(Guid? storeId, DateTime fromDate, DateTime toDate);

        // 3. Tính lương nhân viên (Payroll)
        Task<List<StaffPayrollSlipDto>> CalculateMonthlyPayrollAsync(Guid? storeId, int month, int year);
    }
}
