const Attendance = () => {
  const dailyMoves = [
    { name: "Ahmet Yılmaz", time: "08:45", status: "ontime", avatar: "AY", dept: "Yazılım" },
    { name: "Selin Koç", time: "09:15", status: "late", avatar: "SK", dept: "Tasarım" },
    { name: "Burak Demir", time: "--:--", status: "absent", avatar: "BD", dept: "Satış" },
    { name: "Ayşe Vural", time: "08:50", status: "ontime", avatar: "AV", dept: "İK" },
    { name: "Mert Can", time: "08:30", status: "early", avatar: "MC", dept: "Yazılım" },
  ];

  const timesheetData = [
    { date: "01 Eki Pzt", checkIn: "09:00", checkOut: "18:00", break: "1s", total: "08:00", type: "Tam", status: "ok" },
    { date: "02 Eki Sal", checkIn: "09:15", checkOut: "18:15", break: "1s", total: "08:00", type: "Tam", status: "late" },
    { date: "03 Eki Çar", checkIn: "09:00", checkOut: "20:00", break: "1s", total: "10:00", type: "Mesai", status: "overtime" },
    { date: "04 Eki Per", checkIn: "--:--", checkOut: "--:--", break: "-", total: "00:00", type: "Rapor", status: "absent" },
    { date: "05 Eki Cum", checkIn: "09:00", checkOut: "18:00", break: "1s", total: "08:00", type: "Tam", status: "ok" },
  ];

  const getStatusLabel = (status) =>
    ({
      ontime: "Zamanında",
      late: "Geç kaldı",
      absent: "Gelmedi",
      early: "Erken giriş",
    }[status] || status);

  const renderLiveCards = () =>
    dailyMoves
      .map(
        (move) => `
          <div class="live-card ${move.status}">
            <div class="lc-header">
              <span class="lc-badge ${move.status}">${getStatusLabel(move.status)}</span>
              <span class="lc-time"><i aria-hidden="true" class="fa-regular fa-clock"></i> ${move.time}</span>
            </div>
            <div class="lc-body">
              <div class="lc-avatar">${move.avatar}</div>
              <div>
                <h4>${move.name}</h4>
                <p>${move.dept}</p>
              </div>
            </div>
          </div>
        `
      )
      .join("");

  const renderTimesheet = () =>
    timesheetData
      .map(
        (row) => `
          <tr>
            <td><span class="date-cell">${row.date}</span></td>
            <td>
              ${
                row.type === "Tam"
                  ? '<span class="type-badge reg">Normal</span>'
                  : row.type === "Mesai"
                  ? '<span class="type-badge over">Fazla mesai</span>'
                  : '<span class="type-badge abs">İzin/Rapor</span>'
              }
            </td>
            <td class="mono">${row.checkIn}</td>
            <td class="mono">${row.checkOut}</td>
            <td class="mono">${row.break}</td>
            <td><strong class="mono">${row.total}</strong></td>
            <td>
              ${
                row.status === "late"
                  ? '<span class="warn-text"><i aria-hidden="true" class="fa-solid fa-triangle-exclamation"></i> Geç</span>'
                  : row.status === "overtime"
                  ? '<span class="success-text"><i aria-hidden="true" class="fa-solid fa-star"></i> +2s mesai</span>'
                  : row.status === "absent"
                  ? '<span class="danger-text">Gelmedi</span>'
                  : '<span class="ok-text"><i aria-hidden="true" class="fa-solid fa-check"></i> Uygun</span>'
              }
            </td>
            <td class="text-right csv-skip"><button class="btn-icon-sm" title="Kaydı düzenle" aria-label="${row.date} puantaj kaydını düzenle" onclick="showToast('${row.date} kaydı düzenleme için açıldı.', 'info')"><i aria-hidden="true" class="fa-solid fa-pen"></i></button></td>
          </tr>
        `
      )
      .join("");

  return `
    <div id="attendance-screen">
      <div class="page-header">
        <div>
          <h2>Mesai & Puantaj Takibi</h2>
          <p>Giriş-çıkış saatleri, vardiya durumu ve aylık puantaj kontrolü.</p>
        </div>
        <div class="header-actions">
          <div class="date-picker-wrapper">
            <button class="btn-icon" aria-label="Önceki ay" title="Önceki ay" onclick="changeAttendanceMonth(-1)"><i aria-hidden="true" class="fa-solid fa-chevron-left"></i></button>
            <span class="current-month"><i aria-hidden="true" class="fa-solid fa-calendar-days"></i> <span id="attendance-month">Ekim 2025</span></span>
            <button class="btn-icon" aria-label="Sonraki ay" title="Sonraki ay" onclick="changeAttendanceMonth(1)"><i aria-hidden="true" class="fa-solid fa-chevron-right"></i></button>
          </div>
          <button class="btn btn-primary" onclick="exportTableToCSV('.att-table', 'puantaj-raporu')">
            <i aria-hidden="true" class="fa-solid fa-file-export"></i> Rapor Al
          </button>
        </div>
      </div>

      <div class="stats-stripe">
        <div class="stat-box"><span class="sb-label">Toplam Çalışma</span><strong class="sb-val">1,240 <small>saat</small></strong></div>
        <div class="stat-box"><span class="sb-label">Fazla Mesai</span><strong class="sb-val text-orange">42 <small>saat</small></strong></div>
        <div class="stat-box"><span class="sb-label">Geç Kalma</span><strong class="sb-val text-red">12 <small>kişi</small></strong></div>
        <div class="stat-box"><span class="sb-label">Devamsızlık</span><strong class="sb-val">3 <small>gün</small></strong></div>
      </div>

      <div class="att-content surface">
        <div class="att-tabs">
          <button class="att-tab active" onclick="switchAttTab('live-view', event)">
            <i aria-hidden="true" class="fa-solid fa-video"></i> Canlı İzleme
          </button>
          <button class="att-tab" onclick="switchAttTab('timesheet-view', event)">
            <i aria-hidden="true" class="fa-solid fa-table"></i> Aylık Puantaj
          </button>
        </div>

        <div id="live-view" class="att-section active">
          <div class="live-grid">
            ${renderLiveCards()}
            <div class="live-card ghost">
              <i aria-hidden="true" class="fa-solid fa-plus"></i>
              <span>Manuel giriş ekle</span>
            </div>
          </div>
        </div>

        <div id="timesheet-view" class="att-section">
          <div class="ts-filter">
            <select class="user-select">
              <option>Ahmet Yılmaz (Yazılım)</option>
              <option>Selin Koç (Tasarım)</option>
            </select>
            <div class="legend">
              <span class="leg-item"><span class="dot ok"></span> Normal</span>
              <span class="leg-item"><span class="dot warn"></span> Geç/Erken</span>
              <span class="leg-item"><span class="dot danger"></span> Eksik</span>
            </div>
          </div>

          <div class="table-wrapper">
            <table class="att-table">
              <thead>
                <tr>
                  <th>Tarih</th><th>Tip</th><th>Giriş</th><th>Çıkış</th><th>Mola</th><th>Net Süre</th><th>Durum</th><th></th>
                </tr>
              </thead>
              <tbody>${renderTimesheet()}</tbody>
              <tfoot>
                <tr>
                  <td colspan="5" class="text-right"><strong>Aylık Toplam:</strong></td>
                  <td><strong class="text-blue mono">42:00</strong></td>
                  <td colspan="2"></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>
    </div>
  `;
};

const attendanceMonths = ["Ağustos 2025", "Eylül 2025", "Ekim 2025", "Kasım 2025", "Aralık 2025"];
let attendanceMonthIndex = 2;

window.changeAttendanceMonth = (step) => {
  attendanceMonthIndex = Math.min(
    Math.max(attendanceMonthIndex + step, 0),
    attendanceMonths.length - 1
  );
  const label = document.getElementById("attendance-month");
  if (label) label.textContent = attendanceMonths[attendanceMonthIndex];
};

window.switchAttTab = (tabId, evt) => {
  document.querySelectorAll(".att-tab").forEach((button) => button.classList.remove("active"));
  if (evt?.currentTarget) evt.currentTarget.classList.add("active");

  document.querySelectorAll(".att-section").forEach((section) => section.classList.remove("active"));
  document.getElementById(tabId).classList.add("active");
};
