type PreviewChargesModalProps = {
  isOpen: boolean;
  title: string;
  message: string;
  accountNumber: string;
  accountName: string;
  numberOfPages: number;
  totalChargeText: string;
  accountToCharge?: string;
  isPrinting: boolean;
  onPrint: () => void;
  onCancel: () => void;
};

const PreviewChargesModal = ({
  isOpen,
  title,
  message,
  accountNumber,
  accountName,
  numberOfPages,
  totalChargeText,
  accountToCharge,
  isPrinting,
  onPrint,
  onCancel,
}: PreviewChargesModalProps) => {
  const displayAccountToCharge = accountToCharge
    ? accountToCharge.replace(/^\s*GHS\s*/i, "").trim()
    : "";

  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-2xl rounded-lg bg-white p-6 shadow-xl">
        <h3 className="text-2xl font-bold text-slate-900">{title}</h3>

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
              {numberOfPages}
            </p>
          </div>
          <div>
            <p className="text-sm text-slate-600">Total Charge</p>
            <p className="text-lg font-semibold text-slate-900">
              {totalChargeText}
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
            <p className="mt-1 text-sm text-slate-900 font-semibold ">
              {message}
            </p>
          </div>
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            disabled={isPrinting}
            className="rounded bg-red-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onPrint}
            disabled={isPrinting}
            className="rounded bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isPrinting ? "Printing..." : "Print"}
          </button>
        </div>
      </div>
    </div>
  );
};

export default PreviewChargesModal;
