namespace ABCBook.Model.Enum
{
    public enum SystemRole
    {
        SysChainOwner = 1,     // Chủ chuỗi / Quản trị tối cao
        ChainManager = 2,      // Quản lý chuỗi
        StoreManager = 3,      // Cửa hàng trưởng (phạm vi store)
        InventoryManager = 4,  // Quản lý kho chuỗi/store
        Accountant = 5,        // Kế toán chuỗi/store
        Cashier = 6,           // Thu ngân tại quầy
        SalesStaff = 7,        // Nhân viên bán hàng
        Customer = 8           // Khách hàng trực tuyến
    }

    public enum ProductType
    {
        Book = 1,              // Sách (có ISBN, tác giả, NXB)
        Stationery = 2,        // Văn phòng phẩm
        Souvenir = 3,          // Quà tặng & Hàng lưu niệm
        OtherRetail = 4        // Hàng hóa bán lẻ khác
    }

    public enum StockTransactionType
    {
        PurchaseReceipt = 1,   // Nhập kho từ NCC (PO/GRN)
        PosSale = 2,           // Xuất kho bán lẻ tại quầy POS
        OnlineSale = 3,        // Xuất kho bán hàng online E-commerce
        TransferOut = 4,       // Xuất luân chuyển đi chi nhánh khác
        TransferIn = 5,        // Nhập luân chuyển từ chi nhánh khác về
        DamagedLoss = 6,       // Xuất hủy hàng hỏng / mất mát
        StockAuditAdjustment = 7, // Điều chỉnh kiểm kê
        CustomerReturn = 8,    // Nhập hàng khách đổi trả
        SupplierReturn = 9     // Xuất trả hàng nhà cung cấp
    }

    public enum TransferStatus
    {
        Draft = 1,             // Dự thảo yêu cầu
        Dispatched = 2,        // Đã xuất kho, đang vận chuyển (In-Transit)
        Received = 3,          // Đã nhận đủ hoặc nhận kèm biên bản
        Cancelled = 4          // Hủy điều chuyển
    }

    public enum PurchaseOrderStatus
    {
        Draft = 1,
        Confirmed = 2,
        PartialReceived = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum ShiftStatus
    {
        Open = 1,              // Ca đang mở, thu ngân đang bán
        Closed = 2,            // Thu ngân đã đóng ca, chốt kiểm đếm
        Approved = 3           // Quản lý cửa hàng đã xác nhận duyệt ca
    }

    public enum OrderSource
    {
        Pos = 1,               // Đơn bán tại quầy thu ngân
        WebEcommerce = 2       // Đơn đặt từ website bán hàng trực tuyến
    }

    public enum OrderStatus
    {
        PendingPayment = 1,    // Chờ thanh toán (đơn online hoặc pending)
        Completed = 2,         // Hoàn tất (hàng đã giao, tiền đã thu)
        Cancelled = 3,         // Hủy đơn
        Refunded = 4           // Hoàn tiền toàn phần
    }

    public enum PaymentMethod
    {
        Cash = 1,              // Tiền mặt tại quầy
        VietQR = 2,            // Chuyển khoản VietQR động
        BankCard = 3,          // Quẹt thẻ POS ngân hàng
        Cod = 4                // Thu tiền khi nhận hàng (COD)
    }

    public enum ExpenseStatus
    {
        Pending = 1,           // Chờ duyệt chi
        Approved = 2,          // Đã được phê duyệt
        Paid = 3,              // Đã xuất quỹ chi trả
        Rejected = 4           // Từ chối phê duyệt
    }

    public enum LoyaltyTransactionType
    {
        EarnFromOrder = 1,     // Tích điểm từ đơn hàng thành công
        RedeemForDiscount = 2, // Đổi điểm giảm giá đơn hàng
        ManualAdjustment = 3,  // Điều chỉnh thủ công (chăm sóc KH)
        Expired = 4            // Hết hạn điểm thưởng
    }
}
