import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "../../api/client";
import type { components } from "../../api/schema";

export type DashboardMetricsDto = components["schemas"]["DashboardMetricsDto"];
export type ManagerLoadDto = components["schemas"]["ManagerLoadDto"];
export type EmployeeVoiceDto = components["schemas"]["EmployeeVoiceDto"];
export type ComplianceRiskDto = components["schemas"]["ComplianceRiskDto"];
export type RiskDetailDto = components["schemas"]["RiskDetailDto"];
export type GlobalActionDto = components["schemas"]["GlobalActionDto"];
export type RiskEmployeeDto = components["schemas"]["RiskEmployeeDto"];

export const useDashboardMetrics = () =>
  useQuery({ queryKey: ["dashboard", "metrics"], queryFn: () => apiFetch<DashboardMetricsDto>("/dashboard/metrics") });

export const useManagerLoad = () =>
  useQuery({ queryKey: ["dashboard", "manager-load"], queryFn: () => apiFetch<ManagerLoadDto>("/dashboard/manager-load") });

export const useEmployeeVoice = () =>
  useQuery({ queryKey: ["dashboard", "employee-voice"], queryFn: () => apiFetch<EmployeeVoiceDto>("/dashboard/employee-voice") });

export const useComplianceRisk = () =>
  useQuery({ queryKey: ["dashboard", "compliance"], queryFn: () => apiFetch<ComplianceRiskDto>("/dashboard/compliance") });

export const useAttritionDetail = () =>
  useQuery({ queryKey: ["dashboard", "attrition"], queryFn: () => apiFetch<RiskDetailDto>("/dashboard/attrition") });

export const useBurnoutDetail = () =>
  useQuery({ queryKey: ["dashboard", "burnout"], queryFn: () => apiFetch<RiskDetailDto>("/dashboard/burnout") });

export const useOpenActions = () =>
  useQuery({
    queryKey: ["actions", "list"],
    queryFn: () => apiFetch<GlobalActionDto[]>("/actions"),
    select: (all) => all.filter((a) => a.status !== "done"),
  });
