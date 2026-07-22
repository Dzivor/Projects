import React from "react";
import { Navigate } from "react-router-dom";

interface AdminRouteProps {
  children: React.ReactNode;
}

export function AdminRoute({ children }: AdminRouteProps) {
  const raw = localStorage.getItem("authUser");

  if (!raw) return <Navigate to="/" replace />;

  let user: { isAdmin?: unknown };
  try {
    user = JSON.parse(raw) as { isAdmin?: unknown };
  } catch {
    return <Navigate to="/" replace />;
  }

  const isAdminNormalized = (() => {
    if (user.isAdmin === true) return true;
    if (user.isAdmin === "true") return true;
    if (user.isAdmin === 1) return true;
    return false;
  })();

  if (!isAdminNormalized) return <Navigate to="/welcome" replace />;

  return <>{children}</>;
}

export default AdminRoute;
