import { render, screen } from "@testing-library/react";
import { expect, test } from "vitest";
import { ApiError } from "../../api/client";
import { PageError, PageLoading } from "./PageState";

test("yükleme durumu render edilir", () => {
  render(<PageLoading />);
  expect(screen.getByText("Yükleniyor")).toBeInTheDocument();
});

test("ApiError mesajı gösterilir", () => {
  render(<PageError error={new ApiError(409, "Çakışan kayıt.")} />);
  expect(screen.getByRole("alert")).toHaveTextContent("Çakışan kayıt.");
});
