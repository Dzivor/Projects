import React from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import {
  FileText,
  LayoutDashboard,
  Users,
  ClipboardList,
  LogOut,
} from "lucide-react";

interface Props {
  children: React.ReactNode;
}

export const AdminLayout: React.FC<Props> = ({ children }) => {
  const navigate = useNavigate();
  const loc = useLocation();
  const raw = localStorage.getItem("authUser");
  const user = raw
    ? (JSON.parse(raw) as { username?: string; fullName?: string })
    : null;

  const logout = () => {
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
    navigate("/");
  };

  const goToStatements = () => navigate("/welcome");

  const navItem = (to: string, label: string, icon: React.ReactNode) => {
    const active = loc.pathname === to || loc.pathname.startsWith(to + "/");
    return (
      <NavLink
        to={to}
        className={`flex items-center gap-3 px-4 py-2 rounded-lg ${active ? "border-l-2 border-amber-400 bg-amber-50 text-amber-600" : "text-gray-600 hover:bg-gray-50"}`}
      >
        {icon}
        <span className="text-sm font-medium">{label}</span>
      </NavLink>
    );
  };

  return (
    <div className="min-h-screen flex bg-gray-50">
      <aside className="w-52 border-r border-gray-100 bg-white px-3 py-6">
        <div className="mb-8 px-2">
          <div className="text-2xl font-medium text-[#E6A817]">
            umb{" "}
            <span className="ml-2 text-xs text-gray-500 bg-gray-100 rounded-full px-2 py-0.5 align-middle">
              admin
            </span>
          </div>
        </div>
        <nav className="space-y-1">
          {navItem("/admin", "Overview", <LayoutDashboard size={16} />)}
          {navItem("/admin/users", "User Management", <Users size={16} />)}
          {navItem(
            "/admin/audit-logs",
            "Audit Logs",
            <ClipboardList size={16} />,
          )}
          {navItem("/admin/settings", "Settings", <FileText size={16} />)}
        </nav>
      </aside>

      <div className="flex-1 flex flex-col">
        <header className="flex items-center justify-between border-b border-gray-100 bg-white px-6 py-4">
          <div />
          <div className="flex items-center gap-3">
            <button
              onClick={goToStatements}
              className="flex items-center gap-2 rounded-lg bg-[#E6A817] px-3 py-2 text-sm font-medium text-[#1a1000] hover:brightness-95"
            >
              <FileText size={16} />
              Go to Statements
            </button>
            <div className="flex items-center gap-2 text-gray-600">
              <div className="flex items-center gap-2">
                <svg
                  className="h-6 w-6 rounded-full bg-gray-100 p-1 text-gray-500"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                >
                  <circle cx="12" cy="8" r="3"></circle>
                  <path d="M6 20c0-3.3 2.7-6 6-6s6 2.7 6 6"></path>
                </svg>
                <div className="text-sm">{user?.username ?? ""}</div>
              </div>
            </div>
            <button
              onClick={logout}
              className="flex items-center gap-2 rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-600 hover:bg-gray-50"
            >
              <LogOut size={14} />
              Logout
            </button>
          </div>
        </header>

        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  );
};

export default AdminLayout;
