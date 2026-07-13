const getDashboardMetrics = () => {
  const employees = [
    {
      name: "Ahmet Yılmaz",
      title: "Senior Developer",
      dept: "Yazılım",
      manager: "Ece Arslan",
      absence: 18,
      lateness: 22,
      overtime: 74,
      unusedLeave: 82,
      pulse: 52,
      performance: 68,
      roleCriticality: 92,
      trend: "Son 30 günde fazla mesai +%18",
      action: "1:1 görüşme ve izin planı başlat",
    },
    {
      name: "Selin Koç",
      title: "UI Designer",
      dept: "Tasarım",
      manager: "Ece Arslan",
      absence: 8,
      lateness: 12,
      overtime: 66,
      unusedLeave: 71,
      pulse: 61,
      performance: 74,
      roleCriticality: 76,
      trend: "İzin erteleme ve teslim baskısı yükseldi",
      action: "Sprint kapasitesi ve izin penceresi netleştir",
    },
    {
      name: "Burak Demir",
      title: "Satış Temsilcisi",
      dept: "Satış",
      manager: "Murat Kaya",
      absence: 28,
      lateness: 31,
      overtime: 58,
      unusedLeave: 48,
      pulse: 49,
      performance: 62,
      roleCriticality: 69,
      trend: "Geç kalma ve kısa rapor paterni oluştu",
      action: "Hedef baskısı ve vardiya düzeni incelensin",
    },
    {
      name: "Ayşe Vural",
      title: "İK Uzmanı",
      dept: "İK",
      manager: "Deniz Şahin",
      absence: 6,
      lateness: 9,
      overtime: 34,
      unusedLeave: 42,
      pulse: 78,
      performance: 84,
      roleCriticality: 58,
      trend: "Stabil bağlılık ve dengeli kapasite",
      action: "Standart takip yeterli",
    },
    {
      name: "Mert Can",
      title: "DevOps Engineer",
      dept: "Operasyon",
      manager: "Deniz Şahin",
      absence: 12,
      lateness: 17,
      overtime: 69,
      unusedLeave: 77,
      pulse: 57,
      performance: 72,
      roleCriticality: 88,
      trend: "Kritik rol bağımlılığı ve nöbet yoğunluğu arttı",
      action: "Yedekleme planı ve nöbet rotasyonu oluştur",
    },
  ];

  const riskScoreFor = (employee) =>
    Math.round(
      employee.absence * 0.18 +
        employee.lateness * 0.14 +
        employee.overtime * 0.2 +
        employee.unusedLeave * 0.15 +
        (100 - employee.pulse) * 0.18 +
        (100 - employee.performance) * 0.15
    );

  const enrichedEmployees = employees.map((employee) => ({
    ...employee,
    riskScore: riskScoreFor(employee),
    attritionRisk:
      employee.pulse < 55 || employee.roleCriticality > 85
        ? "high"
        : employee.pulse < 65 || employee.roleCriticality > 75
        ? "medium"
        : "low",
    burnoutRisk:
      employee.overtime > 65 && employee.unusedLeave > 65
        ? "high"
        : employee.overtime > 55 || employee.unusedLeave > 65
        ? "medium"
        : "low",
  }));

  const riskScore = Math.round(
    enrichedEmployees.reduce((total, employee) => total + employee.riskScore, 0) /
      enrichedEmployees.length
  );

  const employeeVoiceMetrics = {
    pulseScore: 64,
    eNps: 8,
    participationRate: 76,
    decliningTeams: 2,
    sentimentTrend: "Son 3 nabız ölçümünde bağlılık 7 puan geriledi",
    departments: [
      { dept: "Yazılım", pulse: 58, eNps: 2, participation: 82, mood: "Baskı yüksek", driver: "Teslim takvimi ve fazla mesai", level: "high" },
      { dept: "Satış", pulse: 61, eNps: 5, participation: 74, mood: "Kırılgan", driver: "Hedef baskısı ve vardiya düzeni", level: "medium" },
      { dept: "Operasyon", pulse: 63, eNps: 7, participation: 71, mood: "Yoğun", driver: "Nöbet rotasyonu ve kritik rol bağımlılığı", level: "medium" },
      { dept: "Tasarım", pulse: 69, eNps: 14, participation: 78, mood: "Dengeli", driver: "Teslim baskısı izleniyor", level: "low" },
      { dept: "İK", pulse: 78, eNps: 24, participation: 88, mood: "Pozitif", driver: "Net iletişim ve dengeli kapasite", level: "low" },
    ],
    riskTeams: [
      { team: "Yazılım Platform", owner: "Ece Arslan", signal: "Sessiz memnuniyetsizlik", reason: "Nabız skoru düştü, fazla mesai yorumu arttı", action: "Yönetici ile 1:1 görüşme ritmi başlat", level: "high" },
      { team: "Saha Satış", owner: "Murat Kaya", signal: "Bağlılık kırılması", reason: "eNPS düşük, hedef baskısı geri bildirimi yükseldi", action: "Ekip içi iş yükü ve hedef dağılımı kontrolü yap", level: "medium" },
      { team: "Operasyon Nöbet", owner: "Deniz Şahin", signal: "Takip önerisi", reason: "Katılım oranı düşük, nöbet yorumu artıyor", action: "Takip anketi ve nöbet rotasyonu planla", level: "medium" },
    ],
    signals: [
      "Yazılım ekibinde fazla mesai kaynaklı negatif yorumlar arttı.",
      "Satış ekibinde hedef baskısı ve adalet algısı birlikte izlenmeli.",
      "Operasyon ekibinde katılım oranı düştüğü için takip anketi öneriliyor.",
    ],
    recommendedActions: [
      "Yönetici ile 1:1 görüşme başlat",
      "Ekip içi iş yükü kontrolü yap",
      "Takip anketi planla",
    ],
  };

  const complianceMetrics = {
    documentComplianceScore: 82,
    missingDocuments: 11,
    upcomingDocuments: 18,
    auditReadinessRisk: "Orta",
    auditReadinessScore: 74,
    records: [
      { employee: "Ahmet Yılmaz", dept: "Yazılım", document: "İSG yenileme formu", owner: "İK Operasyon", dueDate: "3 gün", status: "Süresi Yaklaşıyor", level: "medium" },
      { employee: "Burak Demir", dept: "Satış", document: "KVKK açık rıza eki", owner: "İK Operasyon", dueDate: "Bugün", status: "Eksik", level: "high" },
      { employee: "Mert Can", dept: "Operasyon", document: "Görev tanımı onayı", owner: "Deniz Şahin", dueDate: "5 gün", status: "İncelemede", level: "medium" },
      { employee: "Selin Koç", dept: "Tasarım", document: "Yan hak bilgilendirme formu", owner: "İK Operasyon", dueDate: "9 gün", status: "Süresi Yaklaşıyor", level: "low" },
      { employee: "Ayşe Vural", dept: "İK", document: "Personel dosyası kontrolü", owner: "İK Operasyon", dueDate: "Tamamlandı", status: "Tamamlandı", level: "low" },
    ],
    deadlines: [
      { title: "KVKK açık rıza eki", count: 4, dueDate: "Bugün", owner: "İK Operasyon", level: "high" },
      { title: "İSG yenileme formları", count: 7, dueDate: "3 gün", owner: "İSG Sorumlusu", level: "medium" },
      { title: "Görev tanımı onayları", count: 5, dueDate: "5 gün", owner: "Yöneticiler", level: "medium" },
      { title: "Yan hak bilgilendirmeleri", count: 2, dueDate: "9 gün", owner: "İK Operasyon", level: "low" },
    ],
    auditChecklist: [
      { label: "Personel dosyası bütünlüğü", value: 84, level: "medium" },
      { label: "Süresi yaklaşan evrak kontrolü", value: 68, level: "high" },
      { label: "Sorumlu atama netliği", value: 79, level: "medium" },
      { label: "Denetim klasörü hazırlığı", value: 72, level: "medium" },
    ],
    recommendedActions: [
      "Eksik KVKK eklerini bugün kapat",
      "İSG yenilemeleri için sorumlu ataması yap",
      "Denetim klasörü kontrol listesini haftalık takip et",
    ],
  };

  const actions = [
    {
      priority: "Bugün müdahale et",
      title: "Yazılım ekibinde tükenmişlik sinyali yükseldi",
      owner: "Ece Arslan",
      reason: "Fazla mesai ve kullanılmayan izin birlikte artıyor.",
      action: "Ekip lideriyle kapasite görüşmesi planla.",
      level: "high",
      route: "burnout-risk",
    },
    {
      priority: "Bugün müdahale et",
      title: "2 kritik rolde ayrılma riski var",
      owner: "İK Yöneticisi",
      reason: "Nabız skoru düştü, rol kritiklik seviyesi yüksek.",
      action: "1:1 görüşme ve yedekleme planı başlat.",
      level: "high",
      route: "attrition-risk",
    },
    {
      priority: "Bu hafta takip et",
      title: "Satış ekibinde devamsızlık paterni oluşuyor",
      owner: "Murat Kaya",
      reason: "Son 30 günde geç kalma ve kısa raporlar arttı.",
      action: "Vardiya ve hedef baskısı birlikte incelensin.",
      level: "medium",
      route: "burnout-risk",
    },
    {
      priority: "Bu hafta takip et",
      title: "Yönetici yükü iki ekipte kritik eşiğe yaklaştı",
      owner: "Deniz Şahin",
      reason: "Onay bekleyenler, açık aksiyonlar ve ekip nabzı birlikte izlenmeli.",
      action: "Aksiyonları delege et ve haftalık takip ritmi kur.",
      level: "medium",
      route: "manager-load",
    },
    {
      priority: "Bu hafta takip et",
      title: "Çalışan nabzında 2 ekipte düşüş sinyali var",
      owner: "İK Business Partner",
      reason: "Yazılım ve Satış ekiplerinde eNPS ve yorum tonu birlikte geriliyor.",
      action: "Yönetici 1:1 ritmi ve kısa takip anketi planla.",
      level: "medium",
      route: "employee-voice",
    },
    {
      priority: "Bugün müdahale et",
      title: "KVKK ve İSG evraklarında kritik son tarih var",
      owner: "İK Operasyon",
      reason: "11 eksik evrak ve bugün kapanması gereken 4 KVKK eki bulunuyor.",
      action: "Eksik evrak sahiplerini netleştir ve gün sonu kontrolü yap.",
      level: "high",
      route: "compliance-risk",
    },
    {
      priority: "İzlemede kalsın",
      title: "İşe alım hunisinde teklif kabul oranı düşüyor",
      owner: "İşe Alım",
      reason: "Tekliften kabul aşamasına geçiş %62 seviyesinde.",
      action: "Teklif paketi ve aday deneyimi gözden geçirilsin.",
      level: "low",
      route: "recruitment",
    },
  ];

  return {
    riskScore,
    managerLoadIndex: 71,
    attritionHigh: enrichedEmployees.filter((employee) => employee.attritionRisk === "high").length,
    burnoutRisk: enrichedEmployees.filter((employee) => employee.burnoutRisk === "high").length,
    criticalActions: actions.length,
    pulseScore: employeeVoiceMetrics.pulseScore,
    hiringHealth: 72,
    skillGap: 38,
    criticalRoleRisk: 3,
    employeeVoiceMetrics,
    complianceMetrics,
    employees: enrichedEmployees,
    managers: [
      { name: "Ece Arslan", team: 18, approvals: 5, actions: 7, overtime: 68, pulse: 59, load: 78 },
      { name: "Murat Kaya", team: 14, approvals: 3, actions: 5, overtime: 52, pulse: 61, load: 64 },
      { name: "Deniz Şahin", team: 21, approvals: 6, actions: 8, overtime: 58, pulse: 66, load: 73 },
      { name: "Can Uslu", team: 9, approvals: 1, actions: 2, overtime: 34, pulse: 76, load: 38 },
    ],
    riskTrend: [48, 51, 49, 54, 57, 59, 62, 64, 61, 67, 69, riskScore],
    departmentRisk: [
      { dept: "Yazılım", risk: 74, drivers: ["Fazla mesai", "Kritik rol bağımlılığı", "Kullanılmayan izin"] },
      { dept: "Satış", risk: 68, drivers: ["Geç kalma artışı", "Nabız düşüşü", "Açık aksiyon"] },
      { dept: "Tasarım", risk: 56, drivers: ["Yönetici yükü", "Teslim baskısı", "İzin erteleme"] },
      { dept: "İK", risk: 34, drivers: ["Düşük risk", "Stabil bağlılık", "Dengeli kapasite"] },
      { dept: "Operasyon", risk: 61, drivers: ["Vardiya yoğunluğu", "Rapor artışı", "Yedekleme eksiği"] },
    ],
    actions,
    talentCapacity: [
      { label: "İşe Alım Sağlığı", value: 72, meta: "8 açık pozisyon, 41 gün ortalama kapanış", tone: "medium" },
      { label: "Beceri Açığı", value: 38, meta: "Veri analitiği ve ekip liderliği öne çıkıyor", tone: "high" },
      { label: "Kritik Rol Riski", value: 3, meta: "Yedekleme planı olmayan kritik rol", tone: "high" },
      { label: "Kültür / Nabız", value: 68, meta: "Mini eNPS +12, yönetici iletişimi izleniyor", tone: "medium" },
    ],
  };
};

const getRiskLevel = (score) => {
  if (score >= 70) return "high";
  if (score >= 55) return "medium";
  return "low";
};

const getRiskLabel = (score) => {
  if (score >= 70) return "Yüksek risk";
  if (score >= 55) return "Orta risk";
  return "Kontrollü";
};

const getLevelText = (level) =>
  ({ high: "Yüksek", medium: "Orta", low: "Düşük" }[level] || "İzlemede");

// Tek aksiyon kaynağı ilkesi: dashboard yalnızca en acil N aksiyonu özetler,
// tam liste Global Aksiyon Merkezi'nde (/actions) yaşar.
const getTopPriorityActions = (actions, count = 3) => {
  const order = { high: 0, medium: 1, low: 2 };
  return [...actions]
    .sort((a, b) => (order[a.level] ?? 3) - (order[b.level] ?? 3))
    .slice(0, count);
};

const renderBackToRisk = () => `
  <button class="btn btn-secondary btn-sm" onclick="navigateTo('dashboard', event)">
    <i aria-hidden="true" class="fa-solid fa-arrow-left"></i> Risk Merkezi
  </button>
`;

const Dashboard = () => {
  const metrics = getDashboardMetrics();
  setTimeout(() => initDashboardCharts(metrics), 100);

  return `
    <div class="dashboard-wrapper">
      <div class="welcome-header">
        <div>
          <h2>Risk & Aksiyon Merkezi</h2>
          <p class="text-muted">Bugünün odak sorusu: hangi risk büyüyor, neden büyüyor ve hangi aksiyon alınmalı?</p>
        </div>
        <div class="date-widget">
          <i aria-hidden="true" class="fa-regular fa-calendar"></i>
          <span>${new Date().toLocaleDateString("tr-TR", {
            weekday: "long",
            year: "numeric",
            month: "long",
            day: "numeric",
          })}</span>
        </div>
      </div>

      <div class="kpi-grid intelligence-kpis">
        <button class="kpi-card risk-kpi ${getRiskLevel(metrics.riskScore)} metric-card-button" onclick="navigateTo('actions', event)">
          <div class="kpi-icon bg-red-light"><i aria-hidden="true" class="fa-solid fa-shield-heart"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">İK Risk Skoru</span>
            <h3 class="kpi-value">${metrics.riskScore}<small>/100</small></h3>
            <span class="kpi-sub">${getRiskLabel(metrics.riskScore)} · 3 ana risk sürücüsü</span>
          </div>
        </button>
        <button class="kpi-card risk-kpi medium metric-card-button" onclick="navigateTo('manager-load', event)">
          <div class="kpi-icon bg-orange-light"><i aria-hidden="true" class="fa-solid fa-user-group"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Yönetici Yük Endeksi</span>
            <h3 class="kpi-value">${metrics.managerLoadIndex}<small>/100</small></h3>
            <span class="kpi-sub">5 onay, 14 açık aksiyon, yoğun ekip</span>
          </div>
        </button>
        <button class="kpi-card risk-kpi high metric-card-button" onclick="navigateTo('attrition-risk', event)">
          <div class="kpi-icon bg-purple-light"><i aria-hidden="true" class="fa-solid fa-person-walking-arrow-right"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Ayrılma Riski Radarı</span>
            <h3 class="kpi-value">${metrics.attritionHigh}</h3>
            <span class="kpi-sub">Yüksek riskli çalışan segmenti</span>
          </div>
        </button>
        <button class="kpi-card risk-kpi medium metric-card-button" onclick="navigateTo('actions', event)">
          <div class="kpi-icon bg-blue-light"><i aria-hidden="true" class="fa-solid fa-bolt"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Bugünkü Kritik Aksiyon</span>
            <h3 class="kpi-value">${metrics.criticalActions}</h3>
            <span class="kpi-sub">${metrics.actions.filter((action) => action.priority === "Bugün müdahale et").length} bugün, ${metrics.actions.filter((action) => action.priority === "Bu hafta takip et").length} bu hafta, ${metrics.actions.filter((action) => action.priority === "İzlemede kalsın").length} izlemede</span>
          </div>
        </button>
      </div>

      <div class="risk-dashboard-grid">
        <div class="risk-main">
          <div class="risk-decision-grid">
            <div class="card chart-card">
              <div class="card-header-clean">
                <div>
                  <h4>90 Günlük Risk Trendi</h4>
                  <p class="text-muted">Risk skoru son 12 hafta içinde yukarı yönlü seyrediyor.</p>
                </div>
                <span class="status-pill pending">+8 puan</span>
              </div>
              <div class="chart-container">
                <canvas id="riskTrendChart"></canvas>
              </div>
            </div>

            <div class="card heatmap-card">
              <div class="card-header-clean">
                <div>
                  <h4>Departman Bazlı Risk Isı Haritası</h4>
                  <p class="text-muted">Risk seviyesi ve öne çıkan nedenler.</p>
                </div>
              </div>
              <div class="risk-heatmap">
                ${metrics.departmentRisk
                  .map(
                    (dept) => `
                      <div class="heatmap-line ${getRiskLevel(dept.risk)}">
                        <div class="heatmap-score">${dept.risk}</div>
                        <div class="heatmap-body">
                          <strong>${dept.dept}</strong>
                          <span>${dept.drivers.join(" · ")}</span>
                        </div>
                        <div class="heatmap-bar"><span style="width:${dept.risk}%"></span></div>
                      </div>
                    `
                  )
                  .join("")}
              </div>
            </div>
          </div>

          <div class="card capacity-card">
            <div class="card-header-clean">
              <div>
                <h4>Yetenek ve Kapasite</h4>
                <p class="text-muted">İşe alım, beceri, kritik rol ve kültür sinyalleri.</p>
              </div>
              <button class="btn btn-secondary btn-sm" onclick="navigateTo('recruitment', event)">
                <i aria-hidden="true" class="fa-solid fa-arrow-up-right-from-square"></i> İşe alıma git
              </button>
            </div>
            <div class="capacity-grid">
              ${metrics.talentCapacity
                .map(
                  (item) => `
                    <div class="capacity-item ${item.tone}">
                      <div class="capacity-top">
                        <span>${item.label}</span>
                        <strong>${item.value}${item.label === "Kritik Rol Riski" ? "" : "%"}</strong>
                      </div>
                      <div class="progress-bar"><div class="fill" style="width:${item.label === "Kritik Rol Riski" ? item.value * 22 : item.value}%"></div></div>
                      <small>${item.meta}</small>
                    </div>
                  `
                )
                .join("")}
            </div>
          </div>

          <div class="card signal-card">
            <div class="card-header-clean">
              <div>
                <h4>Kurumsal Sinyaller</h4>
                <p class="text-muted">Çalışan nabzı, sessiz memnuniyetsizlik ve evrak uyum risklerini birlikte izleyin.</p>
              </div>
              <div class="toolbar-actions">
                <button class="btn btn-secondary btn-sm" onclick="navigateTo('employee-voice', event)">
                  <i aria-hidden="true" class="fa-solid fa-wave-square"></i> Nabız detayı
                </button>
                <button class="btn btn-secondary btn-sm" onclick="navigateTo('compliance-risk', event)">
                  <i aria-hidden="true" class="fa-solid fa-file-shield"></i> Uyum detayı
                </button>
              </div>
            </div>
            <div class="signal-grid">
              <button class="signal-item high metric-card-button" onclick="navigateTo('employee-voice', event)">
                <div class="signal-icon"><i aria-hidden="true" class="fa-solid fa-heart-pulse"></i></div>
                <div>
                  <span>Çalışan Nabız Skoru</span>
                  <strong>${metrics.employeeVoiceMetrics.pulseScore}<small>/100</small></strong>
                  <p>${metrics.employeeVoiceMetrics.sentimentTrend}</p>
                </div>
              </button>
              <button class="signal-item medium metric-card-button" onclick="navigateTo('employee-voice', event)">
                <div class="signal-icon"><i aria-hidden="true" class="fa-solid fa-comments"></i></div>
                <div>
                  <span>eNPS / Bağlılık</span>
                  <strong>+${metrics.employeeVoiceMetrics.eNps}</strong>
                  <p>${metrics.employeeVoiceMetrics.decliningTeams} ekipte sessiz memnuniyetsizlik sinyali var.</p>
                </div>
              </button>
              <button class="signal-item medium metric-card-button" onclick="navigateTo('compliance-risk', event)">
                <div class="signal-icon"><i aria-hidden="true" class="fa-solid fa-folder-check"></i></div>
                <div>
                  <span>Evrak Uyum Skoru</span>
                  <strong>${metrics.complianceMetrics.documentComplianceScore}<small>/100</small></strong>
                  <p>${metrics.complianceMetrics.missingDocuments} eksik evrak, ${metrics.complianceMetrics.upcomingDocuments} yaklaşan son tarih.</p>
                </div>
              </button>
              <button class="signal-item medium metric-card-button" onclick="navigateTo('compliance-risk', event)">
                <div class="signal-icon"><i aria-hidden="true" class="fa-solid fa-clipboard-check"></i></div>
                <div>
                  <span>Denetime Hazırlık</span>
                  <strong>${metrics.complianceMetrics.auditReadinessRisk}</strong>
                  <p>Hazırlık skoru ${metrics.complianceMetrics.auditReadinessScore}/100; kritik evrak aksiyonları izleniyor.</p>
                </div>
              </button>
            </div>
          </div>
        </div>

        <aside class="card action-center">
          <div class="card-header-clean">
            <div>
              <h4>En Acil Aksiyonlar</h4>
              <p class="text-muted">Önceliği en yüksek 3 müdahale.</p>
            </div>
            <button class="btn btn-secondary btn-sm" onclick="navigateTo('actions', event)">
              Tümünü aç (${metrics.actions.length})
            </button>
          </div>
          <div class="action-list">
            ${getTopPriorityActions(metrics.actions, 3)
              .map(
                (action) => `
                  <button class="risk-action ${action.level}" onclick="navigateTo('${action.route}', event)">
                    <div class="action-priority">${action.priority}</div>
                    <strong>${action.title}</strong>
                    <p>${action.reason}</p>
                    <div class="recommended-action">
                      <i aria-hidden="true" class="fa-solid fa-lightbulb"></i>
                      <span>${action.action}</span>
                    </div>
                  </button>
                `
              )
              .join("")}
          </div>
        </aside>
      </div>
    </div>
  `;
};

const OverviewDashboard = () => {
  setTimeout(() => initDashboardCharts(), 100);

  return `
    <div class="dashboard-wrapper">
      <div class="welcome-header">
        <div>
          <h2>Genel Durum</h2>
          <p class="text-muted">Şirkette bugün ne oluyor? Operasyonel İK görünümünü hızlıca tarayın.</p>
        </div>
        <div class="date-widget">
          <i aria-hidden="true" class="fa-regular fa-calendar"></i>
          <span>${new Date().toLocaleDateString("tr-TR", {
            weekday: "long",
            year: "numeric",
            month: "long",
            day: "numeric",
          })}</span>
        </div>
      </div>

      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-icon bg-blue-light"><i aria-hidden="true" class="fa-solid fa-users"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Aktif Personel</span>
            <h3 class="kpi-value">142</h3>
            <span class="kpi-trend"><i aria-hidden="true" class="fa-solid fa-arrow-trend-up"></i> Bu çeyrek %12 artış</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon bg-orange-light"><i aria-hidden="true" class="fa-solid fa-file-signature"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Onay Bekleyen</span>
            <h3 class="kpi-value">5</h3>
            <a href="#" class="kpi-link" onclick="navigateTo('manager', event)">Talepleri incele</a>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon bg-purple-light"><i aria-hidden="true" class="fa-solid fa-briefcase"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Açık Pozisyon</span>
            <h3 class="kpi-value">8</h3>
            <span class="kpi-sub">32 yeni başvuru</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon bg-red-light"><i aria-hidden="true" class="fa-solid fa-clock-rotate-left"></i></div>
          <div class="kpi-content">
            <span class="kpi-label">Kritik Hatırlatma</span>
            <h3 class="kpi-value">2</h3>
            <span class="kpi-sub text-danger">Deneme süresi bu hafta bitiyor</span>
          </div>
        </div>
      </div>

      <div class="charts-grid">
        <div class="card chart-card">
          <div class="card-header-clean">
            <div>
              <h4>Departman Dağılımı</h4>
              <p class="text-muted">Aktif çalışan kırılımı</p>
            </div>
          </div>
          <div class="chart-container">
            <canvas id="overviewDeptChart"></canvas>
          </div>
        </div>

        <div class="card chart-card">
          <div class="card-header-clean">
            <div>
              <h4>İşe Alım Hunisi</h4>
              <p class="text-muted">Bu ay aday ilerleyişi</p>
            </div>
            <select class="small-select"><option>Bu Ay</option><option>Bu Yıl</option></select>
          </div>
          <div class="chart-container">
            <canvas id="overviewRecruitmentChart"></canvas>
          </div>
        </div>

        <div class="card status-widget">
          <div class="card-header-clean">
            <h4>Anlık Çalışma Durumu</h4>
          </div>
          <div class="status-list">
            <div class="status-item">
              <div class="status-info"><span class="dot dot-green"></span><span>Ofiste</span></div>
              <strong>85</strong>
            </div>
            <div class="status-item">
              <div class="status-info"><span class="dot dot-blue"></span><span>Uzaktan</span></div>
              <strong>42</strong>
            </div>
            <div class="status-item">
              <div class="status-info"><span class="dot dot-orange"></span><span>İzinli / Raporlu</span></div>
              <strong>15</strong>
            </div>
          </div>
          <div class="pulse-check">
            <small>Çalışan memnuniyeti</small>
            <div class="progress-bar"><div class="fill" style="width: 78%"></div></div>
            <small class="text-right">%78 pozitif</small>
          </div>
        </div>
      </div>

      <div class="bottom-grid">
        <div class="card task-list">
          <div class="card-header-clean">
            <div>
              <h4>Bekleyen Aksiyonlar</h4>
              <p class="text-muted">Öncelik sırasına göre</p>
            </div>
            <span class="badge-count">4</span>
          </div>
          <div class="task-stack">
            <div class="task-item">
              <div class="task-icon warning"><i aria-hidden="true" class="fa-solid fa-plane-departure"></i></div>
              <div class="task-desc">
                <strong>Ahmet Yılmaz - Yıllık izin</strong>
                <small>12 - 18 Ağustos, 6 gün</small>
              </div>
              <div class="toolbar-actions">
                <button class="btn-icon-sm" aria-label="Ahmet Yılmaz izin talebini onayla" title="Onayla" onclick="resolvePendingTask(this, true)"><i aria-hidden="true" class="fa-solid fa-check"></i></button>
                <button class="btn-icon-sm" aria-label="Ahmet Yılmaz izin talebini reddet" title="Reddet" onclick="resolvePendingTask(this, false)"><i aria-hidden="true" class="fa-solid fa-xmark"></i></button>
              </div>
            </div>
            <div class="task-item">
              <div class="task-icon info"><i aria-hidden="true" class="fa-solid fa-file-invoice-dollar"></i></div>
              <div class="task-desc">
                <strong>Ayşe Demir - Masraf fişi</strong>
                <small>Taksi ve yemek, 450 TL</small>
              </div>
              <div class="toolbar-actions">
                <button class="btn-icon-sm" aria-label="Ayşe Demir masraf talebini onayla" title="Onayla" onclick="resolvePendingTask(this, true)"><i aria-hidden="true" class="fa-solid fa-check"></i></button>
                <button class="btn-icon-sm" aria-label="Ayşe Demir masraf talebini reddet" title="Reddet" onclick="resolvePendingTask(this, false)"><i aria-hidden="true" class="fa-solid fa-xmark"></i></button>
              </div>
            </div>
          </div>
        </div>

        <div class="card upcoming-list">
          <div class="card-header-clean">
            <h4>Bu Haftaki Önemli Günler</h4>
          </div>
          <div class="people-row">
            <div class="person-circle" title="Mehmet Can">MC</div>
            <div class="person-circle bg-pink" title="Selin Koç">SK</div>
            <div class="person-circle bg-yellow" title="Ali Veli">AV</div>
            <button class="add-wish-btn" aria-label="Kutlama mesajı gönder" title="Kutlama mesajı gönder" onclick="showToast('Kutlama mesajı gönderildi.', 'success')"><i aria-hidden="true" class="fa-solid fa-paper-plane"></i></button>
          </div>
        </div>
      </div>
    </div>
  `;
};

const AttritionRiskDetail = () => {
  const metrics = getDashboardMetrics();
  const employees = [...metrics.employees].sort((a, b) => b.riskScore - a.riskScore);

  return `
    <div class="detail-page">
      <div class="page-header">
        <div>
          <h2>Ayrılma Riski Detayı</h2>
          <p>Riskli personelleri, sinyal nedenlerini ve önerilen takip aksiyonlarını görün.</p>
        </div>
        ${renderBackToRisk()}
      </div>
      <div class="detail-kpi-grid">
        <div class="stat-box"><span class="sb-label">Yüksek Risk</span><strong class="sb-val text-red">${employees.filter((e) => e.attritionRisk === "high").length}</strong></div>
        <div class="stat-box"><span class="sb-label">Orta Risk</span><strong class="sb-val text-orange">${employees.filter((e) => e.attritionRisk === "medium").length}</strong></div>
        <div class="stat-box"><span class="sb-label">Kritik Rol Riski</span><strong class="sb-val">${employees.filter((e) => e.roleCriticality > 85).length}</strong></div>
        <div class="stat-box"><span class="sb-label">Ortalama Nabız</span><strong class="sb-val">${metrics.pulseScore}<small>%</small></strong></div>
      </div>
      <div class="table-container">
        <table class="detail-table data-table">
          <thead>
            <tr><th>Personel</th><th>Departman</th><th>Yönetici</th><th>Risk</th><th>Son Sinyal</th><th>Önerilen Aksiyon</th></tr>
          </thead>
          <tbody>
            ${employees
              .map(
                (employee) => `
                  <tr>
                    <td><strong>${employee.name}</strong><small>${employee.title}</small></td>
                    <td>${employee.dept}</td>
                    <td>${employee.manager}</td>
                    <td><span class="risk-badge ${employee.attritionRisk}">${getLevelText(employee.attritionRisk)}</span></td>
                    <td>${employee.trend}</td>
                    <td>${employee.action}</td>
                  </tr>
                `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;
};

const BurnoutRiskDetail = () => {
  const metrics = getDashboardMetrics();
  const employees = [...metrics.employees].sort((a, b) => b.overtime + b.unusedLeave - (a.overtime + a.unusedLeave));

  return `
    <div class="detail-page">
      <div class="page-header">
        <div>
          <h2>Tükenmişlik Sinyali</h2>
          <p>Fazla mesai, kullanılmayan izin, geç çıkış ve ekip yoğunluğu kırılımlarını izleyin.</p>
        </div>
        ${renderBackToRisk()}
      </div>
      <div class="detail-kpi-grid">
        <div class="stat-box"><span class="sb-label">Yüksek Sinyal</span><strong class="sb-val text-red">${employees.filter((e) => e.burnoutRisk === "high").length}</strong></div>
        <div class="stat-box"><span class="sb-label">Fazla Mesai Ort.</span><strong class="sb-val">60<small>%</small></strong></div>
        <div class="stat-box"><span class="sb-label">Kullanılmayan İzin</span><strong class="sb-val">64<small>%</small></strong></div>
        <div class="stat-box"><span class="sb-label">Yoğun Departman</span><strong class="sb-val">3</strong></div>
      </div>
      <div class="table-container">
        <table class="detail-table data-table">
          <thead>
            <tr><th>Personel</th><th>Departman</th><th>Fazla Mesai</th><th>Kullanılmayan İzin</th><th>Nabız</th><th>Seviye</th><th>Önerilen Aksiyon</th></tr>
          </thead>
          <tbody>
            ${employees
              .map(
                (employee) => `
                  <tr>
                    <td><strong>${employee.name}</strong><small>${employee.title}</small></td>
                    <td>${employee.dept}</td>
                    <td>${employee.overtime}%</td>
                    <td>${employee.unusedLeave}%</td>
                    <td>${employee.pulse}%</td>
                    <td><span class="risk-badge ${employee.burnoutRisk}">${getLevelText(employee.burnoutRisk)}</span></td>
                    <td>${employee.action}</td>
                  </tr>
                `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;
};

const ManagerLoadDetail = () => {
  const metrics = getDashboardMetrics();

  return `
    <div class="detail-page">
      <div class="page-header">
        <div>
          <h2>Yönetici Yükü Detayı</h2>
          <p>Yönetici bazlı ekip büyüklüğü, bekleyen onay, açık aksiyon ve ekip nabzını görün.</p>
        </div>
        ${renderBackToRisk()}
      </div>
      <div class="detail-kpi-grid">
        <div class="stat-box"><span class="sb-label">Yük Endeksi</span><strong class="sb-val text-orange">${metrics.managerLoadIndex}<small>/100</small></strong></div>
        <div class="stat-box"><span class="sb-label">Kritik Yönetici</span><strong class="sb-val">${metrics.managers.filter((m) => m.load > 70).length}</strong></div>
        <div class="stat-box"><span class="sb-label">Bekleyen Onay</span><strong class="sb-val">15</strong></div>
        <div class="stat-box"><span class="sb-label">Açık Aksiyon</span><strong class="sb-val">22</strong></div>
      </div>
      <div class="table-container">
        <table class="detail-table data-table">
          <thead>
            <tr><th>Yönetici</th><th>Ekip</th><th>Onay</th><th>Aksiyon</th><th>Fazla Mesai</th><th>Ekip Nabzı</th><th>Yük</th><th>Öneri</th></tr>
          </thead>
          <tbody>
            ${metrics.managers
              .map(
                (manager) => `
                  <tr>
                    <td><strong>${manager.name}</strong><small>${manager.load > 70 ? "Kritik takip" : "Normal takip"}</small></td>
                    <td>${manager.team} kişi</td>
                    <td>${manager.approvals}</td>
                    <td>${manager.actions}</td>
                    <td>${manager.overtime}%</td>
                    <td>${manager.pulse}%</td>
                    <td><span class="risk-badge ${getRiskLevel(manager.load)}">${manager.load}/100</span></td>
                    <td>${manager.load > 70 ? "Aksiyon devri ve kapasite görüşmesi" : "Haftalık takip yeterli"}</td>
                  </tr>
                `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;
};

// Not: Eski ActionCenterDetail görünümü kaldırıldı; /risk/action-center rotası
// artık tek aksiyon kaynağı olan Global Aksiyon Merkezi'ne (ActionsCenter) yönlenir.

const resolvePendingTask = (button, approved) => {
  const item = button.closest(".task-item");
  const label = item?.querySelector("strong")?.textContent || "Talep";
  item?.remove();
  showToast(
    approved ? `${label} onaylandı.` : `${label} reddedildi.`,
    approved ? "success" : "info"
  );

  const badge = document.querySelector(".task-list .badge-count");
  const remaining = document.querySelectorAll(".task-list .task-item").length;
  if (badge) badge.textContent = remaining;
};
window.resolvePendingTask = resolvePendingTask;

const EmployeeVoiceDetail = () => {
  const metrics = getDashboardMetrics();
  const voice = metrics.employeeVoiceMetrics;

  return `
    <div class="detail-page">
      <div class="page-header">
        <div>
          <h2>Çalışan Sesleri / Nabız Analitiği</h2>
          <p>Departman bazlı ruh hali, bağlılık sinyali ve takip önerilerini tek görünümde izleyin.</p>
        </div>
        ${renderBackToRisk()}
      </div>

      <div class="detail-kpi-grid">
        <div class="stat-box"><span class="sb-label">Nabız Skoru</span><strong class="sb-val text-orange">${voice.pulseScore}<small>/100</small></strong></div>
        <div class="stat-box"><span class="sb-label">eNPS</span><strong class="sb-val">+${voice.eNps}</strong></div>
        <div class="stat-box"><span class="sb-label">Katılım Oranı</span><strong class="sb-val">${voice.participationRate}<small>%</small></strong></div>
        <div class="stat-box"><span class="sb-label">Düşen Takım</span><strong class="sb-val text-red">${voice.decliningTeams}</strong></div>
      </div>

      <div class="voice-layout">
        <div class="table-container">
          <table class="detail-table data-table">
            <thead>
              <tr><th>Departman</th><th>Ruh Hali</th><th>Nabız</th><th>eNPS</th><th>Katılım</th><th>Öne Çıkan Sinyal</th><th>Seviye</th></tr>
            </thead>
            <tbody>
              ${voice.departments
                .map(
                  (department) => `
                    <tr>
                      <td><strong>${department.dept}</strong></td>
                      <td>${department.mood}</td>
                      <td>${department.pulse}/100</td>
                      <td>${department.eNps > 0 ? "+" : ""}${department.eNps}</td>
                      <td>${department.participation}%</td>
                      <td>${department.driver}</td>
                      <td><span class="risk-badge ${department.level}">${getLevelText(department.level)}</span></td>
                    </tr>
                  `
                )
                .join("")}
            </tbody>
          </table>
        </div>

        <aside class="card insight-panel">
          <div class="card-header-clean">
            <div>
              <h4>Riskli Ekipler</h4>
              <p class="text-muted">Sessiz memnuniyetsizlik ve bağlılık kırılmaları.</p>
            </div>
          </div>
          <div class="action-list">
            ${voice.riskTeams
              .map(
                (team) => `
                  <div class="risk-action ${team.level}">
                    <div class="action-priority">${team.signal}</div>
                    <strong>${team.team}</strong>
                    <p>${team.reason}</p>
                    <small>${team.owner}</small>
                    <div class="recommended-action"><i aria-hidden="true" class="fa-solid fa-lightbulb"></i><span>${team.action}</span></div>
                  </div>
                `
              )
              .join("")}
          </div>
        </aside>
      </div>

      <div class="detail-support-grid">
        <section class="card">
          <div class="card-header-clean"><h4>Son Nabız Sinyalleri</h4></div>
          <div class="signal-list">
            ${voice.signals.map((signal) => `<div class="signal-note"><i aria-hidden="true" class="fa-solid fa-circle-info"></i><span>${signal}</span></div>`).join("")}
          </div>
        </section>
        <section class="card">
          <div class="card-header-clean"><h4>Önerilen Aksiyonlar</h4></div>
          <div class="signal-list">
            ${voice.recommendedActions.map((action) => `<div class="signal-note action"><i aria-hidden="true" class="fa-solid fa-check"></i><span>${action}</span></div>`).join("")}
          </div>
        </section>
      </div>
    </div>
  `;
};

const ComplianceRiskDetail = () => {
  const metrics = getDashboardMetrics();
  const compliance = metrics.complianceMetrics;

  return `
    <div class="detail-page">
      <div class="page-header">
        <div>
          <h2>Uyum, Evrak ve Denetim Risk Merkezi</h2>
          <p>Eksik evrak, yaklaşan son tarih ve denetim hazırlığını operasyonel takip görünümünde yönetin.</p>
        </div>
        ${renderBackToRisk()}
      </div>

      <div class="detail-kpi-grid">
        <div class="stat-box"><span class="sb-label">Evrak Uyum Skoru</span><strong class="sb-val">${compliance.documentComplianceScore}<small>/100</small></strong></div>
        <div class="stat-box"><span class="sb-label">Eksik Evrak</span><strong class="sb-val text-red">${compliance.missingDocuments}</strong></div>
        <div class="stat-box"><span class="sb-label">Süresi Yaklaşan</span><strong class="sb-val text-orange">${compliance.upcomingDocuments}</strong></div>
        <div class="stat-box"><span class="sb-label">Denetim Riski</span><strong class="sb-val text-orange">${compliance.auditReadinessRisk}</strong></div>
      </div>

      <div class="compliance-layout">
        <div class="table-container">
          <table class="detail-table data-table">
            <thead>
              <tr><th>Personel</th><th>Departman</th><th>Evrak</th><th>Sorumlu</th><th>Son Tarih</th><th>Durum</th><th>Risk</th></tr>
            </thead>
            <tbody>
              ${compliance.records
                .map(
                  (record) => `
                    <tr>
                      <td><strong>${record.employee}</strong></td>
                      <td>${record.dept}</td>
                      <td>${record.document}</td>
                      <td>${record.owner}</td>
                      <td>${record.dueDate}</td>
                      <td><span class="status-pill ${record.status === "Tamamlandı" ? "approved" : record.status === "Eksik" ? "rejected" : "pending"}">${record.status}</span></td>
                      <td><span class="risk-badge ${record.level}">${getLevelText(record.level)}</span></td>
                    </tr>
                  `
                )
                .join("")}
            </tbody>
          </table>
        </div>

        <aside class="card insight-panel">
          <div class="card-header-clean">
            <div>
              <h4>Yaklaşan Son Tarihler</h4>
              <p class="text-muted">Kritik evrak aksiyonları ve sorumlular.</p>
            </div>
          </div>
          <div class="deadline-list">
            ${compliance.deadlines
              .map(
                (deadline) => `
                  <div class="deadline-item ${deadline.level}">
                    <div>
                      <strong>${deadline.title}</strong>
                      <span>${deadline.count} kayıt · ${deadline.owner}</span>
                    </div>
                    <em>${deadline.dueDate}</em>
                  </div>
                `
              )
              .join("")}
          </div>
        </aside>
      </div>

      <div class="detail-support-grid">
        <section class="card">
          <div class="card-header-clean"><h4>Denetim Hazırlığı</h4><span class="status-pill pending">${compliance.auditReadinessScore}/100</span></div>
          <div class="audit-readiness-list">
            ${compliance.auditChecklist
              .map(
                (item) => `
                  <div class="audit-readiness-item ${item.level}">
                    <div class="capacity-top"><span>${item.label}</span><strong>${item.value}%</strong></div>
                    <div class="progress-bar"><div class="fill" style="width:${item.value}%"></div></div>
                  </div>
                `
              )
              .join("")}
          </div>
        </section>
        <section class="card">
          <div class="card-header-clean"><h4>Önerilen Aksiyonlar</h4></div>
          <div class="signal-list">
            ${compliance.recommendedActions.map((action) => `<div class="signal-note action"><i aria-hidden="true" class="fa-solid fa-check"></i><span>${action}</span></div>`).join("")}
          </div>
        </section>
      </div>
    </div>
  `;
};

const chartToken = (name, fallback) =>
  getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;

window.initDashboardCharts = (metrics = getDashboardMetrics()) => {
  const riskTrend = document.getElementById("riskTrendChart");
  const overviewDept = document.getElementById("overviewDeptChart");
  const overviewRecruitment = document.getElementById("overviewRecruitmentChart");

  if (typeof Chart === "undefined") {
    document
      .querySelectorAll("#riskTrendChart, #overviewDeptChart, #overviewRecruitmentChart")
      .forEach((canvas) => {
        canvas.outerHTML =
          '<div class="chart-fallback"><i aria-hidden="true" class="fa-solid fa-chart-line"></i><span>Grafik önizlemesi yüklenemedi</span></div>';
      });
    return;
  }

  if (riskTrend) {
    if (Chart.getChart("riskTrendChart")) Chart.getChart("riskTrendChart").destroy();
    new Chart(riskTrend, {
      type: "line",
      data: {
        labels: ["H-12", "H-11", "H-10", "H-9", "H-8", "H-7", "H-6", "H-5", "H-4", "H-3", "H-2", "Bu hafta"],
        datasets: [
          {
            label: "İK Risk Skoru",
            data: metrics.riskTrend,
            borderColor: chartToken("--primary", "#0f766e"),
            backgroundColor: "rgba(15, 118, 110, 0.12)",
            fill: true,
            borderWidth: 2.5,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: chartToken("--surface", "#ffffff"),
            pointBorderColor: chartToken("--primary", "#0f766e"),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { mode: "index", intersect: false } },
        scales: {
          y: { min: 0, max: 100, grid: { color: chartToken("--line-soft", "#e9efef") }, ticks: { stepSize: 20 } },
          x: { grid: { display: false } },
        },
      },
    });
  }

  if (overviewDept) {
    if (Chart.getChart("overviewDeptChart")) Chart.getChart("overviewDeptChart").destroy();
    new Chart(overviewDept, {
      type: "doughnut",
      data: {
        labels: ["Yazılım", "Satış", "İK", "Finans", "Operasyon"],
        datasets: [
          {
            data: [35, 25, 10, 15, 15],
            backgroundColor: ["#0f766e", "#0e7490", "#b98a2f", "#157f3d", "#5b7c99"],
            borderWidth: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: "right", labels: { usePointStyle: true, boxWidth: 8 } } },
      },
    });
  }

  if (overviewRecruitment) {
    if (Chart.getChart("overviewRecruitmentChart")) Chart.getChart("overviewRecruitmentChart").destroy();
    new Chart(overviewRecruitment, {
      type: "bar",
      data: {
        labels: ["Başvuru", "Ön Görüşme", "Mülakat", "Teklif", "İşe Giriş"],
        datasets: [{ label: "Aday", data: [120, 64, 45, 12, 5], backgroundColor: chartToken("--primary", "#0f766e"), borderRadius: 5 }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: { y: { beginAtZero: true, grid: { color: chartToken("--line-soft", "#e9efef") } }, x: { grid: { display: false } } },
        plugins: { legend: { display: false } },
      },
    });
  }
};
