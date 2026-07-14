import { useState } from "react";
import { ApiError } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../layout/ToastProvider";
import { formatLeaveDate } from "./format";
import { useCreateLeave, useLeaveBalance, useLeaveTypes, useSubstituteOptions, type LeaveTypeDto } from "./queries";

const typeCardMeta = (type: LeaveTypeDto, remaining: number): { icon: string; tone: string; small: string } => {
  const name = type.name ?? "";
  if (name.includes("Yıllık")) return { icon: "fa-sun", tone: "annual", small: `Kalan: ${remaining} gün` };
  if (name.includes("Rapor")) return { icon: "fa-notes-medical", tone: "sick", small: "Belge gerekli" };
  if (name.includes("Mazeret")) return { icon: "fa-clock", tone: "excuse", small: "Saatlik/günlük" };
  if (name.includes("Uzaktan")) return { icon: "fa-laptop-house", tone: "remote", small: "Evden çalışma" };
  return { icon: "fa-sun", tone: "annual", small: name };
};

export function LeaveRequestModal({ onClose }: { onClose: () => void }) {
  const { user } = useAuth();
  const { showToast } = useToast();
  const typesQ = useLeaveTypes();
  const balanceQ = useLeaveBalance();
  const substitutesQ = useSubstituteOptions(user?.role !== "employee");
  const createLeave = useCreateLeave();

  const [typeId, setTypeId] = useState<number | null>(null);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [description, setDescription] = useState("");
  const [substituteId, setSubstituteId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const types = typesQ.data ?? [];
  const remaining = balanceQ.data?.remainingDays ?? 0;
  const selectedTypeId = typeId ?? types[0]?.id ?? null;

  // Takvim günü ön izlemesi; kesin süreyi backend hesaplar.
  const start = startDate ? new Date(startDate) : null;
  const end = endDate ? new Date(endDate) : null;
  const validRange = start && end && end.getTime() >= start.getTime();
  const previewDays = validRange ? Math.round((end!.getTime() - start!.getTime()) / 86_400_000) + 1 : null;
  const returnDate = validRange ? new Date(end!.getTime() + 86_400_000) : null;

  const submit = async () => {
    setError(null);
    if (!startDate || !endDate) {
      showToast("Başlangıç ve bitiş tarihlerini seçin.", "warning");
      return;
    }
    try {
      await createLeave.mutateAsync({
        leaveTypeId: selectedTypeId ?? undefined,
        startDate,
        endDate,
        description: description || null,
        substituteEmployeeId: substituteId ? Number(substituteId) : null,
      });
      showToast("İzin talebiniz yönetici onayına gönderildi.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
    }
  };

  return (
    <div id="leave-modal-overlay" className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>Yeni İzin Talebi</h3>
            <p>Talep detaylarını net ve eksiksiz doldurun.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="İzin talebi penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          <label className="input-label">İzin Türü</label>
          <div className="type-grid">
            {types.map((type) => {
              const meta = typeCardMeta(type, remaining);
              return (
                <label key={type.id} className="type-card">
                  <input
                    type="radio"
                    name="leaveType"
                    checked={selectedTypeId === type.id}
                    onChange={() => setTypeId(type.id ?? null)}
                  />
                  <div className="tc-content">
                    <div className={`tc-icon ${meta.tone}`}><i aria-hidden="true" className={`fa-solid ${meta.icon}`} /></div>
                    <span>{type.name}</span>
                    <small>{meta.small}</small>
                  </div>
                </label>
              );
            })}
          </div>

          <div className="form-grid-2 mt-4">
            <div className="input-group">
              <label className="input-label" htmlFor="start-date">Başlangıç Tarihi</label>
              <input type="date" className="input-control" id="start-date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="end-date">Bitiş Tarihi</label>
              <input type="date" className="input-control" id="end-date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
            </div>
          </div>

          <div className="calc-box">
            <div className="cb-item"><span>Süre</span><strong id="calc-days">{previewDays !== null ? `${previewDays} gün` : "- gün"}</strong></div>
            <div className="cb-item"><span>İşe dönüş</span><strong id="return-date">{returnDate ? formatLeaveDate(returnDate.toISOString()) : "-"}</strong></div>
          </div>

          <div className="input-group mt-4">
            <label className="input-label" htmlFor="leave-desc">Açıklama / Adres</label>
            <textarea id="leave-desc" className="input-control" rows={2} placeholder="İzin nedeni veya bulunacağınız adres" value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>

          <div className="input-group mt-3">
            <label className="input-label" htmlFor="leave-substitute">Yerine Bakacak Kişi</label>
            <select id="leave-substitute" className="input-control" value={substituteId} onChange={(e) => setSubstituteId(e.target.value)}>
              <option value="">Seçiniz...</option>
              {(substitutesQ.data ?? []).map((emp) => (
                <option key={emp.id} value={emp.id}>{emp.name}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={createLeave.isPending}>
            <i aria-hidden="true" className="fa-solid fa-paper-plane" /> Talebi Gönder
          </button>
        </div>
      </div>
    </div>
  );
}
