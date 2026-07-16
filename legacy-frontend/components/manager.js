const ManagerDashboard = () => {
  setTimeout(() => initProCharts(), 200);

  return `
    <div id="manager-screen">
      <div class="manager-header page-header">
        <div>
          <h2>Yönetici Konsolu</h2>
          <p>Şirket geneli izin analitikleri, yoğunluk görünümü ve onay işlemleri.</p>
        </div>
        <div class="mh-actions header-actions">
          <select class="filter-select"><option>Bu Ay (Ekim)</option><option>Geçen Ay</option><option>Yıllık</option></select>
          <select class="filter-select"><option>Tüm Departmanlar</option><option>Yazılım</option><option>İK</option></select>
          <button class="btn btn-secondary" onclick="exportTableToCSV('.mini-table', 'departman-kullanim-raporu')">
            <i aria-hidden="true" class="fa-solid fa-cloud-arrow-down"></i> Raporu İndir
          </button>
        </div>
      </div>

      <div class="stats-grid-pro">
        <div class="kpi-pro theme-blue">
          <div class="kpi-top"><div class="kpi-icon-box"><i aria-hidden="true" class="fa-solid fa-users-viewfinder"></i></div><span class="kpi-trend">+%12</span></div>
          <span class="kpi-val">8 kişi</span>
          <span class="kpi-label">Şu An İzinli</span>
        </div>
        <div class="kpi-pro theme-orange">
          <div class="kpi-top"><div class="kpi-icon-box"><i aria-hidden="true" class="fa-regular fa-folder-open"></i></div><span class="kpi-trend text-orange">Kritik</span></div>
          <span class="kpi-val">5 talep</span>
          <span class="kpi-label">Onay Bekliyor</span>
        </div>
        <div class="kpi-pro theme-purple">
          <div class="kpi-top"><div class="kpi-icon-box"><i aria-hidden="true" class="fa-solid fa-briefcase"></i></div><span class="kpi-trend">+%5</span></div>
          <span class="kpi-val">142 gün</span>
          <span class="kpi-label">Planlanan İzin</span>
        </div>
        <div class="kpi-pro theme-green">
          <div class="kpi-top"><div class="kpi-icon-box"><i aria-hidden="true" class="fa-solid fa-wallet"></i></div></div>
          <span class="kpi-val">%42</span>
          <span class="kpi-label">İzin Kullanım Oranı</span>
        </div>
      </div>

      <div class="dashboard-grid">
        <div class="chart-section card">
          <div class="section-head">
            <div>
              <h3>İzin Kullanım Trendleri</h3>
              <span>Departman ve ay bazlı görünüm</span>
            </div>
            <select class="filter-select"><option>İzin türüne göre</option></select>
          </div>
          <div class="chart-container manager-chart">
            <canvas id="proTrendChart"></canvas>
          </div>

          <div class="section-head heat-title">
            <h3>Departman Yoğunluk Haritası</h3>
          </div>
          <div class="heatmap-container">
            <div class="heatmap-row"><span class="hm-label">Yazılım</span><div class="hm-bars"><div class="hm-cell hm-l1"></div><div class="hm-cell hm-l2"></div><div class="hm-cell hm-l4"></div><div class="hm-cell hm-l3"></div><div class="hm-cell hm-l1"></div></div></div>
            <div class="heatmap-row"><span class="hm-label">Satış</span><div class="hm-bars"><div class="hm-cell hm-l2"></div><div class="hm-cell hm-l1"></div><div class="hm-cell hm-l1"></div><div class="hm-cell hm-l4"></div><div class="hm-cell hm-l2"></div></div></div>
            <div class="heatmap-row"><span class="hm-label">İK</span><div class="hm-bars"><div class="hm-cell hm-l1"></div><div class="hm-cell hm-l3"></div><div class="hm-cell hm-l2"></div><div class="hm-cell hm-l1"></div><div class="hm-cell hm-l1"></div></div></div>
          </div>
        </div>

        <div class="approval-panel card">
          <div class="panel-head">
            <h3>Onay Bekleyenler</h3>
            <span class="status-pill pending">5 yeni</span>
          </div>
          <div class="req-list">
            <div class="req-item">
              <div class="req-user">
                <div class="req-avatar">AY</div>
                <div class="req-meta"><h4>Ahmet Yılmaz</h4><span>Senior Developer • Yazılım</span></div>
              </div>
              <div class="req-details">
                <div><span>İzin Türü</span><strong>Yıllık İzin</strong></div>
                <div><span>Süre</span><strong>5 gün</strong></div>
                <div><span>Tarihler</span><strong>12 Ağustos - 17 Ağustos</strong></div>
              </div>
              <div class="req-actions">
                <button class="btn btn-secondary btn-sm" onclick="resolveApprovalRequest(this, false)">Reddet</button>
                <button class="btn btn-primary btn-sm" onclick="resolveApprovalRequest(this, true)">Onayla</button>
              </div>
            </div>
            <div class="req-item">
              <div class="req-user">
                <div class="req-avatar">SK</div>
                <div class="req-meta"><h4>Selin Koç</h4><span>UI Designer • Tasarım</span></div>
              </div>
              <div class="req-details">
                <div><span>İzin Türü</span><strong>Raporlu</strong></div>
                <div><span>Süre</span><strong>2 gün</strong></div>
              </div>
              <div class="req-actions">
                <button class="btn btn-secondary btn-sm" onclick="resolveApprovalRequest(this, false)">Reddet</button>
                <button class="btn btn-primary btn-sm" onclick="resolveApprovalRequest(this, true)">Onayla</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="table-section">
        <div class="section-head manager-table-head">
          <h3>Departman Bazlı Kullanım Raporu</h3>
        </div>
        <table class="mini-table">
          <thead><tr><th>Departman</th><th>Toplam Personel</th><th>Kullanılan İzin</th><th>Kalan Hak</th><th>Doluluk</th></tr></thead>
          <tbody>
            <tr><td><strong>Yazılım Ekibi</strong></td><td>42 kişi</td><td>120 gün</td><td>340 gün</td><td><div class="progress-mini"><div class="p-fill" style="width:35%"></div></div></td></tr>
            <tr><td><strong>Satış & Pazarlama</strong></td><td>18 kişi</td><td>45 gün</td><td>120 gün</td><td><div class="progress-mini"><div class="p-fill" style="width:28%"></div></div></td></tr>
          </tbody>
        </table>
      </div>
    </div>
  `;
};

window.resolveApprovalRequest = (button, approved) => {
  const item = button.closest(".req-item");
  const name = item?.querySelector("h4")?.textContent || "Talep";
  item?.remove();
  showToast(
    approved ? `${name} talebi onaylandı.` : `${name} talebi reddedildi.`,
    approved ? "success" : "info"
  );

  const pill = document.querySelector(".approval-panel .status-pill");
  const remaining = document.querySelectorAll(".approval-panel .req-item").length;
  if (pill) pill.textContent = remaining ? `${remaining} bekleyen` : "Tamamlandı";
};

window.initProCharts = () => {
  if (typeof Chart === "undefined") {
    const canvas = document.getElementById("proTrendChart");
    if (canvas) {
      canvas.outerHTML = '<div class="chart-fallback"><i aria-hidden="true" class="fa-solid fa-chart-line"></i><span>Grafik önizlemesi yüklenemedi</span></div>';
    }
    return;
  }

  const ctx = document.getElementById("proTrendChart");
  if (ctx) {
    if (Chart.getChart("proTrendChart")) Chart.getChart("proTrendChart").destroy();
    const managerChartToken = (name, fallback) =>
      getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
    const gradient = ctx.getContext("2d").createLinearGradient(0, 0, 0, 300);
    gradient.addColorStop(0, "rgba(15, 118, 110, 0.22)");
    gradient.addColorStop(1, "rgba(15, 118, 110, 0)");

    new Chart(ctx, {
      type: "line",
      data: {
        labels: ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"],
        datasets: [
          {
            label: "Toplam izin günü",
            data: [12, 15, 8, 10, 25, 45, 80, 120, 30, 15, 10, 20],
            borderColor: managerChartToken("--primary", "#0f766e"),
            borderWidth: 2.5,
            backgroundColor: gradient,
            fill: true,
            tension: 0.35,
            pointBackgroundColor: managerChartToken("--surface", "#ffffff"),
            pointBorderColor: managerChartToken("--primary", "#0f766e"),
            pointRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { mode: "index", intersect: false } },
        scales: {
          y: { grid: { color: managerChartToken("--line-soft", "#e9efef") }, beginAtZero: true },
          x: { grid: { display: false } },
        },
      },
    });
  }
};
