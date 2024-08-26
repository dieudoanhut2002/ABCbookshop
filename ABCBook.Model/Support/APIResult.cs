namespace ABCBook.Model.Support
{
    public class APIResult
    {

        /// <summary>
        /// <para>Trạng thái của API(mặc định là thất bại)</para>
        /// <para>true: Thành công</para>
        /// <para>false: Thất bại</para>
        /// </summary>
        public bool Status { get; set; } = false;
        /// <summary>
        /// Mã lỗi cho DEV
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// ID theo dõi APIResult
        /// </summary>
        public string TraceID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Thông báo cho DEV
        /// </summary>
        public string InternalMessage { get; set; }
        /// <summary>
        /// Thông báo cho người dùng
        /// </summary>
        public string UserMessage { get; set; } = "Có lỗi không mong muốn xảy ra, vui lòng thử lại sau";

        /// <summary>
        /// Thông báo của hệ thống khi Code bằng 200
        /// </summary>
        /// <example>Gọi api thành công</example>
        public String? Information { get; set; }
    }
    /// <summary>
    /// Object trả về dữ liệu cho toàn bộ API
    /// </summary>
    public class APIResult<TDataResult> : APIResult
    {

        /// <summary>
        /// chứa dữ liệu trả về
        /// </summary>
        public TDataResult Data { get; set; } = default(TDataResult);
    }
}
