import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { usePreviewImport, useImportEmployees, type ImportPreviewDto } from "./queries";

/**
 * Excel'den toplu personel aktarma. Akış: dosya seç → önizleme (kaydetmez) →
 * rapor → onay → aktar. Önizleme ve aktarım sunucuda aynı doğrulamayı
 * kullandığı için raporda görülen sonuç aktarımda değişmez.
 */
export function ImportModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { showToast } = useToast();
  const preview = usePreviewImport();
  const importer = useImportEmployees();
  const [file, setFile] = useState<File | null>(null);
  const [rapor, setRapor] = useState<ImportPreviewDto | null>(null);

  if (!open) return null;

  const dosyaSecildi = async (secilen: File | null) => {
    setFile(secilen);
    setRapor(null);
    if (!secilen) return;
    try {
      setRapor(await preview.mutateAsync(secilen));
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Dosya okunamadı.", "error");
    }
  };

  const aktar = async () => {
    if (!file) return;
    try {
      const sonuc = await importer.mutateAsync(file);
      showToast(
        `${sonuc.olusturulanSatir} personel aktarıldı, ${sonuc.atlananSatir} satır atlandı.`,
        "success",
      );
      onClose();
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Aktarım başarısız.", "error");
    }
  };

  const aktarilabilir = rapor !== null && rapor.gecerliSatir > 0 && !importer.isPending;

  return (
    <div
      className="fullscreen-modal"
      style={{ display: "flex" }}
      role="dialog"
      aria-label="Excel'den personel içe aktar"
    >
      <section className="card">
        <div className="card-header-clean">
          <div>
            <h4>Excel'den Personel İçe Aktar</h4>
            <p className="text-muted">
              Önce <a href="/api/employees/import/template">şablonu indirin</a>, doldurun ve yükleyin.
              Yükleme yalnızca doğrulama yapar; kayıt için onayınız gerekir.
            </p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} aria-label="Kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="input-group">
          <label className="input-label" htmlFor="import-file">Excel dosyası (.xlsx)</label>
          <input
            id="import-file"
            className="input-control"
            type="file"
            accept=".xlsx"
            onChange={(e) => dosyaSecildi(e.target.files?.[0] ?? null)}
          />
        </div>

        {preview.isPending && <p className="pending-desc">Dosya doğrulanıyor…</p>}

        {rapor && (
          <>
            <div className="kpi-grid">
              <div className="kpi-card">
                <div className="kpi-content">
                  <span className="kpi-label">Toplam</span>
                  <h3 className="kpi-value">{rapor.toplamSatir}</h3>
                </div>
              </div>
              <div className="kpi-card">
                <div className="kpi-content">
                  <span className="kpi-label">Geçerli</span>
                  <h3 className="kpi-value">{rapor.gecerliSatir}</h3>
                </div>
              </div>
              <div className="kpi-card">
                <div className="kpi-content">
                  <span className="kpi-label">Hatalı</span>
                  <h3 className="kpi-value">{rapor.hataliSatir}</h3>
                </div>
              </div>
              <div className="kpi-card">
                <div className="kpi-content">
                  <span className="kpi-label">Mükerrer</span>
                  <h3 className="kpi-value">{rapor.mukerrerSatir}</h3>
                </div>
              </div>
            </div>

            {rapor.mukerrerSatir > 0 && (
              <p className="pending-desc">
                Mükerrer satırlar TC Kimlik No ile tespit edildi ve atlanacak; mevcut kayıtlar
                değiştirilmez. TC'si boş satırlar bu kontrolden geçemez.
              </p>
            )}

            {rapor.bilinmeyenDepartmanlar.length > 0 && (
              <section className="surface">
                <strong>Sistemde olmayan departmanlar</strong>
                <ul>
                  {rapor.bilinmeyenDepartmanlar.map((departman) => (
                    <li key={departman}>{departman}</li>
                  ))}
                </ul>
                <small>Bu departmanları önce oluşturun, sonra dosyayı tekrar yükleyin.</small>
              </section>
            )}

            {rapor.sorunlar.length > 0 && (
              <table className="data-table">
                <thead>
                  <tr><th>Satır</th><th>Alan</th><th>Sorun</th></tr>
                </thead>
                <tbody>
                  {rapor.sorunlar.map((sorun, index) => (
                    <tr key={`${sorun.satirNo}-${sorun.alan}-${index}`}>
                      <td>{sorun.satirNo}</td>
                      <td>{sorun.alan}</td>
                      <td>{sorun.mesaj}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        <div className="toolbar-actions">
          <button className="btn btn-secondary" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={aktar} disabled={!aktarilabilir}>
            Aktar
          </button>
        </div>
      </section>
    </div>
  );
}
