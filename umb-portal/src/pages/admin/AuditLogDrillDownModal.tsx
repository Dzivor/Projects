import React from "react";
import type {
  AuditLogChargeDrillDownDTO,
  AuditLogDrillDownDTO,
} from "../../services/adminService";

type Props = {
  isOpen: boolean;
  isLoading: boolean;
  data: AuditLogDrillDownDTO | null;
  onClose: () => void;
};

const Row: React.FC<{ label: string; value: React.ReactNode }> = ({
  label,
  value,
}) => (
  <div>
    <div className="text-sm text-slate-600">{label}</div>
    <div className="text-sm font-semibold text-slate-900">{value}</div>
  </div>
);

const Copyable: React.FC<{ value: string | null | undefined }> = ({
  value,
}) => {
  if (!value) return <span className="text-gray-400">—</span>;
  return (
    <span className="inline-flex items-center gap-2">
      <span className="font-mono text-sm">{value}</span>
      <button
        type="button"
        className="rounded border border-gray-200 px-2 py-0.5 text-xs text-gray-600 hover:bg-gray-50"
        onClick={async () => {
          try {
            await navigator.clipboard.writeText(value);
          } catch {
            // ignore
          }
        }}
      >
        Copy
      </button>
    </span>
  );
};

const formatDateTimeMaybe = (v: string | null | undefined) => {
  if (!v) return "—";
  const d = new Date(v);
  return Number.isNaN(d.getTime())
    ? v
    : d.toLocaleString(undefined, {
        year: "numeric",
        month: "short",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      });
};

const ChargeSection: React.FC<{
  data: AuditLogDrillDownDTO;
}> = ({ data }) => {
  if (data.chargeMessage) {
    return (
      <div className="rounded-lg border border-amber-100 bg-amber-50 p-4">
        <div className="text-sm font-semibold text-amber-900">
          {data.chargeMessage}
        </div>
      </div>
    );
  }

  const charge: AuditLogChargeDrillDownDTO | null | undefined = data.charge;

  if (!charge) {
    return (
      <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 text-sm text-gray-500">
        No charge details available
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 gap-4">
      <Row label="Debit Account Number" value={charge.debitAccountNumber} />
      <Row label="Credit Account Number" value={charge.creditAccountNumber} />
      <Row
        label="Statement Account Number"
        value={charge.statementAccountNumber}
      />
      <div>
        <div className="text-sm text-slate-600">Bank Transaction Reference</div>
        <div className="mt-1 text-sm font-semibold text-slate-900">
          <Copyable value={charge.bankTransactionReference ?? undefined} />
        </div>
      </div>
      <Row label="Narration" value={charge.narration || "—"} />
      <Row
        label="CompletedAt"
        value={formatDateTimeMaybe(charge.completedAt ?? undefined)}
      />
    </div>
  );
};

const AuditLogDrillDownModal: React.FC<Props> = ({
  isOpen,
  isLoading,
  data,
  onClose,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-3xl rounded-lg bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h3 className="text-2xl font-bold text-slate-900">
              Audit Log Details
            </h3>
            {data ? (
              <div className="mt-1 text-sm text-slate-600">
                {data.staffUsername} • {data.channelUsed}
              </div>
            ) : null}
          </div>
          <button
            type="button"
            className="rounded border border-gray-200 px-3 py-1 text-sm text-gray-600 hover:bg-gray-50"
            onClick={onClose}
          >
            Close
          </button>
        </div>

        {isLoading || !data ? (
          <div className="mt-8 rounded-lg border border-gray-200 bg-gray-50 p-6">
            <div className="text-sm font-semibold text-gray-700">
              Loading...
            </div>
          </div>
        ) : (
          <div className="mt-5 space-y-5">
            <div className="rounded-lg border border-gray-200 bg-white p-4">
              <div className="grid grid-cols-2 gap-4">
                <Row label="Account Number" value={data.accountNumber} />
                <Row
                  label="Account Holder Name"
                  value={data.accountHolderName}
                />
                <Row
                  label="Period"
                  value={`${new Date(data.startDate).toLocaleDateString()} – ${new Date(
                    data.endDate,
                  ).toLocaleDateString()}`}
                />
                <Row label="Channel" value={data.channelUsed} />
                <Row label="Number of Pages" value={data.numberOfPages} />
                <Row
                  label="Amount Charged"
                  value={data.wasWaived ? "—" : data.amountCharged}
                />
              </div>

              <div className="mt-4">
                <Row
                  label="Staff"
                  value={`${data.staffFullName} (${data.staffUsername})`}
                />
              </div>
            </div>

            <div>
              <div className="mb-3 text-sm font-semibold text-slate-900">
                {data.chargeMessage ? "Charge" : "Charge Details"}
              </div>
              <ChargeSection data={data} />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default AuditLogDrillDownModal;
