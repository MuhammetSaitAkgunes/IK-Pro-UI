import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "../../api/client";
import type { components } from "../../api/schema";

export type OverviewDto = components["schemas"]["OverviewDto"];

export const useOverview = () =>
  useQuery({ queryKey: ["dashboard", "overview"], queryFn: () => apiFetch<OverviewDto>("/dashboard/overview") });
