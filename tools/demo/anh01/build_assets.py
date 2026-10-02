"""Rebuild fictional demo assets locally; no network or database access."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "docs/demo/anh01"
FACTS = json.loads((OUT / "facts.json").read_text(encoding="utf-8"))
DISCLAIMER = "DỮ LIỆU DEMO — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ"


def dossier(font_dir):
    pdfmetrics.registerFont(TTFont("Demo", str(font_dir / "arial.ttf")))
    pdfmetrics.registerFont(TTFont("DemoBold", str(font_dir / "arialbd.ttf")))
    styles = {
        "body": ParagraphStyle("body", fontName="Demo", fontSize=10, leading=15, spaceAfter=8),
        "title": ParagraphStyle("title", fontName="DemoBold", fontSize=22, leading=29, spaceAfter=18, textColor=colors.HexColor("#153c35")),
        "heading": ParagraphStyle("heading", fontName="DemoBold", fontSize=13, leading=18, spaceBefore=12, spaceAfter=9),
        "small": ParagraphStyle("small", fontName="Demo", fontSize=8, leading=12, spaceAfter=7),
    }
    story = []

    def p(text, style="body"):
        return Paragraph(text, styles[style])

    def table(rows, widths):
        result = Table([[p(str(cell), "small") for cell in row] for row in rows], colWidths=widths, repeatRows=1)
        result.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#e0eee9")),
            ("GRID", (0, 0), (-1, -1), .5, colors.HexColor("#b7c9c3")),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("TOPPADDING", (0, 0), (-1, -1), 9), ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ]))
        return result

    def header(title):
        story.extend([p("ROADGUARD / ANH-01 / DEMO", "small"), p(title, "title"), p(DISCLAIMER, "heading")])

    def footer(canvas, doc):
        canvas.setFont("Demo", 8)
        canvas.setFillColor(colors.HexColor("#aa2626"))
        canvas.drawString(44, 30, DISCLAIMER)
        canvas.setFillColor(colors.black)
        canvas.drawRightString(A4[0] - 44, 30, f"DEMO / {doc.page}")

    header("Hồ sơ dự án đường mô phỏng")
    story.append(p("Bộ hồ sơ hư cấu để trình diễn luồng project → geometry → segment → survey → dataset → đánh giá thủ công. Không đại diện công trình, tổ chức hoặc cá nhân thật."))
    story.append(table([
        ["Thông tin", "Giá trị hư cấu"],
        ["Tên / mã", f"{FACTS['name']}<br/>{FACTS['code']}"],
        ["Tuyến", "DEMO-R01 / 200 m / 2 đoạn, mỗi đoạn 100 m"],
        ["Bàn giao", "01/09/2026 / " + FACTS["handoverDocumentNo"]],
        ["Bảo hành mô phỏng", "01/09/2026 đến 01/09/2027; chỉ là lựa chọn của kịch bản demo"],
        ["Mặt đường / vùng khảo sát", "Tổng bề rộng 7 m / 9 m; không phải bề rộng đệm mỗi bên"],
        ["Hệ tọa độ / lý trình", "EPSG:32648 (m); lý trình gốc 1.000 m"],
        ["Lịch khảo sát", "05/10/2026, 08:00-17:00, UTC+07:00"],
    ], [155, 352]))
    story.extend([p("Danh mục hồ sơ", "heading"), p("1. Thông tin dự án; 2. Biên bản bàn giao mô phỏng; 3. Nghĩa vụ bảo hành mô phỏng; 4. Danh mục tuyến, segment và kế hoạch khảo sát; 5. Nguồn tham khảo và giới hạn."), p("Toàn bộ các trang được gói trong một PDF để dùng như một handover attachment. PDF này không thay thế video khảo sát và không chứng minh chất lượng công trình.")])
    story.append(PageBreak())

    header("Biên bản bàn giao mô phỏng")
    story.append(p("Số: " + FACTS["handoverDocumentNo"] + " / Ngày: 01/09/2026"))
    story.append(p("Bên giao: Đơn vị thi công DEMO A (hư cấu). Bên nhận: Đơn vị quản lý DEMO B (hư cấu). Đại diện chỉ được mô tả bằng vai trò; không có tên người thật, con dấu hoặc chữ ký."))
    story.append(p("Phạm vi bàn giao", "heading"))
    story.append(p("Tuyến DEMO-R01 dài 200 m, từ lý trình 1.000 m đến 1.200 m, bề rộng mặt đường 7 m. Hai đoạn 100 m được quản lý riêng để minh họa baseline từng phần. Không mô phỏng giấy phép, cấp công trình hoặc chứng nhận nghiệm thu nhà nước."))
    story.append(table([["Hạng mục", "Nội dung mô phỏng"], ["Hồ sơ kèm theo", "Trang thông tin, nghĩa vụ bảo hành, geometry/segment và lịch khảo sát trong PDF này."], ["Tình trạng", "Dữ liệu giả lập để chạy workflow; không kết luận công trình đủ điều kiện sử dụng."], ["Tồn tại cần theo dõi", "Bằng chứng RIGHT_EDGE của đoạn 1 còn thiếu; SURFACE đoạn 2 không đủ chất lượng/phạm vi."], ["Xác nhận", "Không ký; không đóng dấu. Chỉ lưu dấu vết thao tác API trong môi trường demo cô lập."]], [155, 352]))
    story.extend([p("Kết luận mô phỏng", "heading"), p("Bàn giao tập dữ liệu phục vụ trình diễn. Việc chọn baseline khảo sát chỉ chấp nhận band có ba chiều PASS theo pilot manual; không phải nghiệm thu công trình, xác nhận hết bảo hành hay kết luận không có lỗi.")])
    story.append(PageBreak())

    header("Nghĩa vụ bảo hành mô phỏng")
    story.append(p("Kỳ mô phỏng: 01/09/2026 đến 01/09/2027. Kỳ này là dữ liệu hư cấu, không được trình bày như thời hạn pháp luật hoặc tiêu chuẩn bắt buộc. Không mô phỏng tiền giữ lại hoặc bảo lãnh."))
    story.append(table([["Vai trò hư cấu", "Nghĩa vụ trong kịch bản"], ["Đơn vị thi công DEMO A", "Tiếp nhận thông báo hư hỏng, khảo sát, đề xuất sửa chữa và cung cấp bằng chứng xử lý đối với phạm vi tuyến DEMO-R01."], ["Đơn vị quản lý DEMO B", "Ghi nhận vị trí, thông báo sự cố/hư hỏng, quản lý hồ sơ và theo dõi yêu cầu bảo hành."], ["PM DEMO", "Lập lịch, giao khảo sát, xem video và SRT, ghi nhận PASS/FAIL/UNKNOWN theo từng chiều và chọn baseline phần đủ căn cứ."], ["Operator DEMO", "Nhận task; upload media đúng task; nộp dataset immutable; thực hiện child supplement khi được giao."]], [155, 352]))
    story.extend([p("Phạm vi và giới hạn", "heading"), p("Phạm vi mô phỏng là mặt đường và mép phải được chỉ rõ trong kế hoạch. Nghĩa vụ sửa chữa, chi phí, thời gian phản hồi và tranh chấp thực tế không được xác định bởi hồ sơ demo này. Không suy kỳ bảo hành thành thời hạn xóa dữ liệu."), p("Nguồn được dùng để tham khảo cấu trúc về phạm vi, trách nhiệm, hồ sơ bàn giao và bảo hành; không đánh giá hiệu lực pháp luật hiện tại. Xem provenance ở trang cuối.")])
    story.append(PageBreak())

    header("Tuyến, segment và kế hoạch khảo sát")
    story.append(p("Tim tuyến metric: (500000, 1200000) → (500200, 1200000), EPSG:32648. Các tọa độ là giả lập; không chỉ một công trình thật. Hình tuyến là đoạn thẳng 200 m, bề rộng mặt đường 7 m, vùng khảo sát 9 m."))
    story.append(table([["Segment", "Offset (m)", "Lý trình (m)", "Chiều dài"], ["DEMO-S01", "0-100", "1000-1100", "100 m"], ["DEMO-S02", "100-200", "1100-1200", "100 m"]], [100, 130, 150, 127]))
    story.append(p("Kế hoạch: 05/10/2026, 08:00-17:00 (UTC+07). SURFACE: cả hai segment; RIGHT_EDGE: segment 1. Một synthetic MP4 dài 4 giây và SRT ghép cặp dùng làm dữ liệu demo, không phải ảnh hiện trường."))
    story.append(table([["Scope", "Vị trí", "Chất lượng", "Phạm vi"], ["S01 / SURFACE / 0-2 s", "PASS", "PASS", "PASS"], ["S01 / RIGHT_EDGE / thiếu căn cứ", "UNKNOWN", "UNKNOWN", "UNKNOWN"], ["S02 / SURFACE / 2-4 s", "PASS", "FAIL", "FAIL"]], [240, 89, 89, 89]))
    story.append(p("Kịch bản PM xem synthetic overlay để xác nhận vị trí, thấy SURFACE S01 rõ và đủ phạm vi; S02 có vật cản cố ý; RIGHT_EDGE S01 chưa được quay. Kết quả là dữ liệu thủ công của demo, không phải thuật toán chấm tự động."))
    story.append(p("Chỉ chọn baseline S01 / SURFACE qua API. Tạo supplement child cho S01 / RIGHT_EDGE và S02 / SURFACE; không seed baseline, không sửa source dataset hay tái sử dụng segment ID theo sequence sau publish mới."))
    story.append(PageBreak())

    header("Nguồn tham khảo và provenance")
    story.append(p("Ngày truy cập: 02/10/2026. Nghiên cứu: CURRENT_VERIFIED - đã truy cập trang công bố chính thức, tải bản PDF ký số và đọc trực quan trang in 34-36."))
    for source in FACTS["provenance"]["sources"]:
        story.append(p(source["url"], "small"))
    story.append(p("Nguồn: Nghị định 06/2021/NĐ-CP trên Cổng thông tin điện tử Chính phủ. Điều 26: tổ chức hồ sơ hoàn thành; Điều 27: bàn giao và hồ sơ phục vụ quản lý, vận hành, bảo trì; Điều 28-29: phạm vi, thời gian và trách nhiệm bảo hành."))
    story.append(p("Nội dung được tham khảo: cách nhóm hồ sơ, nhận diện bên giao/nhận, mô tả phạm vi, tài liệu kèm theo và bảng trách nhiệm. Chỉ diễn giải cấu trúc/thuật ngữ; không sao chép dấu, chữ ký, mẫu pháp lý hoặc khẳng định hiệu lực/điều kiện áp dụng hiện tại."))
    story.append(p("Facts canonical: docs/demo/anh01/facts.json. Builder đọc cùng facts; script setup dùng cùng geometry, thời gian và scenario. GUID tài nguyên do API cấp; operationId/idempotency-key demo cố định bảo đảm retry, không giả định ID production."))
    story.append(p("Kiểm chứng tài liệu không đồng nghĩa BE integration. Chỉ sau khi upload VERIFIED, submit dataset, assessment và baseline chạy qua API/SQL trong môi trường cô lập mới được ghi BE verified. Live external/deployment và chất lượng hiện trường không được kiểm chứng bởi tài sản synthetic."))
    SimpleDocTemplate(str(OUT / "dossier-demo.pdf"), pagesize=A4, rightMargin=44, leftMargin=44, topMargin=43, bottomMargin=52).build(story, onFirstPage=footer, onLaterPages=footer)


def media(ffmpeg, font_dir):
    font = ImageFont.truetype(str(font_dir / "arialbd.ttf"), 16)
    frames = []
    for index in range(40):
        segment = 1 if index < 20 else 2
        offset = (index + .5) * 5
        img = Image.new("RGB", (640, 360), "#bcd9bd")
        draw = ImageDraw.Draw(img)
        draw.rectangle((0, 70, 640, 300), fill="#686c70")
        for x in range(-40, 700, 90):
            draw.rectangle((x - index * 6 % 90, 181, x + 42 - index * 6 % 90, 187), fill="white")
        draw.line((0, 75, 640, 75), fill="white", width=3)
        draw.line((0, 295, 640, 295), fill="white", width=3)
        if segment == 2:
            img = img.filter(ImageFilter.GaussianBlur(12))
            draw = ImageDraw.Draw(img)
            draw.rectangle((145, 105, 520, 275), fill="#222222")
            draw.text((190, 185), "DEMO OBSTRUCTION", font=font, fill="white")
        draw.rectangle((0, 0, 640, 65), fill="#142d27")
        draw.text((14, 10), "DEMO SYNTHETIC - NOT FIELD FOOTAGE", font=font, fill="white")
        draw.text((14, 35), f"S0{segment} SURFACE / EPSG32648 E={500000 + offset:.1f} N=1200000", font=font, fill="white")
        draw.rectangle((0, 310, 640, 360), fill="#142d27")
        draw.text((14, 322), f"t={index / 10:.1f}s / station={1000 + offset:.1f}m / width=7m", font=font, fill="white")
        frames.append(img.tobytes())
    proc = subprocess.run([str(ffmpeg), "-y", "-f", "rawvideo", "-pixel_format", "rgb24", "-video_size", "640x360", "-framerate", "10", "-i", "pipe:0", "-an", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-map_metadata", "-1", "-movflags", "+faststart", str(OUT / "survey-synthetic.mp4")], input=b"".join(frames), capture_output=True)
    if proc.returncode:
        raise RuntimeError(proc.stderr.decode(errors="replace"))
    cues = []
    for index in range(4):
        offset = index * 50 + 25
        cues.append(f"{index + 1}\n00:00:0{index},000 --> 00:00:0{index + 1},000\nDEMO SYNTHETIC EPSG:32648 E:{500000 + offset} N:1200000 station:{1000 + offset}m\n")
    (OUT / "survey-synthetic.srt").write_text("\n".join(cues), encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--font-dir", type=Path, default=Path("C:/Windows/Fonts"))
    parser.add_argument("--ffmpeg", type=Path)
    args = parser.parse_args()
    dossier(args.font_dir)
    if args.ffmpeg:
        media(args.ffmpeg, args.font_dir)
    assets = []
    for filename in ("dossier-demo.pdf", "survey-synthetic.mp4", "survey-synthetic.srt"):
        path = OUT / filename
        if path.exists():
            assets.append({"fileName": filename, "sizeBytes": path.stat().st_size, "checksumSha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    (OUT / "asset-manifest.json").write_text(json.dumps({"label": "DEMO SYNTHETIC", "files": assets}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(assets, indent=2))
