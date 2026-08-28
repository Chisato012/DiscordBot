# Bot Discord

Rảnh thì làm vui + cứu giúp nhu cầu nhỏ cho các bros trong discord

## 🚀 Tính năng chính
* Hỗ trợ hai phương thức tương tác: Giao diện dòng lệnh (Console) và Giao diện đồ họa (GUI).

## 📁 Cấu trúc thư mục
* `DiscordBot.Console/`: Chạy trên nền tảng dòng lệnh.
* `DiscordBot.GUI/`: Đồ họa trực quan.

Mỗi bot là một project độc lập nằm trong thư mục giao diện phù hợp, ví dụ `DiscordBot.Console/StockBot/StockBot.csproj`. Cách này cho phép thêm bot console hoặc GUI mà không làm lẫn mã nguồn và cấu hình build.

## 🛠️ API sử dụng
Dự án này tham khảo từ nhiều nguồn khác nhau:
* **[VNSTOCK](https://github.com/thinh-vu/vnstock/blob/main/vnstock/explorer/kbs/const.py?utm_source=chatgpt.com)**: Tham khảo lấy API từ các sàn chứng khoán.

## ⚙️ Công nghệ sử dụng
* .NET 10 (Native AOT)

## 🏗️ Build tự động

GitHub Actions tự tìm mọi file `.csproj` đặt trực tiếp trong một thư mục bot của `DiscordBot.Console/`, rồi publish theo cấu hình của chính project. Với cấu hình hiện tại, mỗi console bot được build Native AOT cho `win-x64` và `linux-x64` khi thay đổi mã nguồn bot, khi mở pull request, hoặc khi chạy thủ công từ tab **Actions**. Bản build được đính kèm vào mỗi workflow run dưới dạng artifact `.tar.gz` và được giữ 14 ngày.

Thêm console bot mới chỉ cần tạo project theo dạng `DiscordBot.Console/TenBot/TenBot.csproj`; không cần sửa workflow hay cấu hình build. Danh sách nhóm thư mục và nền tảng build nằm trong `.github/build-groups.json`. Khi thêm GUI, copy block `console`, đổi `name` và `directory` thành `gui` và `DiscordBot.GUI`, sau đó chọn các nền tảng GUI hỗ trợ. Một project không tương thích Native AOT chỉ cần không đặt `<PublishAot>true</PublishAot>` trong file `.csproj` của riêng nó.

## 📦 Phát hành phiên bản

Mỗi bot có chuỗi phiên bản riêng và dùng tag theo dạng `tenbot-vMAJOR.MINOR.PATCH`, ví dụ `stockbot-v1.0.0` hoặc `stockbot-v1.1.0-beta.1`. Push tag sẽ chỉ build những project nằm trong thư mục bot tương ứng (ví dụ `StockBot`) và tự tạo GitHub Release với các binary của bot đó. Tag bản vá của StockBot không ảnh hưởng đến bot khác; một bot mới có thể bắt đầu từ `tenbot-v1.0.0`.

```powershell
git tag -a stockbot-v1.0.0 -m "StockBot v1.0.0"
git push origin stockbot-v1.0.0
```
