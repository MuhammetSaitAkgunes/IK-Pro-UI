const Personnel = () => {
  // Tek kaynak: personel dizini mockData'daki getDirectory'den gelir
  // (global arama ile aynı veri).
  const employees = getDirectory();
  const departments = [...new Set(employees.map((user) => user.dept))];

  return `
    <div id="personnel-screen">
      <div id="list-screen">
        <div class="page-header">
          <div>
            <h2>Personel Yönetimi</h2>
            <p>Sicil, özlük, iletişim ve kurumsal bilgileri tek ekrandan yönetin.</p>
          </div>
          <button class="btn btn-primary" onclick="openModal()">
            <i aria-hidden="true" class="fa-solid fa-plus"></i> Yeni Personel
          </button>
        </div>

        <div class="filter-bar surface">
          <div class="search-wrapper">
            <i aria-hidden="true" class="fa-solid fa-magnifying-glass"></i>
            <label class="sr-only" for="personnel-search">Personel ara</label>
            <input id="personnel-search" type="text" class="search-input" placeholder="Ad, departman veya görev ara" oninput="filterPersonnel()" />
          </div>
          <label class="sr-only" for="personnel-dept-filter">Departman filtresi</label>
          <select id="personnel-dept-filter" class="filter-select" onchange="filterPersonnel()">
            <option value="">Departman: Tümü</option>
            ${departments.map((dept) => `<option value="${dept}">${dept}</option>`).join("")}
          </select>
          <label class="sr-only" for="personnel-status-filter">Durum filtresi</label>
          <select id="personnel-status-filter" class="filter-select" onchange="filterPersonnel()">
            <option value="">Durum: Tümü</option>
            <option value="active">Aktif</option>
            <option value="passive">Pasif</option>
          </select>
          <button class="btn btn-secondary" onclick="exportPersonnel()">
            <i aria-hidden="true" class="fa-solid fa-file-excel"></i> Dışa Aktar
          </button>
        </div>

        <div id="personnel-bulk-bar" class="bulk-bar surface" hidden>
          <strong id="personnel-bulk-count">0 kişi seçildi</strong>
          <div class="toolbar-actions">
            <button class="btn btn-secondary btn-sm" onclick="exportPersonnel()">
              <i aria-hidden="true" class="fa-solid fa-file-excel"></i> Seçilenleri dışa aktar
            </button>
            <button class="btn btn-secondary btn-sm" onclick="bulkDeactivatePersonnel()">
              <i aria-hidden="true" class="fa-solid fa-user-slash"></i> Pasife al
            </button>
            <button class="btn btn-ghost btn-sm" onclick="clearPersonnelSelection()">Seçimi temizle</button>
          </div>
        </div>

        <div class="table-container">
          <table class="pro-table" id="personnel-table">
            <thead>
              <tr>
                <th class="csv-skip check-col">
                  <input type="checkbox" aria-label="Tüm personeli seç" onchange="toggleAllPersonnel(this)" />
                </th>
                <th>Personel</th>
                <th>Departman</th>
                <th>TC Kimlik</th>
                <th>İşe Giriş</th>
                <th>Durum</th>
                <th style="text-align:right" class="csv-skip">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              ${employees
                .map(
                  (user) => `
                    <tr
                      data-search="${`${user.name} ${user.title} ${user.dept}`.toLocaleLowerCase("tr-TR")}"
                      data-dept="${user.dept}"
                      data-status="${user.status}"
                    >
                      <td class="csv-skip check-col">
                        <input type="checkbox" class="personnel-row-check" aria-label="${user.name} kaydını seç" onchange="updatePersonnelBulkBar()" />
                      </td>
                      <td>
                        <div class="user-meta">
                          <div class="avatar-sm" aria-hidden="true">${user.initials}</div>
                          <div class="meta-info">
                            <strong>${user.name}</strong>
                            <small>${user.title}</small>
                          </div>
                        </div>
                      </td>
                      <td><strong>${user.dept}</strong></td>
                      <td class="mono">${user.tc}</td>
                      <td>${user.start}</td>
                      <td>
                        <span class="badge badge-${user.status}">
                          ${user.status === "active" ? "Aktif" : "Pasif"}
                        </span>
                      </td>
                      <td style="text-align:right" class="csv-skip">
                        <div class="row-actions">
                          <button class="btn-icon-sm" title="Görüntüle" aria-label="${user.name} kartını görüntüle" onclick="openModal('${user.name}')">
                            <i aria-hidden="true" class="fa-regular fa-eye"></i>
                          </button>
                          <button class="btn-icon-sm" title="Düzenle" aria-label="${user.name} kaydını düzenle" onclick="openModal('${user.name}')">
                            <i aria-hidden="true" class="fa-solid fa-pen"></i>
                          </button>
                        </div>
                      </td>
                    </tr>
                  `
                )
                .join("")}
            </tbody>
          </table>
          <div id="personnel-empty" class="empty-state" hidden>
            <i aria-hidden="true" class="fa-solid fa-user-slash"></i>
            <h3>Eşleşen personel bulunamadı</h3>
            <p>Arama veya filtre ölçütlerini değiştirerek yeniden deneyin.</p>
          </div>
        </div>
      </div>

      <div id="personnel-modal" class="fullscreen-modal">
        <div class="modal-header">
          <div>
            <h2>Yeni Personel Kartı</h2>
            <p>Gerekli alanları tamamlayarak özlük kaydını oluşturun.</p>
          </div>
          <div class="modal-actions">
            <button class="btn btn-ghost" onclick="closeModal()">Vazgeç</button>
            <button class="btn btn-primary" onclick="savePerson()">
              <i aria-hidden="true" class="fa-solid fa-check"></i> Kaydet
            </button>
          </div>
        </div>

        <div class="modal-body">
          <aside class="modal-sidebar">
            <button class="nav-btn active" onclick="switchFormTab('tab-kimlik')">
              <i aria-hidden="true" class="fa-regular fa-id-card"></i> Kimlik Bilgileri
            </button>
            <button class="nav-btn" onclick="switchFormTab('tab-iletisim')">
              <i aria-hidden="true" class="fa-solid fa-phone"></i> İletişim & Adres
            </button>
            <button class="nav-btn" onclick="switchFormTab('tab-is')">
              <i aria-hidden="true" class="fa-solid fa-briefcase"></i> İş & Kurumsal
            </button>
            <button class="nav-btn" onclick="switchFormTab('tab-mali')">
              <i aria-hidden="true" class="fa-solid fa-wallet"></i> Mali Bilgiler
            </button>
            <button class="nav-btn" onclick="switchFormTab('tab-ozluk')">
              <i aria-hidden="true" class="fa-solid fa-shield-heart"></i> Özlük & Sağlık
            </button>
            <button class="nav-btn" onclick="switchFormTab('tab-evrak')">
              <i aria-hidden="true" class="fa-solid fa-folder-tree"></i> Evraklar
            </button>
          </aside>

          <main class="modal-content-area">
            <div id="tab-kimlik" class="content-section active">
              <div class="section-head">
                <div>
                  <h3>Kimlik & Kişisel Bilgiler</h3>
                  <span>Nüfus bilgilerini resmi evraklarla uyumlu girin.</span>
                </div>
              </div>
              <div class="form-grid">
                <div class="photo-upload col-12">
                  <div class="photo-preview"><i aria-hidden="true" class="fa-solid fa-user"></i></div>
                  <div>
                    <span class="upload-btn">Fotoğraf Yükle</span>
                    <small>JPG/PNG, maksimum 2 MB</small>
                  </div>
                </div>
                <div class="input-group col-6">
                  <label class="input-label">TC Kimlik No *</label>
                  <input type="text" class="input-control" maxlength="11" placeholder="11 haneli numara" />
                </div>
                <div class="input-group col-6">
                  <label class="input-label">Doğum Tarihi</label>
                  <input type="date" class="input-control" />
                </div>
                <div class="input-group col-6">
                  <label class="input-label">Adı</label>
                  <input type="text" class="input-control" />
                </div>
                <div class="input-group col-6">
                  <label class="input-label">Soyadı</label>
                  <input type="text" class="input-control" />
                </div>
                <div class="input-group col-4">
                  <label class="input-label">Cinsiyet</label>
                  <select class="input-control"><option>Erkek</option><option>Kadın</option></select>
                </div>
                <div class="input-group col-4">
                  <label class="input-label">Medeni Durum</label>
                  <select class="input-control"><option>Evli</option><option>Bekar</option></select>
                </div>
                <div class="input-group col-4">
                  <label class="input-label">Kan Grubu</label>
                  <select class="input-control"><option>0 Rh+</option><option>A Rh+</option><option>B Rh+</option></select>
                </div>
              </div>
            </div>

            <div id="tab-iletisim" class="content-section">
              <div class="section-head"><div><h3>İletişim Bilgileri</h3><span>Personelin ulaşılabilir iletişim kanalları.</span></div></div>
              <div class="form-grid">
                <div class="input-group col-6"><label class="input-label">Cep Telefonu</label><input type="tel" class="input-control" placeholder="(5XX) ..." /></div>
                <div class="input-group col-6"><label class="input-label">Kişisel E-Posta</label><input type="email" class="input-control" /></div>
                <div class="input-group col-12"><label class="input-label">Ev Adresi</label><textarea class="input-control" rows="3"></textarea></div>
                <div class="input-group col-6"><label class="input-label">Acil Durum Kişisi</label><input type="text" class="input-control" placeholder="Ad Soyad" /></div>
                <div class="input-group col-6"><label class="input-label">Yakınlık / Telefon</label><input type="text" class="input-control" placeholder="Örn: Eşi - 0532..." /></div>
              </div>
            </div>

            <div id="tab-is" class="content-section">
              <div class="section-head"><div><h3>Kurumsal Bilgiler</h3><span>Pozisyon, çalışma şekli ve organizasyon bilgileri.</span></div></div>
              <div class="form-grid">
                <div class="input-group col-6"><label class="input-label">Departman</label><select class="input-control"><option>Yazılım</option><option>İK</option><option>Satış</option></select></div>
                <div class="input-group col-6"><label class="input-label">Ünvan / Görev</label><input type="text" class="input-control" /></div>
                <div class="input-group col-6"><label class="input-label">İşe Giriş Tarihi</label><input type="date" class="input-control" /></div>
                <div class="input-group col-6"><label class="input-label">Çalışma Şekli</label><select class="input-control"><option>Tam Zamanlı</option><option>Yarı Zamanlı</option><option>Uzaktan</option></select></div>
                <div class="notice-card col-12">
                  <strong>Önceki çalışma geçmişi</strong>
                  <p>Yeniden işe alım kararları için değerlendirme notu bırakın.</p>
                  <div class="form-grid-2">
                    <div class="input-group"><label class="input-label">Tekrar Çalışma Durumu</label><select class="input-control"><option>Değerlendirilmedi</option><option>Çalışılabilir</option><option>Kararsız</option><option>Çalışılmaz</option></select></div>
                    <div class="input-group"><label class="input-label">Eski Çıkış Kodu</label><select class="input-control"><option>Yok</option><option>Kod-03 (İstifa)</option></select></div>
                  </div>
                </div>
              </div>
            </div>

            <div id="tab-mali" class="content-section">
              <div class="section-head"><div><h3>Mali & Yan Haklar</h3><span>Banka, BES ve yan hak tanımlamaları.</span></div></div>
              <div class="form-grid">
                <div class="input-group col-12"><label class="input-label">IBAN Numarası</label><input type="text" class="input-control mono" placeholder="TR..." /></div>
                <div class="input-group col-6"><label class="input-label">Banka Adı</label><input type="text" class="input-control" /></div>
                <div class="input-group col-6"><label class="input-label">Maaş Tipi</label><select class="input-control"><option>Net Maaş</option><option>Brüt Maaş</option></select></div>
                <div class="input-group col-6"><label class="input-label">BES Durumu</label><select class="input-control"><option>Otomatik Katılım</option><option>İptal</option><option>Muaf</option></select></div>
                <div class="input-group col-6"><label class="input-label">Yemek Kartı</label><input type="text" class="input-control" /></div>
              </div>
            </div>

            <div id="tab-ozluk" class="content-section">
              <div class="section-head"><div><h3>Özlük & Sağlık</h3><span>Zimmet ve İSG süreçleri için ek bilgiler.</span></div></div>
              <div class="form-grid">
                <div class="input-group col-3"><label class="input-label">T-Shirt</label><select class="input-control"><option>S</option><option>M</option><option>L</option><option>XL</option></select></div>
                <div class="input-group col-3"><label class="input-label">Pantolon</label><select class="input-control"><option>30</option><option>32</option><option>34</option></select></div>
                <div class="input-group col-3"><label class="input-label">Mont</label><select class="input-control"><option>M</option><option>L</option></select></div>
                <div class="input-group col-3"><label class="input-label">Ayakkabı</label><input type="number" class="input-control" value="42" /></div>
                <label class="check-card col-4"><input type="checkbox" /> <span>Yüksekte çalışabilir</span></label>
                <label class="check-card col-4"><input type="checkbox" /> <span>Gece vardiyası</span></label>
                <label class="check-card col-4"><input type="checkbox" /> <span>Ağır yük taşıma</span></label>
                <div class="input-group col-12"><label class="input-label">Bilinen Hastalık / Notlar</label><textarea class="input-control" rows="2"></textarea></div>
              </div>
            </div>

            <div id="tab-evrak" class="content-section">
              <div class="upload-drop">
                <i aria-hidden="true" class="fa-solid fa-cloud-arrow-up"></i>
                <h4>Dosyaları sürükleyip bırakın</h4>
                <p>Nüfus cüzdanı, ikametgah, adli sicil ve diğer özlük evrakları.</p>
              </div>
            </div>
          </main>
        </div>
      </div>
    </div>
  `;
};

/* ---------- Liste filtreleme ---------- */

window.filterPersonnel = () => {
  const query = document.getElementById("personnel-search")?.value.trim().toLocaleLowerCase("tr-TR") || "";
  const dept = document.getElementById("personnel-dept-filter")?.value || "";
  const status = document.getElementById("personnel-status-filter")?.value || "";
  let visible = 0;

  document.querySelectorAll("#personnel-table tbody tr").forEach((row) => {
    const matches =
      (!query || row.dataset.search.includes(query)) &&
      (!dept || row.dataset.dept === dept) &&
      (!status || row.dataset.status === status);
    row.style.display = matches ? "" : "none";
    if (matches) visible += 1;
  });

  const empty = document.getElementById("personnel-empty");
  if (empty) empty.hidden = visible !== 0;
};

/* ---------- Toplu seçim ---------- */

const getCheckedPersonnelRows = () =>
  Array.from(document.querySelectorAll("#personnel-table .personnel-row-check:checked"));

window.updatePersonnelBulkBar = () => {
  const bar = document.getElementById("personnel-bulk-bar");
  const count = document.getElementById("personnel-bulk-count");
  const checked = getCheckedPersonnelRows();

  if (bar) bar.hidden = checked.length === 0;
  if (count) count.textContent = `${checked.length} kişi seçildi`;
};

window.toggleAllPersonnel = (master) => {
  document.querySelectorAll("#personnel-table .personnel-row-check").forEach((box) => {
    const row = box.closest("tr");
    if (row && row.style.display !== "none") box.checked = master.checked;
  });
  updatePersonnelBulkBar();
};

window.clearPersonnelSelection = () => {
  document
    .querySelectorAll("#personnel-table input[type='checkbox']")
    .forEach((box) => (box.checked = false));
  updatePersonnelBulkBar();
};

window.bulkDeactivatePersonnel = () => {
  const checked = getCheckedPersonnelRows();
  checked.forEach((box) => {
    const badge = box.closest("tr")?.querySelector(".badge");
    if (badge) {
      badge.className = "badge badge-passive";
      badge.textContent = "Pasif";
      box.closest("tr").dataset.status = "passive";
    }
  });
  showToast(`${checked.length} personel pasife alındı.`, "success");
  clearPersonnelSelection();
};

/* ---------- Dışa aktarma: seçim varsa seçilenler, yoksa görünür liste ---------- */

window.exportPersonnel = () => {
  const checked = getCheckedPersonnelRows();
  if (checked.length) {
    exportTableToCSV(
      "#personnel-table",
      "personel-secili",
      (row) => row.closest("thead") || row.querySelector(".personnel-row-check")?.checked
    );
  } else {
    exportTableToCSV("#personnel-table", "personel-listesi");
  }
};

/* ---------- Personel kartı modalı ---------- */

window.openModal = (personName) => {
  const modal = document.getElementById("personnel-modal");
  if (!modal) return;

  const heading = modal.querySelector(".modal-header h2");
  const description = modal.querySelector(".modal-header p");
  if (heading) heading.textContent = personName ? `Personel Kartı — ${personName}` : "Yeni Personel Kartı";
  if (description)
    description.textContent = personName
      ? "Kayıtlı özlük bilgilerini görüntüleyin ve güncelleyin."
      : "Gerekli alanları tamamlayarak özlük kaydını oluşturun.";

  modal.style.display = "flex";
};

window.closeModal = () => {
  document.getElementById("personnel-modal").style.display = "none";
};

window.switchFormTab = (tabId) => {
  document.querySelectorAll(".nav-btn").forEach((btn) => btn.classList.remove("active"));
  document
    .querySelectorAll(".content-section")
    .forEach((sec) => sec.classList.remove("active"));

  const activeBtn = Array.from(document.querySelectorAll(".nav-btn")).find((btn) =>
    btn.getAttribute("onclick").includes(tabId)
  );
  if (activeBtn) activeBtn.classList.add("active");

  document.getElementById(tabId).classList.add("active");
};

window.savePerson = () => {
  closeModal();
  showToast("Personel kaydı başarıyla oluşturuldu.", "success");
};
