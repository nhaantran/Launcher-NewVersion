using System.ComponentModel;

namespace Launcher.Models
{
    public enum MessageBoxTitle
    {
        [Description("Trò chơi vẫn còn đang chạy!!! Hãy tắt trò chơi.")]
        TurnOffGame,

        [Description("Có lỗi xảy ra trong quá trình tải!!!")]
        ErrorWhileDownloading,

        [Description("Cập nhật thất bại!!! Vui lòng thử lại sau.")]
        UpdateFailed,

        [Description("Lấy dữ liệu từ máy chủ thất bại!!! Vui lòng liên hệ với GM và thử lại sau.")]
        GetServerDataFailed,

        [Description("Lỗi dường truyền!!! Vui lòng thử lại sau.")]
        NetworkError,

        [Description("Quá trình chuẩn bị data thất bại!!! Vui lòng thử lại sau.")]
        PrepareDataFailed,

        [Description("Không tìm thấy file config!!!")]
        ConfigFileNotFound,

        [Description("Không thể tìm thấy trò chơi! Nhấn Sửa lỗi để khắc phục")]
        GameNotFound,

        [Description("Cập nhật danh sách tin tức thất bại, bạn có thể vô trang chủ để xem tin tức mới.\nẤn Start để khởi động game.")]
        UpdateNewsFailed,

        [Description("Đã xảy ra lỗi!!! Nhấn Sửa lỗi để khắc phục")]
        ErrorOccurred,

        [Description("Máy chủ đang quá tải!!!")]
        Overload,
    }
}
