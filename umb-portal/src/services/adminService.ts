import axios from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "../services/requestManager";

/**
 * Admin Service
 * Backend reference (BankStatementAPI):
 * - Controllers/AdminController.cs
 *   - GET    /api/admin/stats
 *   - GET    /api/admin/users
 *   - GET    /api/admin/users/ad-lookup/{username}
 *   - POST   /api/admin/users
 *   - PUT    /api/admin/users/{id}/toggle
 *   - GET    /api/admin/audit-logs
 *   - GET    /api/admin/audit-logs/export/excel
 *   - GET    /api/admin/audit-logs/export/pdf
 *   - GET    /api/admin/settings
 *   - GET    /api/admin/settings/history
 *   - PUT    /api/admin/settings/{key}
 */

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5300";

const getAuthHeader = (): { Authorization: string } => ({
  Authorization: `Bearer ${localStorage.getItem("authToken") ?? ""}`,
});

type AxiosErrorLike = {
  response?: {
    status?: unknown;
  };
};

const getStatusFromError = (err: unknown): number | null => {
  if (typeof err === "object" && err !== null && "response" in err) {
    const maybe = err as AxiosErrorLike;
    const status = maybe.response?.status;
    if (typeof status === "number") return status;
  }
  return null;
};

const handleAuthError = (status: number) => {
  if (status === 401 || status === 403) {
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
    window.location.href = "/";
  }
};

export interface StaffActivityDTO {
  fullName: string;
  username: string;
  statementCount: number;
  primaryChannel: string;
  totalCharged: number;
}

export interface DashboardStatsDTO {
  totalUsers: number;
  activeUsers: number;
  disabledUsers: number;
  statementsToday: number;
  statementsTodayVisa: number;
  statementsTodayEsb: number;
  chargesToday: number;
  statementsThisMonth: number;
  chargesThisMonth: number;
  mostActiveStaff: StaffActivityDTO[];
}

export interface AdminUserDTO {
  id: number;
  username: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isAdmin: boolean;
  createdAt: string;
  addedBy: string;
  totalStatements: number;
}

export interface AdLookupResultDTO {
  found: boolean;
  username: string;
  fullName: string;
  email: string;
  message?: string;
}

export interface AuditLogDTO {
  id: number;
  staffFullName: string;
  staffUsername: string;
  accountNumber: string;
  accountHolderName: string;
  startDate: string;
  endDate: string;
  channelUsed: string;
  numberOfPages: number;
  amountCharged: number;
  accountCharged: string;
  wasWaived: boolean;
  generatedAt: string;
}

export interface AuditLogFilters {
  startDate?: string;
  endDate?: string;
  staffUsername?: string;
  channel?: string;
  accountNumber?: string;
}

export interface AppSettingDTO {
  id: number;
  key: string;
  value: string;
  description: string;
  dataType: string;
  lastUpdatedAt: string;
  lastUpdatedBy: string;
}

export interface SettingsAuditLogDTO {
  id: number;
  settingKey: string;
  oldValue: string;
  newValue: string;
  changedBy: string;
  changedAt: string;
  reason?: string;
}

export interface UpdateSettingRequest {
  value: string;
  reason?: string;
}

const isNonEmptyString = (v: unknown): v is string =>
  typeof v === "string" && v.trim().length > 0;

const buildQueryParams = (filters: AuditLogFilters): Record<string, string> => {
  const entries = Object.entries(filters) as Array<
    [keyof AuditLogFilters, string | undefined]
  >;
  const out: Record<string, string> = {};
  for (const [k, v] of entries) {
    if (isNonEmptyString(v)) out[k as string] = v;
  }
  return out;
};

export const getStats = async (
  signal?: AbortSignal,
): Promise<DashboardStatsDTO> => {
  const controller = createTrackedAbortController();
  if (signal) signal.addEventListener("abort", () => controller.abort());

  try {
    const resp = await axios.get<DashboardStatsDTO>(
      `${API_BASE_URL}/api/admin/stats`,
      {
        headers: getAuthHeader(),
        signal: controller.signal,
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const status = getStatusFromError(err);
    if (status !== null) handleAuthError(status);
    throw err;
  } finally {
    releaseTrackedAbortController(controller);
  }
};

export const getUsers = async (
  search?: string,
  status?: string,
): Promise<AdminUserDTO[]> => {
  const params: Record<string, string> = {};
  if (isNonEmptyString(search)) params.search = search;
  if (isNonEmptyString(status)) params.status = status;

  try {
    const resp = await axios.get<AdminUserDTO[]>(
      `${API_BASE_URL}/api/admin/users`,
      {
        headers: getAuthHeader(),
        params,
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const adLookup = async (
  username: string,
): Promise<AdLookupResultDTO> => {
  try {
    const resp = await axios.get<AdLookupResultDTO>(
      `${API_BASE_URL}/api/admin/users/ad-lookup/${encodeURIComponent(username)}`,
      { headers: getAuthHeader() },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const addUser = async (
  username: string,
  isAdmin: boolean,
): Promise<AdminUserDTO> => {
  try {
    const resp = await axios.post<AdminUserDTO>(
      `${API_BASE_URL}/api/admin/users`,
      { username, isAdmin },
      {
        headers: {
          ...getAuthHeader(),
          "Content-Type": "application/json",
        },
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const toggleUser = async (id: number): Promise<AdminUserDTO> => {
  try {
    const resp = await axios.put<AdminUserDTO>(
      `${API_BASE_URL}/api/admin/users/${id}/toggle`,
      null,
      { headers: getAuthHeader() },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const getAuditLogs = async (
  filters: AuditLogFilters = {},
): Promise<AuditLogDTO[]> => {
  const params = buildQueryParams(filters);
  try {
    const resp = await axios.get<AuditLogDTO[]>(
      `${API_BASE_URL}/api/admin/audit-logs`,
      {
        headers: getAuthHeader(),
        params,
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

const downloadBlob = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(url);
};

export const exportExcel = async (
  filters: AuditLogFilters = {},
): Promise<void> => {
  const params = buildQueryParams(filters);
  try {
    const resp = await axios.get(
      `${API_BASE_URL}/api/admin/audit-logs/export/excel`,
      {
        headers: getAuthHeader(),
        params,
        responseType: "blob",
      },
    );

    const filename = `AuditLogs_${new Date().toISOString().slice(0, 10)}.xlsx`;
    downloadBlob(resp.data as Blob, filename);
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const exportPdf = async (
  filters: AuditLogFilters = {},
): Promise<void> => {
  const params = buildQueryParams(filters);
  try {
    const resp = await axios.get(
      `${API_BASE_URL}/api/admin/audit-logs/export/pdf`,
      {
        headers: getAuthHeader(),
        params,
        responseType: "blob",
      },
    );

    const filename = `AuditLogs_${new Date().toISOString().slice(0, 10)}.pdf`;
    downloadBlob(resp.data as Blob, filename);
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const getSettings = async (): Promise<AppSettingDTO[]> => {
  try {
    const resp = await axios.get<AppSettingDTO[]>(
      `${API_BASE_URL}/api/admin/settings`,
      {
        headers: getAuthHeader(),
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const getSettingsHistory = async (): Promise<SettingsAuditLogDTO[]> => {
  try {
    const resp = await axios.get<SettingsAuditLogDTO[]>(
      `${API_BASE_URL}/api/admin/settings/history`,
      { headers: getAuthHeader() },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

export const updateSetting = async (
  key: string,
  request: UpdateSettingRequest,
): Promise<AppSettingDTO> => {
  try {
    const resp = await axios.put<AppSettingDTO>(
      `${API_BASE_URL}/api/admin/settings/${encodeURIComponent(key)}`,
      request,
      {
        headers: {
          ...getAuthHeader(),
          "Content-Type": "application/json",
        },
      },
    );
    return resp.data;
  } catch (err: unknown) {
    const statusCode = getStatusFromError(err);
    if (statusCode !== null) handleAuthError(statusCode);
    throw err;
  }
};

const adminService = {
  getStats,
  getUsers,
  adLookup,
  addUser,
  toggleUser,
  getAuditLogs,
  exportExcel,
  exportPdf,
  getSettings,
  getSettingsHistory,
  updateSetting,
};

export default adminService;
