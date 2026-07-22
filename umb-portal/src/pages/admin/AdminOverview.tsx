import React, { useEffect, useState } from "react";
import adminService from "../../services/adminService";
import type { DashboardStatsDTO } from "../../services/adminService";
import { Users, FileText, Coins, Calendar, TrendingUp } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { getInitials, formatCurrency } from "../../utils/formatters";

const StatCard: React.FC<{
  title: string;
  value: React.ReactNode;
  subtitle?: string;
  onClick?: () => void;
  icon?: React.ReactNode;
}> = ({ title, value, subtitle, onClick, icon }) => (
  <div
    onClick={onClick}
    className="cursor-pointer rounded-xl border border-gray-200 bg-white p-5 hover:border-amber-300 transition"
  >
    <div className="flex items-center justify-between">
      <div className="text-xs text-gray-500 uppercase tracking-wider flex items-center gap-2">
        {icon}
        <span>{title}</span>
      </div>
    </div>
    <div className="mt-3 text-2xl font-medium text-gray-900">{value}</div>
    {subtitle && <div className="mt-1 text-sm text-gray-400">{subtitle}</div>}
  </div>
);

const AdminOverview: React.FC = () => {
  const [stats, setStats] = useState<DashboardStatsDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    adminService
      .getStats()
      .then((s) => {
        if (mounted) setStats(s);
      })
      .catch(() => {})
      .finally(() => {
        if (mounted) setLoading(false);
      });
    return () => {
      mounted = false;
    };
  }, []);

  if (loading) return <div className="animate-pulse">Loading stats...</div>;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-3 gap-4">
        <StatCard
          title="Total Users"
          icon={<Users size={16} />}
          value={stats?.totalUsers ?? 0}
          subtitle={`${stats?.activeUsers ?? 0} active · ${stats?.disabledUsers ?? 0} disabled`}
          onClick={() => navigate("/admin/users")}
        />
        <StatCard
          title="Statements Today"
          icon={<FileText size={16} />}
          value={stats?.statementsToday ?? 0}
          subtitle={`${stats?.statementsTodayVisa ?? 0} VISA · ${stats?.statementsTodayEsb ?? 0} ESB`}
          onClick={() => navigate("/admin/audit-logs")}
        />
        <StatCard
          title="Charges Today"
          icon={<Coins size={16} />}
          value={stats ? `GHS ${stats.chargesToday.toFixed(2)}` : "GHS 0.00"}
          subtitle="from VISA statements"
          onClick={() => navigate("/admin/audit-logs")}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <StatCard
          title="Statements This Month"
          icon={<Calendar size={16} />}
          value={stats?.statementsThisMonth ?? 0}
        />
        <StatCard
          title="Charges This Month"
          icon={<TrendingUp size={16} />}
          value={
            stats ? `GHS ${stats.chargesThisMonth.toFixed(2)}` : "GHS 0.00"
          }
        />
      </div>

      <div className="bg-white rounded-xl border border-gray-200 p-5">
        <div className="flex items-center justify-between mb-4">
          <div>
            <div className="text-lg font-medium">Most Active Staff</div>
            <div className="text-sm text-gray-400">this month</div>
          </div>
        </div>

        <table className="w-full text-left">
          <thead className="text-xs text-gray-400 uppercase tracking-wider border-b border-gray-100">
            <tr>
              <th className="py-3 px-4">Staff</th>
              <th className="py-3 px-4">Statements</th>
              <th className="py-3 px-4">Channel</th>
              <th className="py-3 px-4">Total Charged</th>
            </tr>
          </thead>
          <tbody>
            {stats?.mostActiveStaff?.map((s) => (
              <tr
                key={s.username}
                className="border-b border-gray-50 hover:bg-gray-50"
              >
                <td className="py-3 px-4">
                  <div className="flex items-center gap-3">
                    <div className="h-8 w-8 flex items-center justify-center rounded-full bg-amber-100 text-amber-800 text-sm">
                      {getInitials(s.fullName)}
                    </div>
                    <div>
                      <div className="font-medium">{s.fullName}</div>
                      <div className="text-sm text-gray-500">{s.username}</div>
                    </div>
                  </div>
                </td>
                <td className="py-3 px-4">{s.statementCount}</td>
                <td className="py-3 px-4">
                  {s.primaryChannel === "VISA" ? (
                    <span className="rounded-full bg-amber-100 px-2 py-0.5 text-amber-800 text-xs">
                      VISA
                    </span>
                  ) : (
                    <span className="rounded-full bg-blue-100 px-2 py-0.5 text-blue-800 text-xs">
                      ESB
                    </span>
                  )}
                </td>
                <td className="py-3 px-4">
                  {s.primaryChannel === "ESB"
                    ? "Free"
                    : formatCurrency(s.totalCharged)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminOverview;
