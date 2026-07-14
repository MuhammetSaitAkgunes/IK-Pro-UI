import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { useEmployeeOptions, useSaveAttendanceEntry, type TimesheetRowDto } from "./queries";

export function AttendanceEntryModal({ rowId, initial, defaultEmployeeId, onClose }: {
  rowId: number | null;
  initial?: TimesheetRowDto;
  defaultEmployeeId: number | null;
  onClose: () => void;
}) {
  const { showToast } = useToast();
  const employeesQ = useEmployeeOptions();
  const save = useSaveAttendanceEntry();

  const [employeeId, setEmployeeId] = useState(String(defaultEmployeeId ?? ""));
  const [workDate, setWorkDate] = useState(initial?.workDate ?? "");
  const [type, setType] = useState(initial?.type ?? "Tam");
  const [checkIn, setCheckIn] = useState(initial?.checkIn?.slice(0, 5) ?? "");
  const [checkOut, setCheckOut] = useState(initial?.checkOut?.slice(0, 5) ?? "");
  const [breakMinutes, setBreakMinutes] = useState(String(initial?.breakMinutes ?? 60));
  const [note, setNote] = useState(initial?.note ?? "");
  const [error, setError] = useState<string | null>(null);

  const isEdit = rowId !== null;

  const submit = async () => {
    setError(null);
    if (!workDate) {
      showToast("Tarih seçin.", "warning");
      return;
    }
    try {
      await save.mutateAsync({
        id: rowId,
        employeeId: Number(employeeId) || 0,
        model: {
          workDate,
          checkIn: checkIn || null,
          checkOut: checkOut || null,
          breakMinutes: Number(breakMinutes) || 0,
          type,
          note: note || null,
        },
      });
      showToast(isEdit ? "Puantaj kaydı güncellendi." : "Manuel giriş eklendi.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
    }
  };

  return (
    <div className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>{isEdit ? "Puantaj Kaydını Düzenle" : "Manuel Puantaj Girişi"}</h3>
            <p>Giriş-çıkış saatlerini ve gün tipini doğru girin.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="Puantaj penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          {!isEdit && (
            <div className="input-group">
              <label className="input-label" htmlFor="ae-employee">Personel</label>
              <select id="ae-employee" className="input-control" value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
                {(employeesQ.data ?? []).map((emp) => (
                  <option key={emp.id} value={emp.id}>{emp.name} ({emp.department})</option>
                ))}
              </select>
            </div>
          )}
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-date">Tarih</label>
              <input id="ae-date" type="date" className="input-control" value={workDate} onChange={(e) => setWorkDate(e.target.value)} disabled={isEdit} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-type">Tip</label>
              <select id="ae-type" className="input-control" value={type ?? "Tam"} onChange={(e) => setType(e.target.value)}>
                <option>Tam</option><option>Mesai</option><option>Rapor</option>
              </select>
            </div>
          </div>
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-in">Giriş</label>
              <input id="ae-in" type="time" className="input-control" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-out">Çıkış</label>
              <input id="ae-out" type="time" className="input-control" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} />
            </div>
          </div>
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-break">Mola (dk)</label>
              <input id="ae-break" type="number" className="input-control" value={breakMinutes} onChange={(e) => setBreakMinutes(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-note">Not</label>
              <input id="ae-note" type="text" className="input-control" value={note ?? ""} onChange={(e) => setNote(e.target.value)} />
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={save.isPending}>
            <i aria-hidden="true" className="fa-solid fa-check" /> Kaydet
          </button>
        </div>
      </div>
    </div>
  );
}
