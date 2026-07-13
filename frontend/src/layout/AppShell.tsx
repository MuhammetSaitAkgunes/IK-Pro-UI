import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { appRoutes, navGroups, navIcons, roleHomeFor, type AppRoute } from "../routes";
import { GlobalSearch } from "./GlobalSearch";

const DEMO_PASSWORD = "demo123";
const demoEmails: Record<string, string> = {
  "hr-admin": "ik@hrmaster.local",
  manager: "ece.arslan@hrmaster.local",
  employee: "ahmet.yilmaz@hrmaster.local",
};

export function AppShell() {
  const { user, login, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [theme, setTheme] = useState(() => localStorage.getItem("ikpro-theme") || "light");
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem("ikpro-sidebar") === "collapsed");

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("ikpro-theme", theme);
  }, [theme]);
  useEffect(() => {
    localStorage.setItem("ikpro-sidebar", collapsed ? "collapsed" : "expanded");
  }, [collapsed]);

  const badge = useQuery({
    queryKey: ["actions", "badge"],
    queryFn: () => apiFetch<{ openCount: number }>("/actions/badge"),
  });
  const actionCount = badge.data?.openCount ?? 0;

  const current: AppRoute | undefined = appRoutes.find((r) => r.path === location.pathname);
  const role = (user?.role ?? "employee") as AppRoute["roles"][number];
  const accessibleNav = appRoutes.filter((r) => r.navKey === r.key && r.roles.includes(role));

  const switchDemoRole = async (nextRole: string) => {
    await login(demoEmails[nextRole] ?? demoEmails["hr-admin"], DEMO_PASSWORD);
    navigate(roleHomeFor(nextRole));
  };

  const handleLogout = async () => {
    await logout();
    navigate("/login");
  };

  return (
    <div className={`app-container ${collapsed ? "sidebar-collapsed" : ""}`}>
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark"><i aria-hidden="true" className="fa-solid fa-users-gear" /></div>
          <div>
            <strong>İK Pro</strong>
            <span>HR MASTER Suite</span>
          </div>
        </div>
        <ul className="nav-links">
          {navGroups.map((group) => {
            const routes = group.keys
              .map((key) => accessibleNav.find((r) => r.key === key))
              .filter((r): r is AppRoute => Boolean(r));
            if (!routes.length) return null;
            return (
              <li key={group.label} style={{ display: "contents" }}>
                <span className="nav-group-label">{group.label}</span>
                {routes.map((route) => (
                  <NavLink
                    key={route.key}
                    to={route.path}
                    data-page={route.key}
                    className={() => `nav-item ${current?.navKey === route.key ? "active" : ""}`}
                  >
                    <i aria-hidden="true" className={`fa-solid ${navIcons[route.key]}`} /> <span>{route.title}</span>
                  </NavLink>
                ))}
              </li>
            );
          })}
        </ul>
        <div className="sidebar-insight">
          <span>Bugünkü öncelik</span>
          <strong>{actionCount} açık aksiyon</strong>
          <small>Risk, bordro ve uyum takibi</small>
        </div>
      </aside>

      <header className="header">
        <div className="header-tools">
          <button className="btn-icon shell-toggle" onClick={() => setCollapsed((c) => !c)} title="Menüyü daralt/genişlet" aria-label="Menüyü daralt veya genişlet">
            <i aria-hidden="true" className="fa-solid fa-bars-staggered" />
          </button>
          <button className="btn-icon theme-toggle" onClick={() => setTheme(theme === "dark" ? "light" : "dark")} title="Tema değiştir" aria-label="Açık/koyu tema değiştir">
            <i id="theme-icon" aria-hidden="true" className={`fa-solid ${theme === "dark" ? "fa-sun" : "fa-moon"}`} />
          </button>
        </div>
        <div className="header-heading">
          <span id="page-eyebrow">{current?.eyebrow ?? "İK Pro"}</span>
          <div className="header-title" id="page-title">{current?.title ?? "Yakında"}</div>
        </div>
        <button className="header-action-button" onClick={() => navigate("/actions")} title="Aksiyon Merkezi" aria-label={`Aksiyon Merkezi: ${actionCount} açık aksiyon`}>
          <i aria-hidden="true" className="fa-solid fa-list-check" />
          <span>{actionCount}</span>
        </button>
        <GlobalSearch />
        <div className="user-profile">
          <div className="user-avatar" aria-hidden="true">{user?.initials}</div>
          <div>
            <strong>{user?.name}</strong>
            <span>{user?.roleLabel}</span>
          </div>
          <select className="role-switcher" value={role} onChange={(e) => switchDemoRole(e.target.value)} title="Demo rol değiştir" aria-label="Demo rol değiştir">
            <option value="hr-admin">İK Admin</option>
            <option value="manager">Yönetici</option>
            <option value="employee">Çalışan</option>
          </select>
          <button className="btn-icon-sm" onClick={handleLogout} title="Çıkış yap" aria-label="Çıkış yap">
            <i aria-hidden="true" className="fa-solid fa-arrow-right-from-bracket" />
          </button>
        </div>
      </header>

      <main className="main-content" id="main-content">
        <Outlet />
      </main>
    </div>
  );
}
