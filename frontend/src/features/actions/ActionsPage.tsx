import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { PageError, PageLoading } from "../shared/PageState";
import { formatTimeAgo } from "../recruitment/format";
import { appRoutes } from "../../routes";
import { actionLevelText, actionPillClass, actionStatusText } from "./format";
import { useAuditLogs, useGlobalActions, type GlobalActionDto } from "./queries";

const TABS: [string, string][] = [["open", "Açık"], ["week", "Bu Hafta"], ["done", "Tamamlanan"]];

const routePathFor = (routeKey?: string | null): string | null =>
  appRoutes.find((route) => route.key === routeKey)?.path ?? null;

export function ActionsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const isMgmt = user?.role === "hr-admin" || user?.role === "manager";
  const [tab, setTab] = useState("open");
  const [filters, setFilters] = useState({ priority: "", source: "", owner: "" });
  const [createOpen, setCreateOpen] = useState(false);

  const actionsQ = useGlobalActions(filters);
  const optionsQ = useGlobalActions({ priority: "", source: "", owner: "" });
  const auditQ = useAuditLogs(isMgmt && tab === "audit");

  if (actionsQ.isPending) return <PageLoading />;
  if (actionsQ.isError) return <PageError error={actionsQ.error} />;

  const actions = actionsQ.data;
  const allActions = optionsQ.data ?? actions;
  const kpis = {
    today: actions.filter((item) => item.due === "Bugün").length,
    overdue: actions.filter((item) => item.due === "Gecikti").length,
    high: actions.filter((item) => item.priority === "high" && item.status !== "done").length,
    done: actions.filter((item) => item.status === "done").length,
  };
  const sources = [...new Set(allActions.map((item) => item.source ?? ""))].filter(Boolean);
  const owners = [...new Set(allActions.map((item) => item.owner ?? ""))].filter(Boolean);
  const visible = tab === "audit" ? [] : actions.filter((item) => item.status === tab);

  const setFilter = (key: "priority" | "source" | "owner") =>
    (e: React.ChangeEvent<HTMLSelectElement>) =>
      setFilters((f) => ({ ...f, [key]: e.target.value }));

  const renderCard = (action: GlobalActionDto) => {
    const sourcePath = routePathFor(action.sourceRoute);
    return (
      <article key={action.id} className={`global-action-card ${action.priority}`}>
        <div className="global-action-top">
          <span className={`status-pill ${actionPillClass(action.priority)}`}>{actionLevelText(action.priority)}</span>
          <span>{action.due}</span>
        </div>
        <h4>{action.title}</h4>
        <p>{action.action}</p>
        <div className="global-action-meta">
          <span><i aria-hidden="true" className="fa-solid fa-layer-group" /> {action.source}</span>
          <span><i aria-hidden="true" className="fa-solid fa-user" /> {action.owner}</span>
        </div>
        <div className="global-action-footer">
          <span className="status-pill info">{actionStatusText(action.status)}</span>
          <div className="toolbar-actions">
            {/* Durum ilerletme + silme Task 3'te */}
            {sourcePath && (
              <button className="btn btn-secondary btn-sm" onClick={() => navigate(sourcePath)}>Kaynağa git</button>
            )}
          </div>
        </div>
      </article>
    );
  };

  return (
    <div id="actions-screen">
      <div className="page-header">
        <div>
          <h2>Global Aksiyon Merkezi</h2>
          <p>Risk, bordro, uyum ve çalışan deneyimi aksiyonlarını tek operasyon merkezinde takip edin.</p>
        </div>
        {/* Yeni Aksiyon butonu Task 3'te (yalnız hr-admin) */}
      </div>

      <div className="actions-kpi-grid">
        <div className="stat-box"><span className="sb-label">Bugün</span><strong className="sb-val">{kpis.today}</strong><small>Kapanması beklenen</small></div>
        <div className="stat-box"><span className="sb-label">Geciken</span><strong className="sb-val text-red">{kpis.overdue}</strong><small>Riskli aksiyon</small></div>
        <div className="stat-box"><span className="sb-label">Yüksek Öncelik</span><strong className="sb-val text-orange">{kpis.high}</strong><small>Aktif takip</small></div>
        <div className="stat-box"><span className="sb-label">Tamamlanan</span><strong className="sb-val">{kpis.done}</strong><small>Bu hafta</small></div>
      </div>

      <section className="card actions-filter-bar">
        <label className="sr-only" htmlFor="actions-priority-filter">Öncelik filtresi</label>
        <select id="actions-priority-filter" className="small-select" value={filters.priority} onChange={setFilter("priority")}>
          <option value="">Öncelik: Tümü</option>
          <option value="high">Yüksek</option>
          <option value="medium">Orta</option>
          <option value="low">Düşük</option>
        </select>
        <label className="sr-only" htmlFor="actions-source-filter">Kaynak filtresi</label>
        <select id="actions-source-filter" className="small-select" value={filters.source} onChange={setFilter("source")}>
          <option value="">Kaynak: Tümü</option>
          {sources.map((source) => <option key={source} value={source}>{source}</option>)}
        </select>
        <label className="sr-only" htmlFor="actions-owner-filter">Sahip filtresi</label>
        <select id="actions-owner-filter" className="small-select" value={filters.owner} onChange={setFilter("owner")}>
          <option value="">Sahip: Tümü</option>
          {owners.map((owner) => <option key={owner} value={owner}>{owner}</option>)}
        </select>
      </section>

      <div className="actions-tabs">
        {TABS.map(([key, label]) => (
          <button key={key} className={`action-tab ${tab === key ? "active" : ""}`} onClick={() => setTab(key)}>
            {label}
          </button>
        ))}
        {isMgmt && (
          <button className={`action-tab ${tab === "audit" ? "active" : ""}`} onClick={() => setTab("audit")}>
            Denetim İzi
          </button>
        )}
      </div>

      {tab !== "audit" && (
        <section className="actions-tab-content active">
          <div className="global-actions-grid">
            {visible.map(renderCard)}
            {visible.length === 0 && <div className="empty-lane">Bu filtrede aksiyon yok.</div>}
          </div>
        </section>
      )}

      {tab === "audit" && (
        <section className="actions-tab-content active">
          <section className="card">
            {auditQ.isPending && <PageLoading />}
            {auditQ.isError && <PageError error={auditQ.error} />}
            {auditQ.data && (
              <div className="audit-timeline">
                {auditQ.data.map((log) => (
                  <div key={log.id} className="audit-item">
                    <div className="audit-dot" />
                    <div className="audit-body">
                      <div className="audit-head">
                        <strong>{log.action}</strong>
                        <span>{formatTimeAgo(log.createdAtUtc)}</span>
                      </div>
                      <p>{log.detail}</p>
                      <small>{log.actor} · {log.module}</small>
                    </div>
                  </div>
                ))}
                {auditQ.data.length === 0 && <p className="pending-desc">Henüz denetim kaydı yok.</p>}
              </div>
            )}
          </section>
        </section>
      )}

      {/* Yeni Aksiyon modalı Task 3'te */}
      {createOpen && null}
    </div>
  );
}
