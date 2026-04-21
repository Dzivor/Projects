import axios from "axios";

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
  staffUsername: string;
};

type StatementPreviewResponse = {
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
  const response = await axios.post<StatementPreviewResponse>(
    `${API_BASE_URL}/api/statement/preview`,
    payload,
    { headers: getAuthHeader() },
  );

  return response.data;
};

export const generateStatementPdf = async (
  payload: StatementRequest,
): Promise<Blob> => {
  const response = await axios.post(
    `${API_BASE_URL}/api/statement/generate`,
    payload,
    {
      headers: getAuthHeader(),
      responseType: "blob",
    },
  );

  return response.data;
};

export const lookupAccount = async (
  accountNumber: string,
): Promise<AccountLookupResponse> => {
  const response = await axios.get<AccountLookupResponse>(
    `${API_BASE_URL}/api/account/lookup/${encodeURIComponent(accountNumber)}`,
    {
      headers: getAuthHeader(),
    },
  );

  return response.data;
};

export type {
  StatementRequest,
  StatementPreviewResponse,
  AccountLookupResponse,
};
