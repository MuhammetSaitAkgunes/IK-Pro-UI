const Leaves = () => {
  const leaveHistory = [
    {
      id: 1,
      type: "Yıllık İzin",
      start: "12 Ağu 2024",
      end: "18 Ağu 2024",
      days: 5,
      status: "approved",
    },
    {
      id: 2,
      type: "Mazeret İzni",
      start: "20 Eyl 2024",
      end: "20 Eyl 2024",
      days: 1,
      status: "pending",
    },
  ];

  const teamLeaves = [
    { name: "Selin Koç", date: "Bugün", type: "Raporlu", avatar: "SK" },
    { name: "Mert Demir", date: "Yarın", type: "Yıllık izin", avatar: "MD" },
  ];

  return `
    <div id="leaves-screen">
      <div class="page-header">
        <div>
          <h2>İzinlerim</h2>
          <p>Bakiye, geçmiş talepler ve ekip yokluk takibini tek alanda görüntüleyin.</p>
        </div>
        <button class="btn btn-primary" onclick="openLeaveModal()">
          <i aria-hidden="true" class="fa-solid fa-plus"></i> İzin Talebi Oluştur
        </button>
      </div>

      <div class="balance-grid">
        <div class="bal-card primary">
          <div class="bal-header">
            <div class="bal-icon"><i aria-hidden="true" class="fa-solid fa-umbrella-beach"></i></div>
            <span class="status-pill info">Aktif bakiye</span>
          </div>
          <div class="bal-info">
            <span>Kalan Yıllık İzin</span>
            <strong>14 <small>gün</small></strong>
          </div>
          <div class="bal-progress"><div class="prog-bar" style="width: 65%"></div></div>
          <span class="bal-sub">Hak ediş: 24 gün</span>
        </div>
        <div class="bal-card">
          <div class="bal-header"><div class="bal-icon"><i aria-hidden="true" class="fa-solid fa-clock-rotate-left"></i></div></div>
          <div class="bal-info">
            <span>Kullanılan Toplam</span>
            <strong>10 <small>gün</small></strong>
          </div>
          <div class="bal-stats">
            <div class="stat-pill"><span class="dot approved"></span> 6 yıllık</div>
            <div class="stat-pill"><span class="dot sick"></span> 3 rapor</div>
          </div>
        </div>
        <div class="bal-card">
          <div class="bal-header"><div class="bal-icon"><i aria-hidden="true" class="fa-solid fa-hourglass-half"></i></div></div>
          <div class="bal-info">
            <span>Onay Bekleyen</span>
            <strong>1 <small>talep</small></strong>
          </div>
          <p class="pending-desc">20 Eylül tarihli mazeret izniniz yönetici onayı bekliyor.</p>
        </div>
      </div>

      <div class="leaves-layout">
        <div class="leaves-list-section">
          <div class="section-header"><h3>İzin Hareketleri</h3></div>
          <div class="table-scroll">
            <table class="leaf-table">
              <thead>
                <tr>
                  <th>Tür</th>
                  <th>Tarih Aralığı</th>
                  <th>Süre</th>
                  <th>Durum</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                ${leaveHistory
                  .map(
                    (leave) => `
                      <tr>
                        <td>
                          <div class="l-type">
                            <span class="dot ${leave.type === "Raporlu" ? "sick" : "annual"}"></span>
                            <strong>${leave.type}</strong>
                          </div>
                        </td>
                        <td>${leave.start} - ${leave.end}</td>
                        <td><span class="days-badge">${leave.days} gün</span></td>
                        <td>
                          <span class="status-pill ${leave.status}">
                            ${leave.status === "approved" ? "Onaylandı" : "Bekliyor"}
                          </span>
                        </td>
                        <td class="text-right">
                          ${
                            leave.status === "pending"
                              ? `<button class="btn-icon-sm" title="Talebi iptal et" aria-label="${leave.type} talebini iptal et" onclick="cancelLeaveRequest(this)"><i aria-hidden="true" class="fa-solid fa-trash"></i></button>`
                              : ""
                          }
                        </td>
                      </tr>
                    `
                  )
                  .join("")}
              </tbody>
            </table>
          </div>
        </div>

        <div class="sidebar-col">
          <div class="team-calendar-widget card">
            <div class="widget-header"><h3>Ofiste Kimler Yok?</h3></div>
            <div class="away-list">
              ${teamLeaves
                .map(
                  (team) => `
                    <div class="away-item">
                      <div class="away-avatar">${team.avatar}</div>
                      <div class="away-info">
                        <strong>${team.name}</strong>
                        <span>${team.date} • ${team.type}</span>
                      </div>
                    </div>
                  `
                )
                .join("")}
            </div>
          </div>
        </div>
      </div>

      <div id="leave-modal-overlay" class="modal-overlay">
        <div class="modal-card scale-in">
          <div class="modal-head">
            <div>
              <h3>Yeni İzin Talebi</h3>
              <p>Talep detaylarını net ve eksiksiz doldurun.</p>
            </div>
            <button class="btn-icon-sm" onclick="closeLeaveModal()" title="Kapat" aria-label="İzin talebi penceresini kapat"><i aria-hidden="true" class="fa-solid fa-xmark"></i></button>
          </div>

          <div class="modal-body-scroll">
            <label class="input-label">İzin Türü</label>
            <div class="type-grid">
              <label class="type-card">
                <input type="radio" name="leaveType" checked />
                <div class="tc-content">
                  <div class="tc-icon annual"><i aria-hidden="true" class="fa-solid fa-sun"></i></div>
                  <span>Yıllık İzin</span>
                  <small>Kalan: 14 gün</small>
                </div>
              </label>
              <label class="type-card">
                <input type="radio" name="leaveType" />
                <div class="tc-content">
                  <div class="tc-icon sick"><i aria-hidden="true" class="fa-solid fa-notes-medical"></i></div>
                  <span>Raporlu</span>
                  <small>Belge gerekli</small>
                </div>
              </label>
              <label class="type-card">
                <input type="radio" name="leaveType" />
                <div class="tc-content">
                  <div class="tc-icon excuse"><i aria-hidden="true" class="fa-solid fa-clock"></i></div>
                  <span>Mazeret</span>
                  <small>Saatlik/günlük</small>
                </div>
              </label>
              <label class="type-card">
                <input type="radio" name="leaveType" />
                <div class="tc-content">
                  <div class="tc-icon remote"><i aria-hidden="true" class="fa-solid fa-laptop-house"></i></div>
                  <span>Uzaktan</span>
                  <small>Evden çalışma</small>
                </div>
              </label>
            </div>

            <div class="form-grid-2 mt-4">
              <div class="input-group">
                <label class="input-label">Başlangıç Tarihi</label>
                <input type="date" class="input-control" id="start-date" onchange="calcDays()" />
              </div>
              <div class="input-group">
                <label class="input-label">Bitiş Tarihi</label>
                <input type="date" class="input-control" id="end-date" onchange="calcDays()" />
              </div>
            </div>

            <div class="calc-box">
              <div class="cb-item"><span>Süre</span><strong id="calc-days">- gün</strong></div>
              <div class="cb-item"><span>İşe dönüş</span><strong id="return-date">-</strong></div>
            </div>

            <div class="input-group mt-4">
              <label class="input-label">Açıklama / Adres</label>
              <textarea class="input-control" rows="2" placeholder="İzin nedeni veya bulunacağınız adres"></textarea>
            </div>

            <div class="input-group mt-3">
              <label class="input-label">Yerine Bakacak Kişi</label>
              <select class="input-control">
                <option>Seçiniz...</option>
                <option>Selin Koç</option>
                <option>Ahmet Yılmaz</option>
              </select>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-ghost" onclick="closeLeaveModal()">Vazgeç</button>
            <button class="btn btn-primary" onclick="submitLeave()">
              <i aria-hidden="true" class="fa-solid fa-paper-plane"></i> Talebi Gönder
            </button>
          </div>
        </div>
      </div>
    </div>
  `;
};

window.openLeaveModal = () => {
  const overlay = document.getElementById("leave-modal-overlay");
  if (overlay) overlay.style.display = "flex";
};

window.closeLeaveModal = () => {
  const overlay = document.getElementById("leave-modal-overlay");
  if (overlay) overlay.style.display = "none";
};

window.calcDays = () => {
  const s = document.getElementById("start-date").value;
  const e = document.getElementById("end-date").value;
  if (s && e) {
    document.getElementById("calc-days").innerText = "3 gün";
    document.getElementById("return-date").innerText = "15 Eki 2024";
  }
};

window.submitLeave = () => {
  const start = document.getElementById("start-date")?.value;
  const end = document.getElementById("end-date")?.value;
  if (!start || !end) {
    showToast("Başlangıç ve bitiş tarihlerini seçin.", "warning");
    return;
  }

  closeLeaveModal();
  showToast("İzin talebiniz yönetici onayına gönderildi.", "success");
};

window.cancelLeaveRequest = (button) => {
  const row = button.closest("tr");
  const type = row?.querySelector("strong")?.textContent || "İzin";
  row?.remove();
  showToast(`${type} talebi iptal edildi.`, "info");
};
