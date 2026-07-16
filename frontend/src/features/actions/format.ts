export const actionLevelText = (level?: string | null): string =>
  ({ high: "Yüksek", medium: "Orta", low: "Düşük" } as Record<string, string>)[level ?? ""] ?? "Normal";

export const actionStatusText = (status?: string | null): string =>
  ({ open: "Açık", week: "Bu Hafta", done: "Tamamlandı" } as Record<string, string>)[status ?? ""] ?? "Açık";

export const actionPillClass = (priority?: string | null): string =>
  priority === "high" ? "rejected" : priority === "medium" ? "pending" : "approved";

export const nextActionStatus = (status?: string | null): string | null =>
  status === "open" ? "week" : status === "week" ? "done" : null;

export const nextActionStatusLabel = (status?: string | null): string | null =>
  status === "open" ? "Bu haftaya al" : status === "week" ? "Tamamlandı işaretle" : null;
