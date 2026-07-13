const Settings = () => {
  return `
    <div id="settings-screen">
      <div class="page-header">
        <div>
          <h2>Sistem Ayarları</h2>
          <p>Şirket profili, bildirimler, güvenlik ve abonelik tercihlerini yönetin.</p>
        </div>
        <button class="btn btn-primary" onclick="saveSettings()">
          <i aria-hidden="true" class="fa-solid fa-save"></i> Değişiklikleri Kaydet
        </button>
      </div>

      <div class="settings-layout">
        <aside class="settings-sidebar">
          <nav class="set-nav">
            <button class="set-link active" onclick="switchSetTab('set-general', event)">
              <i aria-hidden="true" class="fa-solid fa-building"></i> Şirket Bilgileri
            </button>
            <button class="set-link" onclick="switchSetTab('set-notif', event)">
              <i aria-hidden="true" class="fa-solid fa-bell"></i> Bildirimler
            </button>
            <button class="set-link" onclick="switchSetTab('set-security', event)">
              <i aria-hidden="true" class="fa-solid fa-shield-halved"></i> Güvenlik & Yetki
            </button>
            <button class="set-link" onclick="switchSetTab('set-billing', event)">
              <i aria-hidden="true" class="fa-solid fa-credit-card"></i> Abonelik & Fatura
            </button>
          </nav>
        </aside>

        <main class="settings-content">
          <div id="set-general" class="set-section active">
            <div class="set-card">
              <div class="card-head">
                <h3>Marka & Görünüm</h3>
                <p>Sistemde görünecek şirket adı ve marka varlıkları.</p>
              </div>
              <div class="form-row">
                <div class="logo-upload">
                  <div class="current-logo"><i aria-hidden="true" class="fa-solid fa-building"></i></div>
                  <div>
                    <button class="btn-outline">Logo Yükle</button>
                    <small>PNG, JPG, maksimum 2 MB</small>
                  </div>
                </div>
              </div>
              <div class="form-grid-2">
                <div class="input-group">
                  <label>Şirket Adı</label>
                  <input type="text" value="HR Master Teknoloji A.Ş." class="input-control" />
                </div>
                <div class="input-group">
                  <label>Web Sitesi</label>
                  <input type="text" value="www.hrmaster.com" class="input-control" />
                </div>
              </div>
            </div>

            <div class="set-card mt-4">
              <div class="card-head"><h3>İletişim Bilgileri</h3></div>
              <div class="form-grid-2">
                <div class="input-group">
                  <label>E-Posta (Sistem)</label>
                  <input type="email" value="info@hrmaster.com" class="input-control" />
                </div>
                <div class="input-group">
                  <label>Telefon</label>
                  <input type="tel" value="+90 212 555 00 00" class="input-control" />
                </div>
                <div class="input-group col-span-2">
                  <label>Merkez Adres</label>
                  <textarea rows="2" class="input-control">Maslak Mah. Büyükdere Cad. No:123 Sarıyer/İstanbul</textarea>
                </div>
              </div>
            </div>
          </div>

          <div id="set-notif" class="set-section">
            <div class="set-card">
              <div class="card-head"><h3>E-Posta Bildirimleri</h3></div>
              <div class="toggle-row">
                <div><strong>Yeni Personel Kaydı</strong><p>Sisteme yeni biri eklendiğinde yöneticilere bildir.</p></div>
                <label class="switch"><input type="checkbox" checked /><span class="slider round"></span></label>
              </div>
              <div class="divider"></div>
              <div class="toggle-row">
                <div><strong>İzin Talepleri</strong><p>Personel izin talebi oluşturduğunda anında e-posta gönder.</p></div>
                <label class="switch"><input type="checkbox" checked /><span class="slider round"></span></label>
              </div>
              <div class="divider"></div>
              <div class="toggle-row">
                <div><strong>Haftalık Rapor</strong><p>Her pazartesi sabahı özet operasyon raporu gönder.</p></div>
                <label class="switch"><input type="checkbox" /><span class="slider round"></span></label>
              </div>
            </div>
          </div>

          <div id="set-security" class="set-section">
            <div class="set-card">
              <div class="card-head"><h3>Giriş Güvenliği</h3></div>
              <div class="form-grid-2">
                <div class="input-group">
                  <label>Mevcut Şifre</label>
                  <input type="password" placeholder="••••••••" class="input-control" />
                </div>
              </div>
              <div class="form-grid-2 mt-3">
                <div class="input-group"><label for="new-password">Yeni Şifre</label><input id="new-password" type="password" class="input-control" /></div>
                <div class="input-group"><label for="new-password-repeat">Yeni Şifre (Tekrar)</label><input id="new-password-repeat" type="password" class="input-control" /></div>
              </div>
              <button class="btn btn-secondary mt-4" onclick="updatePassword()">Şifreyi Güncelle</button>
            </div>

            <div class="set-card mt-4">
              <div class="card-head"><h3>İki Aşamalı Doğrulama</h3></div>
              <div class="toggle-row">
                <div><strong>SMS ile doğrulama</strong><p>Giriş yaparken telefonunuza tek kullanımlık kod gönderilir.</p></div>
                <label class="switch"><input type="checkbox" /><span class="slider round"></span></label>
              </div>
            </div>
          </div>

          <div id="set-billing" class="set-section">
            <div class="plan-banner">
              <div class="pb-info">
                <span class="badge-pro">PRO PLAN</span>
                <h3>HR Master Kurumsal</h3>
                <p>Yıllık ödeme planı aktif. Bir sonraki yenileme: <strong>12 Ekim 2025</strong></p>
              </div>
              <div class="pb-price">₺12.000<small>/yıl</small></div>
            </div>

            <div class="set-card mt-4">
              <div class="card-head"><h3>Ödeme Yöntemi</h3></div>
              <div class="cc-preview">
                <div class="cc-icon"><i aria-hidden="true" class="fa-brands fa-cc-mastercard"></i></div>
                <span>•••• •••• •••• 4582</span>
                <button class="btn-text" onclick="showToast('Ödeme yöntemi değişikliği demo kapsamı dışındadır.', 'info')">Değiştir</button>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  `;
};

window.switchSetTab = (tabId, evt) => {
  document.querySelectorAll(".set-link").forEach((button) => button.classList.remove("active"));
  if (evt?.currentTarget) evt.currentTarget.classList.add("active");

  document.querySelectorAll(".set-section").forEach((section) => section.classList.remove("active"));
  document.getElementById(tabId).classList.add("active");
};

window.saveSettings = () => {
  showToast("Ayarlar başarıyla kaydedildi.", "success");
};

window.updatePassword = () => {
  const next = document.getElementById("new-password")?.value;
  const repeat = document.getElementById("new-password-repeat")?.value;

  if (!next || !repeat) {
    showToast("Yeni şifre alanlarını doldurun.", "warning");
    return;
  }
  if (next !== repeat) {
    showToast("Şifreler eşleşmiyor, kontrol edin.", "error");
    return;
  }

  document.getElementById("new-password").value = "";
  document.getElementById("new-password-repeat").value = "";
  showToast("Şifreniz güncellendi.", "success");
};
