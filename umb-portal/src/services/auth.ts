import axios from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "./requestManager";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7174";

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  token: string;
  username: string;
  firstName: string;
  expiresAt: string;
};

export const login = async (payload: LoginRequest): Promise<LoginResponse> => {
  const controller = createTrackedAbortController();

  try {
    const response = await axios.post<LoginResponse>(
      `${API_BASE_URL}/api/auth/login`,
      payload,
      {
        signal: controller.signal,
      },
    );

    return response.data;
  } finally {
    releaseTrackedAbortController(controller);
  }
};
