import axios from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "./requestManager";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7174";

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
};

type AccountLookupResponse = {
  accountNumber: string;
  accountName: string;
};

const getAuthHeader = () => {
  const token = localStorage.getItem("authToken");
  return token ? { Authorization: `Bearer ${token}` } : {};
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
