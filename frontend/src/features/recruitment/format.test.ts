import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { formatTimeAgo, scoreClass, statusTagClass } from "./format";

beforeEach(() => {
  vi.useFakeTimers();
  vi.setSystemTime(new Date("2026-07-16T12:00:00Z"));
});
afterEach(() => vi.useRealTimers());

test("formatTimeAgo saat ve gün etiketleri", () => {
  expect(formatTimeAgo("2026-07-16T11:30:00Z")).toBe("Az önce");
  expect(formatTimeAgo("2026-07-16T10:00:00Z")).toBe("2s önce");
  expect(formatTimeAgo("2026-07-15T09:00:00Z")).toBe("1g önce");
  expect(formatTimeAgo(null)).toBe("-");
  expect(formatTimeAgo("bozuk")).toBe("-");
});

test("statusTagClass eski CSS sınıflarına eşler", () => {
  expect(statusTagClass("Yeni")).toBe("yeni");
  expect(statusTagClass("Mülakat")).toBe("mülakat");
  expect(statusTagClass("Teklif")).toBe("teklif");
  expect(statusTagClass("Red")).toBe("red");
  expect(statusTagClass("İşe Alındı")).toBe("hired");
});

test("scoreClass 80 eşiği", () => {
  expect(scoreClass(92)).toBe("high");
  expect(scoreClass(80)).toBe("mid");
  expect(scoreClass(undefined)).toBe("mid");
});
