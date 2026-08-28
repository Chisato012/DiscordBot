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

GitHub Actions sẽ publish Native AOT cho `win-x64` và `linux-x64` khi thay đổi mã nguồn bot, khi mở pull request, hoặc khi chạy thủ công từ tab **Actions**. Bản build được đính kèm vào mỗi workflow run dưới dạng artifact `.tar.gz` và được giữ 14 ngày.

Danh sách bot và nền tảng build nằm trong `.github/ci-targets.json`. Khi thêm bot mới, thêm một mục vào `projects`; không cần tạo workflow mới. Nếu một GUI chưa tương thích Native AOT, đặt `publishAot` thành `false` cho project đó.
