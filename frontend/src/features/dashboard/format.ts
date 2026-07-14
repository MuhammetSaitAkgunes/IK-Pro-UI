export const getRiskLevel = (score: number): "high" | "medium" | "low" =>
  score >= 70 ? "high" : score >= 55 ? "medium" : "low";

export const getRiskLabel = (score: number): string =>
  score >= 70 ? "Yüksek risk" : score >= 55 ? "Orta risk" : "Kontrollü";

const LEVEL_TEXT: Record<string, string> = { high: "Yüksek", medium: "Orta", low: "Düşük" };
export const getLevelText = (level?: string | null): string => LEVEL_TEXT[level ?? ""] ?? "İzlemede";

const PRIORITY_LABEL: Record<string, string> = {
  high: "Bugün müdahale et",
  medium: "Bu hafta takip et",
  low: "İzlemede kalsın",
};
export const priorityLabel = (priority?: string | null): string =>
  PRIORITY_LABEL[priority ?? ""] ?? "İzlemede kalsın";

const PRIORITY_ORDER: Record<string, number> = { high: 0, medium: 1, low: 2 };
export const topPriorityActions = <T extends { priority?: string | null }>(
  items: T[],
  count = 3,
): T[] =>
  [...items]
    .sort((a, b) => (PRIORITY_ORDER[a.priority ?? ""] ?? 3) - (PRIORITY_ORDER[b.priority ?? ""] ?? 3))
    .slice(0, count);

export const formatToday = (): string =>
  new Date().toLocaleDateString("tr-TR", { weekday: "long", year: "numeric", month: "long", day: "numeric" });
