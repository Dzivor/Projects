import { Loader2 } from "lucide-react";

type StatementChannel = "VISA" | "ESB";

type StatementPreviewData = {
  numberOfPages: number;
  totalCharge: number;
  accountToCharge: string;
  chargeMessage: string;
  accountName: string;
  accountNumber: string;
  accountBalance: number;
  accountToChargeName?: string;
  accountToChargeBalance?: number;
};

type PreviewChargesModalProps = {
  isOpen: boolean;
  title: string;
  accountNumber: string;
  accountName: string;
  previewData: StatementPreviewData | null;
  channel: StatementChannel;
  waiveCharge: boolean;
  isPreviewLoading: boolean;
  previewErrorMessage: string;
  isPrinting: boolean;
  onPrint: () => void;
  onCancel: () => void;
  onRetry: () => void;
};

const formatGhsAmount = (amount: number) => `GHS ${amount.toFixed(2)}`;

const PreviewChargesModal = ({
  isOpen,
  title,
  accountNumber,
  accountName,
  previewData,
  channel,
  waiveCharge,
  isPreviewLoading,
  previewErrorMessage,
  isPrinting,
  onPrint,
  onCancel,
  onRetry,
}: PreviewChargesModalProps) => {
  const displayAccountToCharge = previewData?.accountToCharge
    ? previewData.accountToCharge.replace(/^\s*GHS\s*/i, "").trim()
    : "";

  const totalCharge = previewData?.totalCharge ?? 0;
  const accountBalance = previewData?.accountBalance ?? 0;
  const insufficientFunds =
    !isPreviewLoading &&
    !previewErrorMessage &&
    channel === "VISA" &&
    !waiveCharge &&
    accountBalance < totalCharge;

  const chargeMessage = (() => {
    if (previewErrorMessage) {
      return "";
    }

    if (channel === "ESB") {
      return "No charge applicable for ESB channel";
    }

    if (waiveCharge) {
      return "Charge has been waived";
    }

    return `Account balance: ${formatGhsAmount(accountBalance)}. Charge: ${formatGhsAmount(totalCharge)}`;
  })();

  const isPrintDisabled =
    isPreviewLoading ||
    isPrinting ||
    !!previewErrorMessage ||
    insufficientFunds;

  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-2xl rounded-lg bg-white p-6 shadow-xl">
        <h3 className="text-2xl font-bold text-slate-900">{title}</h3>

        {isPreviewLoading ? (
          <div className="mt-10 flex flex-col items-center justify-center gap-4 py-10 text-slate-600">
            <Loader2 className="h-10 w-10 animate-spin text-[#E6A817]" />
            <p className="text-sm font-medium">Loading preview...</p>
          </div>
        ) : previewErrorMessage ? (
          <div className="mt-8 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            <p className="font-semibold">
              Unable to load preview. Please try again.
            </p>
            <p className="mt-2">{previewErrorMessage}</p>
          </div>
        ) : (
          <div className="mt-5 grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-slate-600">Account Number</p>
              <p className="text-lg font-semibold text-slate-900">
                {accountNumber}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Account Name</p>
              <p className="text-lg font-semibold text-slate-900">
                {accountName}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Number of Pages</p>
              <p className="text-lg font-semibold text-slate-900">
                {previewData?.numberOfPages ?? 0}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Total Charge</p>
              <p className="text-lg font-semibold text-slate-900">
                {formatGhsAmount(totalCharge)}
              </p>
            </div>
            <div className="col-span-2">
              <p className="text-sm text-slate-600">Account to Charge</p>
              <p className="text-lg font-semibold text-slate-900">
                {displayAccountToCharge || "N/A"}
              </p>
            </div>
            <div className="col-span-2">
              <p className="text-sm text-slate-600">Charge Message</p>
              <p className="mt-1 text-sm font-semibold text-slate-900">
                {chargeMessage}
              </p>
            </div>

            {previewData?.accountToChargeName && (
              <div className="col-span-2 mt-1">
                <p className="text-sm text-slate-600">
                  Account to Charge - Name
                </p>
                <p className="text-sm font-semibold text-slate-900">
                  {previewData.accountToChargeName}
                </p>
                <p className="text-sm text-slate-600 mt-2">
                  Account to Charge - Balance
                </p>
                <p className="text-sm font-semibold text-slate-900">
                  {formatGhsAmount(previewData.accountToChargeBalance ?? 0)}
                </p>
              </div>
            )}

            {insufficientFunds && (
              <div className="col-span-2 rounded-md border border-red-200 bg-red-50 p-3 text-sm font-medium text-red-700">
                {`Insufficient funds. Available: ${formatGhsAmount(accountBalance)}. Required: ${formatGhsAmount(totalCharge)}.`}
              </div>
            )}
          </div>
        )}

        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            disabled={isPreviewLoading || isPrinting}
            className="rounded bg-red-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>

          {previewErrorMessage ? (
            <button
              type="button"
              onClick={onRetry}
              disabled={isPreviewLoading}
              className="rounded bg-[#E6A817] px-4 py-2 text-sm font-medium text-white transition hover:bg-[#cf980f] disabled:cursor-not-allowed disabled:opacity-50"
            >
              Retry
            </button>
          ) : (
            <button
              type="button"
              onClick={onPrint}
              disabled={isPrintDisabled}
              className={`rounded px-4 py-2 text-sm font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-50 ${
                isPrintDisabled
                  ? "bg-slate-400"
                  : "bg-green-600 hover:bg-green-700"
              }`}
            >
              {isPrinting ? "Printing..." : "Print"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default PreviewChargesModal;
