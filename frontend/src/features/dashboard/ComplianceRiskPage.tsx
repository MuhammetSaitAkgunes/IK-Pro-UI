import { PageError, PageLoading } from "../shared/PageState";
import { BackToRisk } from "./BackToRisk";
import { getLevelText } from "./format";
import { useComplianceRisk } from "./queries";

const statusPillClass = (status?: string | null): string =>
  status === "Tamamlandı" ? "approved" : status === "Eksik" ? "rejected" : "pending";

export function ComplianceRiskPage() {
  const compliance = useComplianceRisk();
  if (compliance.isPending) return <PageLoading />;
  if (compliance.isError) return <PageError error={compliance.error} />;

  const data = compliance.data;

  return (
    <div className="detail-page">
      <div className="page-header">
        <div>
          <h2>Uyum, Evrak ve Denetim Risk Merkezi</h2>
          <p>Eksik evrak, yaklaşan son tarih ve denetim hazırlığını operasyonel takip görünümünde yönetin.</p>
        </div>
        <BackToRisk />
      </div>

      <div className="detail-kpi-grid">
        <div className="stat-box"><span className="sb-label">Evrak Uyum Skoru</span><strong className="sb-val">{data.documentComplianceScore ?? 0}<small>/100</small></strong></div>
        <div className="stat-box"><span className="sb-label">Eksik Evrak</span><strong className="sb-val text-red">{data.missingDocuments ?? 0}</strong></div>
        <div className="stat-box"><span className="sb-label">Süresi Yaklaşan</span><strong className="sb-val text-orange">{data.upcomingDocuments ?? 0}</strong></div>
        <div className="stat-box"><span className="sb-label">Denetim Riski</span><strong className="sb-val text-orange">{data.auditReadinessRisk}</strong></div>
      </div>

      <div className="compliance-layout">
        <div className="table-container">
          <table className="detail-table data-table">
            <thead>
              <tr><th>Personel</th><th>Departman</th><th>Evrak</th><th>Sorumlu</th><th>Son Tarih</th><th>Durum</th><th>Risk</th></tr>
            </thead>
            <tbody>
              {(data.records ?? []).map((record) => (
                <tr key={record.id}>
                  <td><strong>{record.employee}</strong></td>
                  <td>{record.dept}</td>
                  <td>{record.document}</td>
                  <td>{record.owner}</td>
                  <td>{record.dueDate}</td>
                  <td><span className={`status-pill ${statusPillClass(record.status)}`}>{record.status}</span></td>
                  <td><span className={`risk-badge ${record.level ?? ""}`}>{getLevelText(record.level)}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <aside className="card insight-panel">
          <div className="card-header-clean">
            <div>
              <h4>Yaklaşan Son Tarihler</h4>
              <p className="text-muted">Kritik evrak aksiyonları ve sorumlular.</p>
            </div>
          </div>
          <div className="deadline-list">
            {(data.deadlines ?? []).map((deadline) => (
              <div key={deadline.title} className={`deadline-item ${deadline.level ?? ""}`}>
                <div>
                  <strong>{deadline.title}</strong>
                  <span>{deadline.count} kayıt · {deadline.owner}</span>
                </div>
                <em>{deadline.dueDate}</em>
              </div>
            ))}
          </div>
        </aside>
      </div>
    </div>
  );
}
