import axios, { AxiosError } from "axios";
import {
  createTrackedAbortController,
  releaseTrackedAbortController,
} from "./requestManager";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  success: boolean;
  token?: string;
  username?: string;
  fullName?: string;
  userId?: number;
  expiresAt?: string;
  message?: string;
};

export const login = async (payload: LoginRequest): Promise<LoginResponse> => {
  const controller = createTrackedAbortController();

  try {
    const response = await axios.post<LoginResponse>(
      `${API_BASE_URL}/api/auth/login`,
      payload,
      { signal: controller.signal },
    );

    return response.data;
  } catch (error) {
    // Extract the message from the backend response if available
    if (error instanceof AxiosError && error.response?.data) {
      return error.response.data as LoginResponse;
    }

    // Only use a fallback if the backend sent nothing at all
    return {
      success: false,
      message: "Unable to reach the server. Please try again.",
    };
  } finally {
    releaseTrackedAbortController(controller);
  }
};
