import { ApiError } from "../../api/client";

export function PageLoading() {
  return (
    <section className="surface empty-state" role="status">
      <i aria-hidden="true" className="fa-solid fa-circle-notch fa-spin" />
      <h2>Yükleniyor</h2>
      <p>Veriler yükleniyor, lütfen bekleyin.</p>
    </section>
  );
}

export function PageError({ error }: { error: unknown }) {
  const message = error instanceof ApiError ? error.message : "Beklenmeyen bir hata oluştu.";
  return (
    <section className="surface empty-state" role="alert">
      <i aria-hidden="true" className="fa-solid fa-triangle-exclamation" />
      <h2>Veri yüklenemedi</h2>
      <p>{message}</p>
    </section>
  );
}
