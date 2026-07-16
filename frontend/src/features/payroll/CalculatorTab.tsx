import { useEffect, useState } from "react";
import { PageError, PageLoading } from "../shared/PageState";
import { formatPayrollMoney, formatPayrollNumber } from "./format";
import {
  usePayrollPeriod, usePayrollPeriods, usePayrollPreview, usePayrollSettings,
} from "./queries";

const parseNumber = (value: string, fallback = 0): number => {
  const parsed = Number(String(value ?? "").replace(",", "."));
  return Number.isFinite(parsed) ? parsed : fallback;
};

type ScenarioForm = {
  gross: string; days: string; overtimeHours: string; overtimeMultiplier: string;
  premium: string; road: string; meal: string; benefit: string; deductions: string; taxBase: string;
};

export function CalculatorTab() {
  const settingsQ = usePayrollSettings(true);
  const periodsQ = usePayrollPeriods(true);
  const latestPeriodId = periodsQ.data?.[0]?.id ?? null;
  const detailQ = usePayrollPeriod(latestPeriodId);
  const preview = usePayrollPreview();

  const [employeeRowId, setEmployeeRowId] = useState("");
  const [form, setForm] = useState<ScenarioForm | null>(null);

  const settings = settingsQ.data;

  // İlk form değerleri ayarlardan gelir.
  useEffect(() => {
    if (form === null && settings) {
      setForm({
        gross: "0", days: String(settings.defaultWorkedDays ?? 30), overtimeHours: "3",
        overtimeMultiplier: String(settings.overtimeMultiplier ?? 1.5),
        premium: "0", road: "0", meal: "0", benefit: "0", deductions: "0", taxBase: "0",
      });
    }
  }, [form, settings]);

  // Form değişikliğinden 300ms sonra server-side ön hesap (personel arama debounce deseni).
  useEffect(() => {
    if (!form) return;
    const timer = setTimeout(() => {
      preview.mutate({
        grossSalary: parseNumber(form.gross),
        workedDays: Math.round(parseNumber(form.days, 30)),
        overtimeHours: parseNumber(form.overtimeHours),
        overtimeMultiplier: parseNumber(form.overtimeMultiplier, 1.5),
        premiumPay: parseNumber(form.premium),
        roadAllowance: parseNumber(form.road),
        mealAllowance: parseNumber(form.meal),
        benefitPay: parseNumber(form.benefit),
        specialDeductions: parseNumber(form.deductions),
        previousTaxBase: parseNumber(form.taxBase),
      });
    }, 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [form]);

  if (settingsQ.isPending || periodsQ.isPending || form === null) return <PageLoading />;
  if (settingsQ.isError) return <PageError error={settingsQ.error} />;
  if (periodsQ.isError) return <PageError error={periodsQ.error} />;

  const rows = detailQ.data?.rows ?? [];
  const selectedRow = rows.find((r) => String(r.id) === employeeRowId);
  const result = preview.data;

  const fillFromRow = (rowId: string) => {
    setEmployeeRowId(rowId);
    const row = rows.find((r) => String(r.id) === rowId);
    if (!row) return;
    setForm({
      gross: String(row.grossSalary ?? 0),
      days: String(settings!.defaultWorkedDays ?? 30),
      overtimeHours: "3",
      overtimeMultiplier: String(settings!.overtimeMultiplier ?? 1.5),
      premium: String(row.premiumPay ?? 0),
      road: String(row.roadAllowance ?? 0),
      meal: String(row.mealAllowance ?? 0),
      benefit: String(row.benefitPay ?? 0),
      deductions: "0",
      taxBase: String(row.previousTaxBase ?? 0),
    });
  };

  const setField = (key: keyof ScenarioForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f!, [key]: e.target.value }));

  const fields: [keyof ScenarioForm, string, string][] = [
    ["gross", "Brüt ücret", "1"],
    ["days", "Çalışılan gün", "1"],
    ["overtimeHours", "Fazla mesai saati", "1"],
    ["overtimeMultiplier", "Fazla mesai çarpanı", "0.1"],
    ["premium", "Prim", "1"],
    ["road", "Yol yardımı", "1"],
    ["meal", "Yemek yardımı", "1"],
    ["benefit", "Yan hak / ek ödeme", "1"],
    ["deductions", "Özel kesinti", "1"],
    ["taxBase", "Önceki GV matrahı", "1"],
  ];

  return (
    <div className="payroll-calculator-grid">
      <section className="card payroll-calculator-form">
        <div className="card-header-clean">
          <div>
            <h4>Tekil Hesaplama</h4>
            <p className="text-muted">Personel seçin, fazla mesai ve kazanç kalemlerini girerek ön hesap alın.</p>
          </div>
          <span className="status-pill info">Senaryo</span>
        </div>
        <div className="form-grid">
          <div className="input-group col-6">
            <label className="input-label" htmlFor="scenario-employee">Personel</label>
            <select
              id="scenario-employee"
              className="input-control"
              value={employeeRowId}
              onChange={(e) => fillFromRow(e.target.value)}
            >
              <option value="">Serbest giriş</option>
              {rows.map((r) => (
                <option key={r.id} value={r.id}>{r.name} - {r.title}</option>
              ))}
            </select>
          </div>
          {fields.map(([key, label, step]) => (
            <div key={key} className="input-group col-3">
              <label className="input-label" htmlFor={`scenario-${key}`}>{label}</label>
              <input
                id={`scenario-${key}`}
                className="input-control"
                type="number"
                step={step}
                value={form[key]}
                onChange={setField(key)}
              />
            </div>
          ))}
        </div>
        <div className="payroll-note">
          <i aria-hidden="true" className="fa-solid fa-circle-info" />
          <span>Fazla mesai tutarı, brüt ücret / {formatPayrollNumber(settings!.monthlyWorkingHours)} saat üzerinden çarpanla hesaplanır.</span>
        </div>
      </section>

      <aside className="card payroll-scenario-result" id="payroll-scenario-result">
        {result ? (
          <>
            <div className="scenario-result-head">
              <div>
                <span className="status-pill info">Ön hesap</span>
                <h4>{selectedRow?.name ?? "Tekil Hesaplama"}</h4>
                <p>Bu hesap dönem bordrosuna işlenmedi.</p>
              </div>
              <strong>{formatPayrollMoney(result.netPay)}</strong>
            </div>
            <div className="scenario-highlight-grid">
              <div><span>Saatlik ücret</span><strong>{formatPayrollMoney(result.hourlyRate)}</strong></div>
              <div>
                <span>Fazla mesai</span><strong>{formatPayrollMoney(result.overtimePay)}</strong>
                <small>{form.overtimeHours} saat × {formatPayrollNumber(parseNumber(form.overtimeMultiplier, 1.5))}</small>
              </div>
              <div><span>Toplam brüt</span><strong>{formatPayrollMoney(result.grossEarnings)}</strong></div>
              <div><span>İşveren maliyeti</span><strong>{formatPayrollMoney(result.employerCost)}</strong></div>
            </div>
            <div className="scenario-breakdown-grid">
              <section>
                <h5>Kazançlar</h5>
                <div className="payroll-line"><span>Brüt ücret</span><strong>{formatPayrollMoney(result.baseGross)}</strong></div>
                <div className="payroll-line"><span>Fazla mesai</span><strong>{formatPayrollMoney(result.overtimePay)}</strong></div>
                <div className="payroll-line"><span>Prim</span><strong>{formatPayrollMoney(parseNumber(form.premium))}</strong></div>
                <div className="payroll-line"><span>Yol yardımı</span><strong>{formatPayrollMoney(parseNumber(form.road))}</strong></div>
                <div className="payroll-line"><span>Yemek yardımı</span><strong>{formatPayrollMoney(parseNumber(form.meal))}</strong></div>
                <div className="payroll-line"><span>Yan hak / ek ödeme</span><strong>{formatPayrollMoney(parseNumber(form.benefit))}</strong></div>
              </section>
              <section>
                <h5>Kesintiler</h5>
                <div className="payroll-line deduction"><span>SGK işçi payı</span><strong>{formatPayrollMoney(result.sgkEmployee)}</strong></div>
                <div className="payroll-line deduction"><span>İşsizlik işçi payı</span><strong>{formatPayrollMoney(result.unemploymentEmployee)}</strong></div>
                <div className="payroll-line deduction"><span>Gelir vergisi</span><strong>{formatPayrollMoney(result.incomeTax)}</strong></div>
                <div className="payroll-line deduction"><span>Damga vergisi</span><strong>{formatPayrollMoney(result.stampTax)}</strong></div>
                <div className="payroll-line deduction"><span>Özel kesinti</span><strong>{formatPayrollMoney(parseNumber(form.deductions))}</strong></div>
              </section>
              <section>
                <h5>Matrahlar</h5>
                <div className="payroll-line"><span>SGK matrahı</span><strong>{formatPayrollMoney(result.sgkBase)}</strong></div>
                <div className="payroll-line"><span>Gelir vergisi matrahı</span><strong>{formatPayrollMoney(result.incomeTaxBase)}</strong></div>
                <div className="payroll-line"><span>Önceki kümülatif</span><strong>{formatPayrollMoney(parseNumber(form.taxBase))}</strong></div>
                <div className="payroll-line"><span>Dönem sonu kümülatif</span><strong>{formatPayrollMoney(parseNumber(form.taxBase) + (result.incomeTaxBase ?? 0))}</strong></div>
              </section>
            </div>
          </>
        ) : (
          <p className="pending-desc">Hesaplanıyor...</p>
        )}
      </aside>
    </div>
  );
}
