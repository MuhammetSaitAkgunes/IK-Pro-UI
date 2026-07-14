export const minutesToHhMm = (minutes?: number): string => {
  const total = minutes ?? 0;
  const h = Math.floor(total / 60);
  const m = total % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
};

export const formatTime = (value?: string | null): string => (value ? value.slice(0, 5) : "--:--");

export const formatBreak = (minutes?: number): string => {
  if (!minutes) return "-";
  return minutes % 60 === 0 ? `${minutes / 60}s` : `${minutes} dk`;
};

export const formatTsDate = (value?: string): string => {
  if (!value) return "";
  const date = new Date(value);
  const dayMonth = date.toLocaleDateString("tr-TR", { day: "2-digit", month: "short" });
  const weekday = date.toLocaleDateString("tr-TR", { weekday: "short" });
  return `${dayMonth} ${weekday}`;
};

export const monthLabel = (year: number, month: number): string =>
  new Date(year, month - 1, 1).toLocaleDateString("tr-TR", { month: "long", year: "numeric" });

export const LIVE_STATUS_TEXT: Record<string, string> = {
  ontime: "Zamanında",
  late: "Geç kaldı",
  absent: "Gelmedi",
  early: "Erken giriş",
};
