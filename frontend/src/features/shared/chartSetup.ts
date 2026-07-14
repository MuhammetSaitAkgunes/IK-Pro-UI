import { Chart, registerables } from "chart.js";

Chart.register(...registerables);

/** Grafik renkleri CSS token'larından okunur (eski chartToken paritesi). */
export const chartToken = (name: string, fallback: string): string =>
  getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
