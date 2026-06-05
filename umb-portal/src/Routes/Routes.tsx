import { Route, Routes } from "react-router-dom";
import Welcome from "../Components/Welcome";
import Login from "../Components/Login";
import Statement from "../Components/Statement";
import ESBStatement from "../Components/ESB-Statement";
import { AdminRoute } from "../pages/admin/AdminRoute";
import { AdminLayout } from "../layouts/AdminLayout";
import AdminOverview from "../pages/admin/AdminOverview";
import UserManagement from "../pages/admin/UserManagement";
import AuditLogs from "../pages/admin/AuditLogs";
import Settings from "../pages/admin/Settings";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/Statement" element={<Statement />} />
      <Route path="/ESB-Statement" element={<ESBStatement />} />

      <Route
        path="/admin"
        element={
          <AdminRoute>
            <AdminLayout>
              <AdminOverview />
            </AdminLayout>
          </AdminRoute>
        }
      />

      <Route
        path="/admin/users"
        element={
          <AdminRoute>
            <AdminLayout>
              <UserManagement />
            </AdminLayout>
          </AdminRoute>
        }
      />

      <Route
        path="/admin/audit-logs"
        element={
          <AdminRoute>
            <AdminLayout>
              <AuditLogs />
            </AdminLayout>
          </AdminRoute>
        }
      />

      <Route
        path="/admin/settings"
        element={
          <AdminRoute>
            <AdminLayout>
              <Settings />
            </AdminLayout>
          </AdminRoute>
        }
      />
    </Routes>
  );
};

export default AppRoutes;
