const actionLevelText = (level) => ({ high: "Yüksek", medium: "Orta", low: "Düşük" }[level] || "Normal");
const actionStatusText = (status) => ({ open: "Açık", week: "Bu Hafta", done: "Tamamlandı" }[status] || "Açık");

const renderActionCard = (action) => `
  <article class="global-action-card ${action.priority}"
    data-priority="${action.priority}" data-source="${action.source}" data-owner="${action.owner}">
    <div class="global-action-top">
      <span class="status-pill ${action.priority === "high" ? "rejected" : action.priority === "medium" ? "pending" : "approved"}">${actionLevelText(action.priority)}</span>
      <span>${action.due}</span>
    </div>
    <h4>${action.title}</h4>
    <p>${action.action}</p>
    <div class="global-action-meta">
      <span><i aria-hidden="true" class="fa-solid fa-layer-group"></i> ${action.source}</span>
      <span><i aria-hidden="true" class="fa-solid fa-user"></i> ${action.owner}</span>
    </div>
    <div class="global-action-footer">
      <span class="status-pill info">${actionStatusText(action.status)}</span>
      <button class="btn btn-secondary btn-sm" onclick="navigateTo('${action.sourceRoute}', event)">Kaynağa git</button>
    </div>
  </article>
`;

const renderAuditTimeline = () => `
  <div class="audit-timeline">
    ${getMockAuditLogs()
      .map(
        (log) => `
          <div class="audit-item">
            <div class="audit-dot"></div>
            <div class="audit-body">
              <div class="audit-head">
                <strong>${log.action}</strong>
                <span>${log.time}</span>
              </div>
              <p>${log.detail}</p>
              <small>${log.actor} · ${log.module}</small>
            </div>
          </div>
        `
      )
      .join("")}
  </div>
`;

const ActionsCenter = () => {
  const actions = getMockActions();
  const kpis = {
    today: actions.filter((item) => item.due === "Bugün").length,
    overdue: 1,
    high: actions.filter((item) => item.priority === "high" && item.status !== "done").length,
    done: actions.filter((item) => item.status === "done").length,
  };

  return `
    <div id="actions-screen">
      <div class="page-header">
        <div>
          <h2>Global Aksiyon Merkezi</h2>
          <p>Risk, bordro, uyum ve çalışan deneyimi aksiyonlarını tek operasyon merkezinde takip edin.</p>
        </div>
      </div>

      <div class="actions-kpi-grid">
        <div class="stat-box"><span class="sb-label">Bugün</span><strong class="sb-val">${kpis.today}</strong><small>Kapanması beklenen</small></div>
        <div class="stat-box"><span class="sb-label">Geciken</span><strong class="sb-val text-red">${kpis.overdue}</strong><small>Riskli aksiyon</small></div>
        <div class="stat-box"><span class="sb-label">Yüksek Öncelik</span><strong class="sb-val text-orange">${kpis.high}</strong><small>Aktif takip</small></div>
        <div class="stat-box"><span class="sb-label">Tamamlanan</span><strong class="sb-val">${kpis.done}</strong><small>Bu hafta</small></div>
      </div>

      <section class="card actions-filter-bar">
        <label class="sr-only" for="actions-priority-filter">Öncelik filtresi</label>
        <select id="actions-priority-filter" class="small-select" onchange="filterGlobalActions()">
          <option value="">Öncelik: Tümü</option>
          <option value="high">Yüksek</option>
          <option value="medium">Orta</option>
          <option value="low">Düşük</option>
        </select>
        <label class="sr-only" for="actions-source-filter">Kaynak filtresi</label>
        <select id="actions-source-filter" class="small-select" onchange="filterGlobalActions()">
          <option value="">Kaynak: Tümü</option>
          ${[...new Set(actions.map((item) => item.source))].map((source) => `<option value="${source}">${source}</option>`).join("")}
        </select>
        <label class="sr-only" for="actions-owner-filter">Sahip filtresi</label>
        <select id="actions-owner-filter" class="small-select" onchange="filterGlobalActions()">
          <option value="">Sahip: Tümü</option>
          ${[...new Set(actions.map((item) => item.owner))].map((owner) => `<option value="${owner}">${owner}</option>`).join("")}
        </select>
      </section>

      <div class="actions-tabs">
        <button class="action-tab active" onclick="switchActionsTab('actions-open', event)">Açık</button>
        <button class="action-tab" onclick="switchActionsTab('actions-week', event)">Bu Hafta</button>
        <button class="action-tab" onclick="switchActionsTab('actions-done', event)">Tamamlanan</button>
        <button class="action-tab" onclick="switchActionsTab('actions-audit', event)">Denetim İzi</button>
      </div>

      <section id="actions-open" class="actions-tab-content active">
        <div class="global-actions-grid">${actions.filter((item) => item.status === "open").map(renderActionCard).join("")}</div>
      </section>
      <section id="actions-week" class="actions-tab-content">
        <div class="global-actions-grid">${actions.filter((item) => item.status === "week").map(renderActionCard).join("")}</div>
      </section>
      <section id="actions-done" class="actions-tab-content">
        <div class="global-actions-grid">${actions.filter((item) => item.status === "done").map(renderActionCard).join("")}</div>
      </section>
      <section id="actions-audit" class="actions-tab-content">
        <section class="card">${renderAuditTimeline()}</section>
      </section>
    </div>
  `;
};

window.switchActionsTab = (tabId, evt) => {
  document.querySelectorAll(".action-tab").forEach((tab) => tab.classList.remove("active"));
  document.querySelectorAll(".actions-tab-content").forEach((content) => content.classList.remove("active"));
  evt?.currentTarget?.classList.add("active");
  document.getElementById(tabId)?.classList.add("active");
};

window.filterGlobalActions = () => {
  const priority = document.getElementById("actions-priority-filter")?.value || "";
  const source = document.getElementById("actions-source-filter")?.value || "";
  const owner = document.getElementById("actions-owner-filter")?.value || "";

  document.querySelectorAll(".global-action-card").forEach((card) => {
    const matches =
      (!priority || card.dataset.priority === priority) &&
      (!source || card.dataset.source === source) &&
      (!owner || card.dataset.owner === owner);
    card.style.display = matches ? "" : "none";
  });

  // Filtre sonrası boş kalan sekmelerde bilgi notu göster.
  document.querySelectorAll(".global-actions-grid").forEach((grid) => {
    const anyVisible = Array.from(grid.querySelectorAll(".global-action-card")).some(
      (card) => card.style.display !== "none"
    );
    let note = grid.querySelector(".empty-lane");
    if (!anyVisible) {
      if (!note) {
        note = document.createElement("div");
        note.className = "empty-lane";
        note.textContent = "Bu filtrede aksiyon yok.";
        grid.appendChild(note);
      }
      note.hidden = false;
    } else if (note) {
      note.hidden = true;
    }
  });
};
