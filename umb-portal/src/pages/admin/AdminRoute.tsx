import React from "react";
import { Navigate } from "react-router-dom";

interface AdminRouteProps {
  children: React.ReactNode;
}

export function AdminRoute({ children }: AdminRouteProps) {
  const raw = localStorage.getItem("authUser");

  if (!raw) return <Navigate to="/" replace />;

  let user: { isAdmin?: boolean };
  try {
    user = JSON.parse(raw) as { isAdmin?: boolean };
  } catch {
    return <Navigate to="/" replace />;
  }

  if (user.isAdmin !== true) return <Navigate to="/welcome" replace />;

  return <>{children}</>;
}

export default AdminRoute;
