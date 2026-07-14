export const formatLeaveDate = (value?: string | null): string =>
  value
    ? new Date(value).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" })
    : "";

export const LEAVE_STATUS_TEXT: Record<string, string> = {
  pending: "Bekliyor",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  cancelled: "İptal Edildi",
};

const dayDiff = (a: Date, b: Date): number =>
  Math.round((b.setHours(0, 0, 0, 0) - a.setHours(0, 0, 0, 0)) / 86_400_000);

export const awayDateLabel = (start?: string | null, end?: string | null): string => {
  if (!start) return "";
  const startDiff = dayDiff(new Date(), new Date(start));
  const endDiff = end ? dayDiff(new Date(), new Date(end)) : startDiff;
  if (startDiff <= 0 && endDiff >= 0) return "Bugün";
  if (startDiff === 1) return "Yarın";
  return formatLeaveDate(start);
};
