import axios from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "../services/requestManager";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5300";

const getAuthHeader = (): { Authorization: string } => {
  const token = localStorage.getItem("authToken");
  return { Authorization: `Bearer ${token ?? ""}` };
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

const handleAuthError = (status: number) => {
  if (status === 401 || status === 403) {
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
    window.location.href = "/";
  }
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
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
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
  if (search) params.search = search;
  if (status) params.status = status;
  try {
    const resp = await axios.get<AdminUserDTO[]>(
      `${API_BASE_URL}/api/admin/users`,
      {
        headers: getAuthHeader(),
        params,
      },
    );
    return resp.data;
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
    throw err;
  }
};

export const adLookup = async (
  username: string,
): Promise<AdLookupResultDTO> => {
  try {
    const resp = await axios.get<AdLookupResultDTO>(
      `${API_BASE_URL}/api/admin/users/ad-lookup/${encodeURIComponent(username)}`,
      {
        headers: getAuthHeader(),
      },
    );
    return resp.data;
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
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
        headers: { ...getAuthHeader(), "Content-Type": "application/json" },
      },
    );
    return resp.data;
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
    throw err;
  }
};

export const toggleUser = async (id: number): Promise<AdminUserDTO> => {
  try {
    const resp = await axios.put<AdminUserDTO>(
      `${API_BASE_URL}/api/admin/users/${id}/toggle`,
      null,
      {
        headers: getAuthHeader(),
      },
    );
    return resp.data;
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
    throw err;
  }
};

export const getAuditLogs = async (
  filters: AuditLogFilters = {},
): Promise<AuditLogDTO[]> => {
  try {
    const resp = await axios.get<AuditLogDTO[]>(
      `${API_BASE_URL}/api/admin/audit-logs`,
      {
        headers: getAuthHeader(),
        params: filters as Record<string, string>,
      },
    );
    return resp.data;
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
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
  try {
    const resp = await axios.get(
      `${API_BASE_URL}/api/admin/audit-logs/export/excel`,
      {
        headers: getAuthHeader(),
        params: filters as Record<string, string>,
        responseType: "blob",
      },
    );
    const filename = `AuditLogs_${new Date().toISOString().slice(0, 10)}.xlsx`;
    downloadBlob(resp.data as Blob, filename);
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
    throw err;
  }
};

export const exportPdf = async (
  filters: AuditLogFilters = {},
): Promise<void> => {
  try {
    const resp = await axios.get(
      `${API_BASE_URL}/api/admin/audit-logs/export/pdf`,
      {
        headers: getAuthHeader(),
        params: filters as Record<string, string>,
        responseType: "blob",
      },
    );
    const filename = `AuditLogs_${new Date().toISOString().slice(0, 10)}.pdf`;
    downloadBlob(resp.data as Blob, filename);
  } catch (err: any) {
    if (err?.response) handleAuthError(err.response.status);
    throw err;
  }
};

export default {
  getStats,
  getUsers,
  adLookup,
  addUser,
  toggleUser,
  getAuditLogs,
  exportExcel,
  exportPdf,
};
