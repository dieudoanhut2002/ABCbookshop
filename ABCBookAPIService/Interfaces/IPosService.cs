using ABCBook.Model.Entity;
using ABCBookAPIService.DTOs.POS;

namespace ABCBookAPIService.Interfaces
{
    public interface IShiftService
    {
        Task<ShiftDetailDto?> GetActiveShiftForRegisterAsync(Guid registerId);
        Task<ShiftDetailDto?> GetActiveShiftForStaffAsync(Guid staffId);
        Task<ShiftDetailDto?> GetShiftByIdAsync(Guid shiftId);
        Task<Guid> OpenShiftAsync(ShiftOpenDto dto, string operatorName);
        Task<ShiftDetailDto> CloseShiftAsync(ShiftCloseDto dto, string operatorName);
        Task<List<ShiftDetailDto>> GetRecentShiftsAsync(Guid? storeId = null, int count = 20);
        Task<bool> ApproveShiftAsync(Guid shiftId, string operatorName);
    }

    public interface IPosService
    {
        Task<List<PosProductSearchDto>> SearchProductsForPosAsync(Guid warehouseId, string keyword);
        Task<Customer?> FindCustomerByPhoneAsync(string phone);
        Task<Customer> CreateQuickCustomerAsync(string fullName, string phone, string operatorName);
        Task<PosOrderReceiptDto> ProcessCheckoutAsync(PosCheckoutDto dto, string operatorName);
        Task<PosOrderReceiptDto?> GetOrderReceiptAsync(Guid orderId);
    }

    public interface IShiftReportService
    {
        Task<byte[]> GenerateShiftExcelReportAsync(Guid shiftId);
        Task<bool> SendShiftTelegramAlertAsync(Guid shiftId);
        Task<bool> SendShiftEmailReportAsync(Guid shiftId, string? recipientEmail = null);
        Task CheckAndAlertUnclosedShiftsAsync();
    }
}
