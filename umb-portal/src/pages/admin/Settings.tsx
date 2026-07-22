import React, { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  AlertTriangle,
  CreditCard,
  FileText,
  Pencil,
  Shield,
  Settings as SettingsIcon,
  ClipboardList,
  Loader2,
  ArrowDown,
  X,
} from "lucide-react";
import { useToast } from "../../Components/Toast";
import {
  type AppSettingDTO,
  type SettingsAuditLogDTO,
  type UpdateSettingRequest,
  getSettings,
  getSettingsHistory,
  updateSetting,
} from "../../services/adminService";

import { formatDate, formatDateTime } from "../../utils/formatters";

type ValidationError = string | null;

const getSettingGroup = (key: string): string => {
  if (key === "VisaChargePerPage" || key === "ChargeCollectionAccount") {
    return "Charging Settings";
  }
  if (key === "StatementMaxDateRangeDays") {
    return "Statement Settings";
  }
  if (key === "SessionTimeoutMinutes") {
    return "Security Settings";
  }
  return "Other Settings";
};

const groupIcon = (groupName: string) => {
  switch (groupName) {
    case "Charging Settings":
      return CreditCard;
    case "Statement Settings":
      return FileText;
    case "Security Settings":
      return Shield;
    default:
      return SettingsIcon;
  }
};

const formatSettingValue = (setting: AppSettingDTO): string => {
  switch (setting.key) {
    case "VisaChargePerPage":
      return `GHS ${parseFloat(setting.value).toFixed(2)}`;
    case "SessionTimeoutMinutes":
      return `${setting.value} minutes`;
    case "StatementMaxDateRangeDays":
      return `${setting.value} days`;
    case "ChargeCollectionAccount":
      return setting.value.length > 20
        ? `${setting.value.substring(0, 20)}...`
        : setting.value;
    default:
      return setting.value;
  }
};

const validateNewValue = (
  setting: AppSettingDTO,
  value: string,
): ValidationError => {
  const trimmed = value.trim();
  if (!trimmed) return "Value cannot be empty";

  if (setting.dataType === "decimal") {
    const n = parseFloat(trimmed);
    if (Number.isNaN(n) || n <= 0) return "Must be a positive number";
  }

  if (setting.dataType === "int") {
    const n = parseInt(trimmed, 10);
    if (Number.isNaN(n) || n <= 0) return "Must be a positive whole number";
  }

  return null;
};

const placeholderForKey = (key: string): string => {
  switch (key) {
    case "VisaChargePerPage":
      return "e.g. 12.00";
    case "ChargeCollectionAccount":
      return "Enter account number";
    case "SessionTimeoutMinutes":
      return "e.g. 30";
    case "StatementMaxDateRangeDays":
      return "e.g. 365";
    default:
      return "Enter value";
  }
};

const impactMessage = (key: string): string => {
  switch (key) {
    case "VisaChargePerPage":
      return "This applies to all new VISA charges immediately. Active transactions are not affected.";
    case "ChargeCollectionAccount":
      return "All new VISA charges will be directed to this account immediately.";
    case "SessionTimeoutMinutes":
      return "New login sessions will use this timeout value.";
    case "StatementMaxDateRangeDays":
      return "Applies to all new statement requests.";
    default:
      return "This change takes effect immediately.";
  }
};

const Settings: React.FC = () => {
  const { showToast } = useToast();

  const [settings, setSettings] = useState<AppSettingDTO[]>([]);
  const [history, setHistory] = useState<SettingsAuditLogDTO[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [editingSetting, setEditingSetting] = useState<AppSettingDTO | null>(
    null,
  );
  const [newValue, setNewValue] = useState<string>("");
  const [reason, setReason] = useState<string>("");
  const [validationError, setValidationError] = useState<ValidationError>(null);

  const [showConfirmModal, setShowConfirmModal] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const [showAllHistory, setShowAllHistory] = useState<boolean>(false);

  const fetchData = async () => {
    setError(null);
    setLoading(true);
    try {
      const [s, h] = await Promise.all([getSettings(), getSettingsHistory()]);
      setSettings(s);
      setHistory(h);
    } catch {
      setError("Failed to load settings. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    (async () => {
      await fetchData();
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const settingGroups = useMemo(() => {
    const groups: Record<string, AppSettingDTO[]> = {};
    for (const setting of settings) {
      const group = getSettingGroup(setting.key);
      if (!groups[group]) groups[group] = [];
      groups[group].push(setting);
    }

    const order = [
      "Charging Settings",
      "Statement Settings",
      "Security Settings",
      "Other Settings",
    ];

    return order
      .filter((g) => groups[g] && groups[g].length > 0)
      .map((g) => ({ groupName: g, settings: groups[g] }));
  }, [settings]);

  const openEditModal = (setting: AppSettingDTO) => {
    setEditingSetting(setting);
    setNewValue(setting.value);
    setReason("");
    setValidationError(null);
    setSubmitError(null);
    setShowConfirmModal(false);
  };

  const closeEditModal = () => {
    setEditingSetting(null);
    setNewValue("");
    setReason("");
    setValidationError(null);
    setShowConfirmModal(false);
    setSubmitError(null);
  };

  const handlePreviewChange = () => {
    if (!editingSetting) return;
    const err = validateNewValue(editingSetting, newValue);
    if (err) {
      setValidationError(err);
      return;
    }
    setValidationError(null);
    setShowConfirmModal(true);
  };

  const handleConfirmUpdate = async () => {
    if (!editingSetting) return;

    setIsSubmitting(true);
    setSubmitError(null);
    try {
      const request: UpdateSettingRequest = {
        value: newValue.trim(),
        reason: reason.trim() ? reason.trim() : undefined,
      };
      await updateSetting(editingSetting.key, request);

      showToast(
        `${editingSetting.description} updated successfully`,
        "success",
      );
      await fetchData();
      closeEditModal();
    } catch (e: unknown) {
      const message =
        e instanceof Error ? e.message : "Failed to update setting.";
      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex flex-col gap-4">
        {Array.from({ length: 4 }).map((_, idx) => (
          <div
            key={idx}
            className="animate-pulse bg-gray-200 rounded-xl h-24"
          />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div className="text-center">
          <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-amber-50 text-amber-600">
            <ReloadIcon />
          </div>
          <div className="text-sm font-medium text-gray-700">{error}</div>
          <div className="mt-3">
            <button
              onClick={() => void fetchData()}
              className="rounded-lg border border-gray-200 bg-white px-4 py-2 text-sm text-gray-600 hover:bg-gray-50"
            >
              Try again
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {settingGroups.map(({ groupName, settings: groupSettings }) => {
        const Icon = groupIcon(groupName);
        return (
          <div
            key={groupName}
            className="bg-white rounded-xl border border-gray-200 mb-4"
          >
            <div className="flex items-center gap-3 px-5 py-4 border-b border-gray-100">
              <Icon size={18} className="text-[#E6A817]" />
              <div className="text-sm font-medium text-gray-700">
                {groupName}
              </div>
            </div>

            <div className="divide-y divide-gray-50">
              {groupSettings.map((setting) => (
                <div
                  key={setting.key}
                  className="flex items-center justify-between px-5 py-4"
                >
                  <div>
                    <div className="text-sm font-medium text-gray-800">
                      {setting.description}
                    </div>
                    <div className="text-xs font-mono text-gray-400 mt-0.5">
                      {setting.key}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">
                      {setting.lastUpdatedBy
                        ? `Updated ${formatDate(setting.lastUpdatedAt)} by ${setting.lastUpdatedBy}`
                        : ""}
                    </div>
                  </div>

                  <div className="flex items-center gap-4">
                    <div className="text-sm font-medium text-gray-900">
                      {formatSettingValue(setting)}
                    </div>
                    <button
                      aria-label={`Edit ${setting.key}`}
                      className="p-1.5 rounded-lg border border-gray-200 text-gray-400 hover:text-[#E6A817] hover:border-amber-300 transition-colors"
                      onClick={() => openEditModal(setting)}
                      type="button"
                    >
                      <Pencil size={16} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        );
      })}

      {/* History */}
      <div className="bg-white rounded-xl border border-gray-200">
        <div className="flex justify-between items-center p-5 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <ClipboardList size={18} className="text-gray-400" />
            <div className="text-sm font-medium text-gray-700">
              Settings Change History
            </div>
          </div>
          {history.length > 20 && (
            <button
              type="button"
              className="text-xs text-[#E6A817] hover:underline"
              onClick={() => setShowAllHistory((v) => !v)}
            >
              {showAllHistory ? "Show less" : "Show all"}
            </button>
          )}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full table-fixed text-sm">
            <thead>
              <tr className="text-xs text-gray-400 uppercase tracking-wide">
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  Setting
                </th>
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  From
                </th>
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  To
                </th>
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  Changed By
                </th>
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  Date
                </th>
                <th className="px-5 py-3 border-b border-gray-100 text-left">
                  Reason
                </th>
              </tr>
            </thead>
            <tbody>
              {(showAllHistory ? history : history.slice(0, 20)).map((h) => (
                <tr
                  key={h.id}
                  className="border-b border-gray-50 last:border-0 hover:bg-gray-50"
                >
                  <td className="px-5 py-3 font-mono text-xs text-gray-700">
                    {h.settingKey}
                  </td>
                  <td className="px-5 py-3 text-sm text-gray-500">
                    {h.oldValue}
                  </td>
                  <td className="px-5 py-3 text-sm font-medium text-gray-800">
                    {h.newValue}
                  </td>
                  <td className="px-5 py-3 text-sm text-gray-600">
                    {h.changedBy}
                  </td>
                  <td className="px-5 py-3 text-xs text-gray-400">
                    {formatDateTime(h.changedAt)}
                  </td>
                  <td className="px-5 py-3 text-xs text-gray-400">
                    {h.reason || "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {history.length === 0 && (
          <div className="text-sm text-gray-400 text-center py-8">
            No settings changes recorded yet
          </div>
        )}
      </div>

      {/* Edit Modal */}
      {editingSetting && (
        <div
          className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4"
          onClick={closeEditModal}
        >
          <div
            className="bg-white rounded-xl border border-gray-200 w-full max-w-md p-6"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-4 mb-4">
              <div>
                <div className="text-sm font-medium text-gray-800">
                  Change Setting
                </div>
              </div>
              <button
                type="button"
                aria-label="Close"
                className="text-gray-400 hover:text-gray-600"
                onClick={closeEditModal}
              >
                <X size={18} />
              </button>
            </div>

            <div className="bg-gray-50 rounded-lg p-3 mb-4">
              <div className="font-medium text-gray-800 text-sm">
                {editingSetting.description}
              </div>
              <div className="font-mono text-xs text-gray-400 mt-0.5">
                {editingSetting.key}
              </div>
            </div>

            <div className="mb-3">
              <div className="text-xs text-gray-500 mb-1">Current Value</div>
              <div className="bg-gray-100 rounded-lg px-3 py-2 text-sm text-gray-600">
                {formatSettingValue(editingSetting)}
              </div>
            </div>

            <div className="mt-3">
              <label className="text-xs text-gray-500 mb-1 block">
                New Value
              </label>
              {editingSetting.dataType === "decimal" ||
              editingSetting.dataType === "int" ? (
                <input
                  type="number"
                  step={editingSetting.dataType === "decimal" ? "0.01" : "1"}
                  min={"1"}
                  className="border border-gray-300 rounded-lg px-3 py-2 text-sm w-full focus:ring-2 focus:ring-amber-400"
                  value={newValue}
                  onChange={(e) => {
                    setNewValue(e.target.value);
                    setValidationError(null);
                  }}
                  placeholder={placeholderForKey(editingSetting.key)}
                />
              ) : (
                <input
                  type="text"
                  className="border border-gray-300 rounded-lg px-3 py-2 text-sm w-full focus:ring-2 focus:ring-amber-400"
                  value={newValue}
                  onChange={(e) => {
                    setNewValue(e.target.value);
                    setValidationError(null);
                  }}
                  placeholder={placeholderForKey(editingSetting.key)}
                />
              )}

              {validationError && (
                <div className="text-xs text-red-500 mt-1">
                  {validationError}
                </div>
              )}
            </div>

            <div className="mt-3">
              <label className="text-xs text-gray-500 mb-1 block">
                Reason (optional)
              </label>
              <textarea
                rows={2}
                className="border border-gray-300 rounded-lg px-3 py-2 text-sm w-full resize-none focus:ring-2 focus:ring-amber-400"
                placeholder="e.g. Annual rate adjustment..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </div>

            {submitError && (
              <div className="mt-3 text-xs text-red-500 bg-red-50 rounded-lg px-3 py-2">
                {submitError}
              </div>
            )}

            <div className="flex justify-end gap-2 mt-5">
              <button
                type="button"
                className="rounded-lg border border-gray-200 px-4 py-2 text-sm text-gray-600 hover:bg-gray-50"
                onClick={closeEditModal}
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={
                  newValue === editingSetting.value ||
                  Boolean(validationError) ||
                  !newValue.trim() ||
                  isSubmitting
                }
                onClick={handlePreviewChange}
                className="rounded-lg bg-[#E6A817] px-4 py-2 text-sm font-medium text-[#1a1000] disabled:opacity-60"
              >
                Preview Change
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Confirm Modal */}
      {editingSetting && showConfirmModal && (
        <div
          className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4"
          onClick={() => setShowConfirmModal(false)}
        >
          <div
            className="bg-white rounded-xl border border-gray-200 w-full max-w-sm p-5"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between gap-3 mb-2">
              <div className="flex items-center gap-2">
                <AlertTriangle size={20} className="text-amber-500" />
                <div className="font-medium text-gray-800">Confirm Change</div>
              </div>
              <button
                type="button"
                aria-label="Close confirmation"
                className="text-gray-400 hover:text-gray-600"
                onClick={() => setShowConfirmModal(false)}
              >
                <X size={18} />
              </button>
            </div>

            <div className="text-sm text-gray-600">
              You are about to change:
            </div>
            <div className="font-medium text-gray-800 text-sm mt-2">
              {editingSetting.description}
            </div>

            <div className="mt-2">
              <div className="text-sm text-gray-500">From:</div>
              <div className="text-sm text-gray-500">
                {formatSettingValue(editingSetting)}
              </div>
              <ArrowDown size={16} className="text-gray-400 my-1" />
              <div className="text-sm font-medium text-gray-800">
                {formatSettingValue({ ...editingSetting, value: newValue })}
              </div>
            </div>

            <div className="mt-3 p-3 rounded-lg bg-amber-50 border border-amber-200">
              <div className="flex items-start gap-2">
                <AlertCircle
                  size={14}
                  className="text-amber-600 flex-shrink-0"
                />
                <div className="text-xs text-amber-700">
                  {impactMessage(editingSetting.key)}
                </div>
              </div>
              {reason.trim() && (
                <div className="text-xs text-gray-400 italic mt-2">
                  Reason: {reason.trim()}
                </div>
              )}
            </div>

            {submitError && (
              <div className="text-xs text-red-500 bg-red-50 rounded-lg px-3 py-2 mt-3">
                {submitError}
              </div>
            )}

            <div className="flex justify-end gap-2 mt-5">
              <button
                type="button"
                className="rounded-lg border border-gray-200 px-4 py-2 text-sm text-gray-600 hover:bg-gray-50"
                onClick={() => setShowConfirmModal(false)}
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={isSubmitting}
                className="rounded-lg bg-[#E6A817] px-4 py-2 text-sm font-medium text-[#1a1000] disabled:opacity-60"
                onClick={() => void handleConfirmUpdate()}
              >
                {isSubmitting ? (
                  <span className="inline-flex items-center gap-2">
                    <Loader2 size={16} className="animate-spin" />
                    Confirming...
                  </span>
                ) : (
                  "Confirm Change"
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

const ReloadIcon = () => {
  return (
    <span className="inline-flex" aria-hidden="true">
      ↻
    </span>
  );
};

export default Settings;
