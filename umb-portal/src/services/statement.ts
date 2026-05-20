import axios from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "./requestManager";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5300";

type StatementRequest = {
  accountNumber: string;
  startDate: string;
  endDate: string;
  channel: "VISA" | "ESB";
  waiveCharge: boolean;
  chargeAltAccount: boolean;
  altAccountNumber?: string;
  previewToken?: string;
  staffUsername: string;
};

type StatementPreviewResponse = {
  previewToken: string;
  numberOfPages: number;
  totalCharge: number;
  accountToCharge: string;
  chargeMessage: string;
  accountName: string;
  accountNumber: string;
  accountBalance: number;
  accountToChargeName?: string;
  accountToChargeBalance?: number;
};

type AccountLookupResponse = {
  accountNumber: string;
  accountName: string;
};

const getAuthHeader = () => {
  const token = localStorage.getItem("authToken");
  return token ? { Authorization: `Bearer ${token}` } : {};
};

const readBlobMessage = async (blob: Blob): Promise<string | null> => {
  try {
    const text = await blob.text();
    const parsed = JSON.parse(text) as { message?: unknown };
    return typeof parsed.message === "string" && parsed.message.trim()
      ? parsed.message
      : null;
  } catch {
    return null;
  }
};

export const getBackendErrorMessage = async (
  error: unknown,
  fallbackMessage: string,
): Promise<string> => {
  if (!axios.isAxiosError(error)) {
    return fallbackMessage;
  }

  const data = error.response?.data;

  if (typeof data === "string" && data.trim()) {
    try {
      const parsed = JSON.parse(data) as { message?: unknown };
      if (typeof parsed.message === "string" && parsed.message.trim()) {
        return parsed.message;
      }
    } catch {
      return data;
    }
  }

  if (data instanceof Blob) {
    const blobMessage = await readBlobMessage(data);
    if (blobMessage) {
      return blobMessage;
    }
  }

  if (data && typeof data === "object" && "message" in data) {
    const message = (data as { message?: unknown }).message;
    if (typeof message === "string" && message.trim()) {
      return message;
    }
  }

  return fallbackMessage;
};

export const previewStatement = async (
  payload: StatementRequest,
): Promise<StatementPreviewResponse> => {
  const controller = createTrackedAbortController();

  try {
    const response = await axios.post<StatementPreviewResponse>(
      `${API_BASE_URL}/api/statement/preview`,
      payload,
      {
        headers: getAuthHeader(),
        signal: controller.signal,
      },
    );

    return response.data;
  } finally {
    releaseTrackedAbortController(controller);
  }
};

export const generateStatementPdf = async (
  payload: StatementRequest,
): Promise<Blob> => {
  const controller = createTrackedAbortController();

  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/statement/generate`,
      payload,
      {
        headers: getAuthHeader(),
        responseType: "blob",
        signal: controller.signal,
      },
    );

    return response.data;
  } finally {
    releaseTrackedAbortController(controller);
  }
};

export const lookupAccount = async (
  accountNumber: string,
  channel: "VISA" | "ESB",
): Promise<AccountLookupResponse> => {
  const controller = createTrackedAbortController();

  try {
    const response = await axios.get<AccountLookupResponse>(
      `${API_BASE_URL}/api/account/lookup/${encodeURIComponent(accountNumber)}?channel=${channel}`,
      {
        headers: getAuthHeader(),
        signal: controller.signal,
      },
    );

    return response.data;
  } finally {
    releaseTrackedAbortController(controller);
  }
};

export type {
  StatementRequest,
  StatementPreviewResponse,
  AccountLookupResponse,
};
