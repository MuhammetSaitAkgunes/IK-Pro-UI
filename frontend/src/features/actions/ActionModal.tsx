import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { useCreateGlobalAction } from "./queries";

export function ActionModal({ onClose }: { onClose: () => void }) {
  const { showToast } = useToast();
  const createAction = useCreateGlobalAction();
  const [form, setForm] = useState({
    title: "", source: "", owner: "", sourceRoute: "", due: "Bugün", priority: "medium", recommendedAction: "",
  });
  const [error, setError] = useState<string | null>(null);

  const set = (key: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      setForm((f) => ({ ...f, [key]: e.target.value }));

  const submit = async () => {
    setError(null);
    if (!form.title.trim() || !form.source.trim() || !form.owner.trim()) {
      setError("Başlık, kaynak ve sahip zorunludur.");
      return;
    }
    try {
      await createAction.mutateAsync({
        title: form.title.trim(),
        source: form.source.trim(),
        owner: form.owner.trim(),
        sourceRoute: form.sourceRoute.trim() || null,
        due: form.due.trim() || null,
        priority: form.priority,
        recommendedAction: form.recommendedAction.trim() || null,
      });
      showToast("Aksiyon oluşturuldu.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Aksiyon oluşturulamadı.");
    }
  };

  return (
    <div className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>Yeni Aksiyon</h3>
            <p>Operasyon merkezine manuel takip kaydı ekleyin.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="Aksiyon penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>
        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          <div className="form-grid-2">
            <div className="input-group">
              <label className="input-label" htmlFor="act-title">Başlık</label>
              <input id="act-title" className="input-control" value={form.title} onChange={set("title")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-source">Kaynak</label>
              <input id="act-source" className="input-control" value={form.source} onChange={set("source")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-owner">Sahip</label>
              <input id="act-owner" className="input-control" value={form.owner} onChange={set("owner")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-due">Vade etiketi</label>
              <input id="act-due" className="input-control" value={form.due} onChange={set("due")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-priority">Öncelik</label>
              <select id="act-priority" className="input-control" value={form.priority} onChange={set("priority")}>
                <option value="high">Yüksek</option>
                <option value="medium">Orta</option>
                <option value="low">Düşük</option>
              </select>
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-route">Kaynak rota (ops.)</label>
              <input id="act-route" className="input-control" placeholder="payroll, compliance-risk…" value={form.sourceRoute} onChange={set("sourceRoute")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="act-recommended">Önerilen aksiyon</label>
              <textarea id="act-recommended" className="input-control" rows={2} value={form.recommendedAction} onChange={set("recommendedAction")} />
            </div>
          </div>
        </div>
        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={createAction.isPending}>
            <i aria-hidden="true" className="fa-solid fa-check" /> Kaydet
          </button>
        </div>
      </div>
    </div>
  );
}
