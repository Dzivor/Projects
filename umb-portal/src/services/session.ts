import type { NavigateFunction } from "react-router";
import { cancelAllTrackedRequests } from "./requestManager";

export const logoutUser = (navigate: NavigateFunction): void => {
  cancelAllTrackedRequests();
  localStorage.removeItem("authToken");
  localStorage.removeItem("authUser");
  navigate("/", { replace: true });
};
