import { expect, test } from "vitest";
import { ApiError } from "./api/client";
import { createQueryClient } from "./queryClient";

const retryOf = (client: ReturnType<typeof createQueryClient>) =>
  client.getDefaultOptions().queries?.retry as (failureCount: number, error: unknown) => boolean;

test("istemci hatalarında (4xx) yeniden denenmez", () => {
  // İstek yanlışsa tekrarı da yanlıştır; kullanıcıyı boşuna bekletmemeli.
  const retry = retryOf(createQueryClient());
  expect(retry(0, new ApiError(404, "Bulunamadı"))).toBe(false);
  expect(retry(0, new ApiError(403, "Yetkisiz"))).toBe(false);
});

test("sunucu hatalarında sınırlı sayıda yeniden denenir", () => {
  const retry = retryOf(createQueryClient());
  expect(retry(0, new ApiError(500, "Sunucu hatası"))).toBe(true);
  expect(retry(2, new ApiError(500, "Sunucu hatası"))).toBe(false);
});

test("ağ hatası gibi tipsiz hatalarda da yeniden denenir", () => {
  const retry = retryOf(createQueryClient());
  expect(retry(0, new TypeError("Failed to fetch"))).toBe(true);
});

test("veriler kısa süre taze sayılır (odak değişiminde refetch fırtınası olmaz)", () => {
  expect(createQueryClient().getDefaultOptions().queries?.staleTime).toBe(30_000);
});
