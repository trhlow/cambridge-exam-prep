# Kiểm thử tải (VE-41)

`loadtest.mjs` giả lập một phòng thi: N thí sinh đăng nhập (phiếu + mã PIN), bắt đầu phần thi đầu, giữ kết nối SignalR `/hubs/candidate`, gửi heartbeat mỗi 5 s và lưu một câu trả lời mỗi 4–12 s như trình duyệt; một giám thị theo dõi qua `/hubs/staff`, tải lại trang giám sát mỗi 3 s và giữa giờ tạm dừng rồi cho làm tiếp một nhóm thí sinh. Mỗi yêu cầu được đo thời gian; báo cáo gồm p50/p95/p99 theo loại yêu cầu, lỗi, thời gian lệnh của giám thị đến màn hình thí sinh và CPU/bộ nhớ của các container.

Chỉ chạy trên stack e2e tách riêng (cổng 8090), không bao giờ trên CSDL demo: công cụ tạo một kỳ thi "Load test …" cùng các thí sinh của nó.

```bash
# từ thư mục gốc repo
WEB_PORT=8090 MYSQL_PORT=3307 docker compose -p examprep-e2e -f docker/docker-compose.yml -f docker/docker-compose.e2e.yml --env-file docker/.env up -d --build
cd setup/loadtest && npm install
node loadtest.mjs --candidates 100 --minutes 5 --ramp 30 --pausing 10
# dọn: docker compose -p examprep-e2e ... down -v
```

Tham số: `--base` (mặc định `http://localhost:8090`), `--candidates`, `--minutes`, `--ramp` (giây rải đăng nhập), `--pausing` (số thí sinh bị tạm dừng giữa giờ), `--admin user:pass`, `--invigilator user:pass`, `--out` (thư mục báo cáo, mặc định `results/`).

Kết quả các lần chạy nằm trong `results/` (Markdown, đưa vào Chương 4 của báo cáo).
