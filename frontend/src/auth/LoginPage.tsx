import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError } from "../api/client";
import { useAuth } from "./AuthContext";
import { roleHomeFor } from "../routes";

export function LoginPage({ mode = "login" }: { mode?: "login" | "signup" }) {
  const { login, register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("ik@hrmaster.local");
  const [password, setPassword] = useState("demo123");
  const [name, setName] = useState("İK Yöneticisi");
  const [signupEmail, setSignupEmail] = useState("ik@hrmaster.local");
  const [signupPassword, setSignupPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  const run = async (event: FormEvent, action: () => Promise<unknown>) => {
    event.preventDefault();
    setError(null);
    try {
      const user = (await action()) as { role?: string };
      navigate(roleHomeFor(user?.role));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
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
          <span className="status-pill info">Demo erişim</span>
          <h1>Risk, bordro ve İK operasyonlarını tek merkezden yönetin.</h1>
          <p>Bu giriş ekranı gerçek backend oturumu açar; yetki kontrolü .NET policy katmanındadır.</p>
        </div>
        <div className="auth-insight-grid">
          <div><strong>7</strong><span>kritik aksiyon</span></div>
          <div><strong>5</strong><span>bordro kontrolü</span></div>
          <div><strong>82</strong><span>uyum skoru</span></div>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-tabs">
          <button className={`auth-tab ${mode === "login" ? "active" : ""}`} onClick={() => navigate("/login")}>Giriş yap</button>
          <button className={`auth-tab ${mode === "signup" ? "active" : ""}`} onClick={() => navigate("/signup")}>Hesap oluştur</button>
        </div>

        <form id="auth-login" className={`auth-form ${mode === "login" ? "active" : ""}`} onSubmit={(e) => run(e, () => login(email, password))}>
          <h2>Hoş geldiniz</h2>
          <p>Demo hesaba giriş yaparak uygulamayı inceleyebilirsiniz.</p>
          <div className="input-group">
            <label htmlFor="login-email">E-posta</label>
            <input id="login-email" className="input-control" value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="input-group">
            <label htmlFor="login-password">Şifre</label>
            <input id="login-password" className="input-control" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          {mode === "login" && error && <p className="form-error" role="alert">{error}</p>}
          <button type="submit" className="btn btn-primary auth-submit">
            <i aria-hidden="true" className="fa-solid fa-arrow-right-to-bracket" /> Giriş yap
          </button>
        </form>

        <form id="auth-signup" className={`auth-form ${mode === "signup" ? "active" : ""}`} onSubmit={(e) => run(e, () => register(name, signupEmail, signupPassword))}>
          <h2>Demo hesap oluştur</h2>
          <p>Bilgiler gerçek backend'de kullanıcı kaydı oluşturur.</p>
          <div className="input-group">
            <label htmlFor="signup-name">Ad soyad</label>
            <input id="signup-name" className="input-control" value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="input-group">
            <label htmlFor="signup-email">İş e-postası</label>
            <input id="signup-email" className="input-control" value={signupEmail} onChange={(e) => setSignupEmail(e.target.value)} />
          </div>
          <div className="input-group">
            <label htmlFor="signup-password">Şifre</label>
            <input id="signup-password" className="input-control" type="password" value={signupPassword} onChange={(e) => setSignupPassword(e.target.value)} />
          </div>
          {mode === "signup" && error && <p className="form-error" role="alert">{error}</p>}
          <button type="submit" className="btn btn-primary auth-submit">
            <i aria-hidden="true" className="fa-solid fa-user-plus" /> Hesap oluştur
          </button>
        </form>
      </section>
    </main>
  );
}
