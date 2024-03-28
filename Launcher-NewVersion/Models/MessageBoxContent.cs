using System.ComponentModel;

namespace Launcher.Models
{
    public enum MessageBoxContent
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
    }
}
