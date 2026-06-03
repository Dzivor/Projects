import React from "react";
import { Navigate } from "react-router-dom";

interface AdminRouteProps {
  children: React.ReactNode;
}

export function AdminRoute({ children }: AdminRouteProps) {
  const raw = localStorage.getItem("authUser");
  if (!raw) return <Navigate to="/" replace />;

  try {
    const user = JSON.parse(raw) as { isAdmin?: boolean };
    if (!user.isAdmin) return <Navigate to="/welcome" replace />;
    return <>{children}</>;
  } catch {
    return <Navigate to="/" replace />;
  }
}

export default AdminRoute;
