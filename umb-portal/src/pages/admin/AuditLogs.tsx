import React, { useEffect, useState, useCallback } from "react";
import adminService from "../../services/adminService";
import type { AuditLogDTO, AuditLogFilters } from "../../services/adminService";
import { FileSpreadsheet, FileText } from "lucide-react";
import {
  formatDate,
  formatDateTime,
  formatCurrency,
} from "../../utils/formatters";
import { useToast } from "../../Components/Toast";

const AuditLogs: React.FC = () => {
  const [logs, setLogs] = useState<AuditLogDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<AuditLogFilters>({});
  const { showToast } = useToast();

  const loadLogs = useCallback(
    async (f: AuditLogFilters = {}) => {
      setLoading(true);
      try {
        const data = await adminService.getAuditLogs(f);
        setLogs(data);
      } catch {
        showToast("Unable to load audit logs", "error");
      } finally {
        setLoading(false);
      }
    },
    [showToast],
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadLogs();
    }, 0);
    return () => clearTimeout(timeoutId);
  }, [loadLogs]);

  const applyFilters = () => loadLogs(filters);
  const resetFilters = () => {
    setFilters({});
    loadLogs();
  };

  const exportExcel = async () => {
    try {
      await adminService.exportExcel(filters);
    } catch {
      showToast("Unable to download Excel", "error");
    }
  };

  const exportPdf = async () => {
    try {
      await adminService.exportPdf(filters);
    } catch {
      showToast("Unable to download PDF", "error");
    }
  };

  const totalPrinted = logs.length;
  const visaCharges = logs
    .filter((l) => l.channelUsed === "VISA" && !l.wasWaived)
    .reduce((s, l) => s + (l.amountCharged ?? 0), 0);
  const esbFree = logs.filter((l) => l.channelUsed === "ESB").length;
  const waived = logs.filter((l) => l.wasWaived).length;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-4 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-4 text-center">
          <div className="text-sm text-gray-500">Total Printed</div>
          <div className="text-2xl font-medium">{totalPrinted}</div>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4 text-center">
          <div className="text-sm text-gray-500">VISA Charges</div>
          <div className="text-2xl font-medium">
            {formatCurrency(visaCharges)}
          </div>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4 text-center">
          <div className="text-sm text-gray-500">ESB (Free)</div>
          <div className="text-2xl font-medium">{esbFree}</div>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4 text-center">
          <div className="text-sm text-gray-500">Waived</div>
          <div className="text-2xl font-medium">{waived}</div>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <div className="text-lg font-medium">Audit Logs</div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={exportExcel}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm flex items-center gap-2"
            >
              <FileSpreadsheet /> Excel
            </button>
            <button
              onClick={exportPdf}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm flex items-center gap-2"
            >
              <FileText /> PDF
            </button>
          </div>
        </div>

        <div className="mb-4 grid grid-cols-6 gap-3">
          <input
            aria-label="Start date"
            value={filters.startDate ?? ""}
            onChange={(e) =>
              setFilters((f) => ({ ...f, startDate: e.target.value }))
            }
            type="date"
            className="border border-gray-300 rounded-lg px-3 py-2"
          />
          <input
            aria-label="End date"
            value={filters.endDate ?? ""}
            onChange={(e) =>
              setFilters((f) => ({ ...f, endDate: e.target.value }))
            }
            type="date"
            className="border border-gray-300 rounded-lg px-3 py-2"
          />
          <input
            placeholder="Staff username"
            aria-label="Staff username"
            value={filters.staffUsername ?? ""}
            onChange={(e) =>
              setFilters((f) => ({ ...f, staffUsername: e.target.value }))
            }
            className="border border-gray-300 rounded-lg px-3 py-2"
          />
          <select
            aria-label="Filter by channel"
            value={filters.channel ?? ""}
            onChange={(e) =>
              setFilters((f) => ({ ...f, channel: e.target.value }))
            }
            className="border border-gray-300 rounded-lg px-3 py-2"
          >
            <option value="">All Channels</option>
            <option value="VISA">VISA</option>
            <option value="ESB">ESB</option>
          </select>
          <input
            placeholder="Account number"
            aria-label="Account number"
            value={filters.accountNumber ?? ""}
            onChange={(e) =>
              setFilters((f) => ({ ...f, accountNumber: e.target.value }))
            }
            className="border border-gray-300 rounded-lg px-3 py-2"
          />
          <div className="flex gap-2">
            <button
              onClick={applyFilters}
              className="rounded-lg bg-[#E6A817] px-3 py-2 text-sm text-[#1a1000]"
            >
              Apply Filters
            </button>
            <button
              onClick={resetFilters}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
            >
              Reset
            </button>
          </div>
        </div>

        {loading ? (
          <div className="animate-pulse">Loading...</div>
        ) : logs.length === 0 ? (
          <div className="py-8 text-center text-gray-500">
            No audit logs found
          </div>
        ) : (
          <table className="w-full text-left">
            <thead className="text-xs text-gray-400 uppercase tracking-wider border-b border-gray-100">
              <tr>
                <th className="py-3 px-4">Staff</th>
                <th className="py-3 px-4">Account</th>
                <th className="py-3 px-4">Period</th>
                <th className="py-3 px-4">Channel</th>
                <th className="py-3 px-4">Pages</th>
                <th className="py-3 px-4">Charge</th>
                <th className="py-3 px-4">Date</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l) => (
                <tr
                  key={l.id}
                  className="border-b border-gray-50 hover:bg-gray-50"
                >
                  <td className="py-3 px-4">
                    <div className="font-medium">{l.staffFullName}</div>
                    <div className="text-sm text-gray-500">
                      {l.staffUsername}
                    </div>
                  </td>
                  <td className="py-3 px-4">
                    <div className="font-mono text-sm">{l.accountNumber}</div>
                    <div className="text-sm text-gray-500">
                      {l.accountHolderName}
                    </div>
                  </td>
                  <td className="py-3 px-4">
                    {formatDate(l.startDate)} – {formatDate(l.endDate)}
                  </td>
                  <td className="py-3 px-4">
                    {l.channelUsed === "VISA" ? (
                      <span className="rounded-full bg-amber-100 px-2 py-0.5 text-amber-800 text-xs">
                        VISA
                      </span>
                    ) : (
                      <span className="rounded-full bg-blue-100 px-2 py-0.5 text-blue-800 text-xs">
                        ESB
                      </span>
                    )}
                  </td>
                  <td className="py-3 px-4 text-center">{l.numberOfPages}</td>
                  <td className="py-3 px-4">
                    {l.wasWaived ? (
                      <em className="text-gray-500">Waived</em>
                    ) : l.channelUsed === "ESB" ? (
                      <span className="text-green-700">Free</span>
                    ) : (
                      formatCurrency(l.amountCharged)
                    )}
                  </td>
                  <td className="py-3 px-4 text-sm text-gray-500">
                    {formatDateTime(l.generatedAt)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default AuditLogs;
