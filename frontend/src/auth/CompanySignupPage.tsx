import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch, ApiError } from "../api/client";

/**
 * Public self-servis kayıt: müşteri şirketini + ilk hr-admin'ini oluşturur.
 * Başarılıysa "doğrulama e-postası gönderildi" ekranı gösterilir; hesap, e-postadaki
 * davet bağlantısı (/accept-invite) ile şifre belirlenince etkinleşir.
 */
export function CompanySignupPage() {
  const navigate = useNavigate();
  const [companyName, setCompanyName] = useState("");
  const [adminName, setAdminName] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [doneEmail, setDoneEmail] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiFetch("/tenants/signup", {
        method: "POST",
        body: JSON.stringify({ companyName, adminName, adminEmail }),
      });
      setDoneEmail(adminEmail);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Kayıt tamamlanamadı. Lütfen tekrar deneyin.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="auth-shell">
      <section className="auth-visual">
        <div className="auth-brand">
          <div className="brand-mark"><i aria-hidden="true" className="fa-solid fa-users-gear" /></div>
          <div>
            <strong>İK Pro</strong>
            <span>HR MASTER Suite</span>
          </div>
        </div>
        <div className="auth-copy">
          <span className="status-pill info">Şirket kaydı</span>
          <h1>Şirketinizi dakikalar içinde İK Pro'ya taşıyın.</h1>
          <p>Kaydınızı oluşturun; e-postanıza gelen bağlantıyla şifrenizi belirleyip başlayın.</p>
        </div>
      </section>

      <section className="auth-panel">
        {doneEmail ? (
          <div className="auth-form active" role="status">
            <h2>Doğrulama e-postası gönderildi</h2>
            <p>
              <strong>{doneEmail}</strong> adresine bir doğrulama bağlantısı gönderdik.
              Şifrenizi belirleyip hesabınızı etkinleştirmek için e-postanızdaki bağlantıyı açın.
            </p>
            <button type="button" className="btn btn-primary auth-submit" onClick={() => navigate("/login")}>
              <i aria-hidden="true" className="fa-solid fa-arrow-right-to-bracket" /> Girişe dön
            </button>
          </div>
        ) : (
          <form className="auth-form active" onSubmit={submit}>
            <h2>Şirketinizi kaydedin</h2>
            <p>İlk yönetici hesabınızla şirketinizi oluşturun.</p>
            <div className="input-group">
              <label htmlFor="company-name">Şirket adı</label>
              <input id="company-name" className="input-control" value={companyName}
                onChange={(e) => setCompanyName(e.target.value)} />
            </div>
            <div className="input-group">
              <label htmlFor="admin-name">Ad soyad</label>
              <input id="admin-name" className="input-control" value={adminName}
                onChange={(e) => setAdminName(e.target.value)} />
            </div>
            <div className="input-group">
              <label htmlFor="admin-email">İş e-postası</label>
              <input id="admin-email" className="input-control" value={adminEmail}
                onChange={(e) => setAdminEmail(e.target.value)} />
            </div>
            {error && <p className="form-error" role="alert">{error}</p>}
            <button type="submit" className="btn btn-primary auth-submit" disabled={busy}>
              <i aria-hidden="true" className="fa-solid fa-building-user" />{" "}
              {busy ? "Kaydediliyor…" : "Şirketi kaydol"}
            </button>
          </form>
        )}
      </section>
    </main>
  );
}
