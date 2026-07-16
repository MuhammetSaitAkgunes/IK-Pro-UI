import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { useCreateCandidate } from "./queries";

export function CandidateModal({ onClose, onCreated }: {
  onClose: () => void; onCreated: (id: number) => void;
}) {
  const { showToast } = useToast();
  const createCandidate = useCreateCandidate();
  const [form, setForm] = useState({
    name: "", appliedRole: "", score: "70", location: "", experienceYears: "0", summary: "", skills: "",
  });
  const [error, setError] = useState<string | null>(null);

  const set = (key: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm((f) => ({ ...f, [key]: e.target.value }));

  const submit = async () => {
    setError(null);
    if (!form.name.trim() || !form.appliedRole.trim()) {
      setError("Ad Soyad ve pozisyon zorunludur.");
      return;
    }
    try {
      const candidate = await createCandidate.mutateAsync({
        name: form.name.trim(),
        appliedRole: form.appliedRole.trim(),
        score: Math.max(0, Math.min(100, Math.round(Number(form.score) || 0))),
        location: form.location.trim() || null,
        experienceYears: Math.max(0, Math.round(Number(form.experienceYears) || 0)),
        summary: form.summary.trim() || null,
        skills: form.skills.split(",").map((skill) => skill.trim()).filter(Boolean),
      });
      showToast(`${candidate.name} aday havuzuna eklendi.`, "success");
      if (candidate.id !== undefined) onCreated(candidate.id);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Aday eklenemedi.");
    }
  };

  return (
    <div className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>Yeni Aday</h3>
            <p>Aday havuzuna manuel kayıt ekleyin.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="Aday penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>
        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          <div className="form-grid-2">
            <div className="input-group">
              <label className="input-label" htmlFor="cand-name">Ad Soyad</label>
              <input id="cand-name" className="input-control" value={form.name} onChange={set("name")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-role">Başvurulan pozisyon</label>
              <input id="cand-role" className="input-control" value={form.appliedRole} onChange={set("appliedRole")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-score">AI puanı (0-100)</label>
              <input id="cand-score" type="number" className="input-control" value={form.score} onChange={set("score")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-exp">Deneyim (yıl)</label>
              <input id="cand-exp" type="number" className="input-control" value={form.experienceYears} onChange={set("experienceYears")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-location">Lokasyon</label>
              <input id="cand-location" className="input-control" value={form.location} onChange={set("location")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-skills">Yetenekler (virgülle)</label>
              <input id="cand-skills" className="input-control" value={form.skills} onChange={set("skills")} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="cand-summary">Başvuru özeti</label>
              <textarea id="cand-summary" className="input-control" rows={3} value={form.summary} onChange={set("summary")} />
            </div>
          </div>
        </div>
        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={createCandidate.isPending}>
            <i aria-hidden="true" className="fa-solid fa-check" /> Kaydet
          </button>
        </div>
      </div>
    </div>
  );
}
